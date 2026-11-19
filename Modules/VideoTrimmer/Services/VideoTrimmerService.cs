using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PortfolioMvc.Modules.VideoTrimmer.Models;

namespace PortfolioMvc.Modules.VideoTrimmer.Services;

public sealed class VideoTrimmerService
{
    private const long MaxUploadBytes = 500L * 1024 * 1024;
    private const double MaxVideoDurationSeconds = 2 * 60 * 60;
    private const int MaxClips = 500;
    private const int MaxSegmentSeconds = 3600;
    private static readonly TimeSpan ResultRetention = TimeSpan.FromHours(24);
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".mov", ".mkv", ".avi", ".webm", ".m4v"
    };
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly string storagePath;
    private readonly VideoTrimmerOptions options;
    private readonly ILogger<VideoTrimmerService> logger;

    public VideoTrimmerService(
        IWebHostEnvironment environment,
        IOptions<VideoTrimmerOptions> options,
        ILogger<VideoTrimmerService> logger)
    {
        storagePath = Path.Combine(environment.ContentRootPath, "App_Data", "VideoTrimmer");
        this.options = options.Value;
        this.logger = logger;
    }

    public IReadOnlyList<VideoClip> GetClips(string ownerId)
    {
        if (!Directory.Exists(storagePath))
            return Array.Empty<VideoClip>();

        CleanupExpiredJobs();
        var ownerHash = GetOwnerHash(ownerId);
        return Directory.EnumerateDirectories(storagePath)
            .Where(directory => Guid.TryParseExact(Path.GetFileName(directory), "N", out _))
            .SelectMany(directory => Directory.EnumerateFiles(directory, "clip_*.json")
                .Select(ReadMetadata))
            .Where(metadata => metadata.OwnerHash == ownerHash)
            .Where(metadata => File.Exists(GetMediaPath(metadata.Id)))
            .OrderByDescending(metadata => metadata.CreatedUtc)
            .ThenBy(metadata => metadata.ClipNumber)
            .Select(ToClip)
            .ToArray();
    }

    public async Task<IReadOnlyList<VideoClip>> CutAsync(
        IFormFile video,
        int segmentSeconds,
        string ownerId,
        CancellationToken cancellationToken)
    {
        if (segmentSeconds is < 1 or > MaxSegmentSeconds)
            throw new InvalidDataException($"Thời lượng mỗi đoạn phải từ 1 đến {MaxSegmentSeconds} giây.");
        if (video.Length is <= 0 or > MaxUploadBytes)
            throw new InvalidDataException("Video phải có dung lượng lớn hơn 0 và không vượt quá 500 MB.");

        var extension = Path.GetExtension(video.FileName);
        if (!SupportedExtensions.Contains(extension))
            throw new InvalidDataException("Định dạng hỗ trợ: MP4, MOV, MKV, AVI, WEBM và M4V.");

        Directory.CreateDirectory(storagePath);
        CleanupExpiredJobs();
        var jobId = Guid.NewGuid();
        var jobName = jobId.ToString("N");
        var workDirectory = Path.Combine(storagePath, ".work-" + jobName);
        var finalDirectory = Path.Combine(storagePath, jobName);
        Directory.CreateDirectory(workDirectory);

        try
        {
            var inputPath = Path.Combine(workDirectory, "source" + extension.ToLowerInvariant());
            await using (var input = new FileStream(inputPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                await video.CopyToAsync(input, cancellationToken);

            var duration = await GetDurationAsync(inputPath, cancellationToken);
            if (duration <= 0 || duration > MaxVideoDurationSeconds)
                throw new InvalidDataException("Video phải có thời lượng lớn hơn 0 và không vượt quá 2 giờ.");

            if (Math.Ceiling(duration / segmentSeconds) > MaxClips)
                throw new InvalidDataException("Video tạo ra quá 500 đoạn. Hãy tăng số giây mỗi đoạn.");

            var outputPattern = Path.Combine(workDirectory, "clip_%03d.mp4");
            var result = await RunProcessAsync(
                options.FfmpegPath,
                workDirectory,
                new[]
                {
                    "-hide_banner", "-loglevel", "error", "-nostdin", "-y",
                    "-i", inputPath,
                    "-map", "0:v:0", "-map", "0:a?",
                    "-sn", "-dn",
                    "-c:v", "libx264", "-preset", "veryfast", "-crf", "23", "-pix_fmt", "yuv420p",
                    "-c:a", "aac", "-b:a", "128k",
                    "-force_key_frames", $"expr:gte(t,n_forced*{segmentSeconds})",
                    "-f", "segment", "-segment_time", segmentSeconds.ToString(CultureInfo.InvariantCulture),
                    "-segment_start_number", "1", "-reset_timestamps", "1",
                    "-segment_format", "mp4", outputPattern
                },
                cancellationToken);

            if (result.ExitCode != 0)
                throw new VideoProcessingException("FFmpeg không thể xử lý video.", result.StandardError);

            var outputFiles = Directory.EnumerateFiles(workDirectory, "clip_*.mp4")
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            if (outputFiles.Length == 0 || outputFiles.Length > MaxClips)
                throw new VideoProcessingException("FFmpeg không tạo được danh sách đoạn video hợp lệ.", result.StandardError);

            var originalFileName = GetSafeDisplayName(video.FileName);
            var createdUtc = DateTimeOffset.UtcNow;
            var clips = new List<VideoClip>(outputFiles.Length);
            for (var index = 0; index < outputFiles.Length; index++)
            {
                var clipNumber = index + 1;
                var id = $"{jobName}-{clipNumber:D3}";
                var clipDuration = Math.Min(segmentSeconds, Math.Max(0, duration - index * segmentSeconds));
                var metadata = new VideoClipMetadata
                {
                    Id = id,
                    OwnerHash = GetOwnerHash(ownerId),
                    OriginalFileName = originalFileName,
                    ClipNumber = clipNumber,
                    DurationSeconds = clipDuration,
                    CreatedUtc = createdUtc
                };
                var metadataPath = Path.Combine(workDirectory, $"clip_{clipNumber:D3}.json");
                await File.WriteAllTextAsync(metadataPath, JsonSerializer.Serialize(metadata, JsonOptions), cancellationToken);
                clips.Add(ToClip(metadata));
            }

            File.Delete(inputPath);
            Directory.Move(workDirectory, finalDirectory);
            return clips;
        }
        finally
        {
            if (Directory.Exists(workDirectory))
            {
                try
                {
                    Directory.Delete(workDirectory, recursive: true);
                }
                catch (IOException exception)
                {
                    logger.LogWarning(exception, "Could not remove temporary video workspace {Workspace}", workDirectory);
                }
                catch (UnauthorizedAccessException exception)
                {
                    logger.LogWarning(exception, "Could not remove temporary video workspace {Workspace}", workDirectory);
                }
            }
        }
    }

    public (string Path, string DownloadName)? GetDownload(string id, string ownerId)
    {
        if (!TryParseId(id, out var jobId, out var clipNumber))
            return null;

        var metadataPath = GetMetadataPath(jobId, clipNumber);
        var mediaPath = GetMediaPath(jobId, clipNumber);
        if (!File.Exists(metadataPath) || !File.Exists(mediaPath))
            return null;

        var metadata = ReadMetadata(metadataPath);
        if (metadata.OwnerHash != GetOwnerHash(ownerId))
            return null;
        var downloadName = $"{Path.GetFileNameWithoutExtension(metadata.OriginalFileName)}_part_{clipNumber:D3}.mp4";
        return (mediaPath, downloadName);
    }

    public int DeleteClips(IEnumerable<string> ids, string ownerId)
    {
        var deleted = 0;
        var ownerHash = GetOwnerHash(ownerId);
        foreach (var id in ids.Distinct(StringComparer.Ordinal))
        {
            if (!TryParseId(id, out var jobId, out var clipNumber))
                throw new InvalidDataException("Danh sách video cần xóa không hợp lệ.");

            var mediaPath = GetMediaPath(jobId, clipNumber);
            var metadataPath = GetMetadataPath(jobId, clipNumber);
            if (!File.Exists(metadataPath))
                continue;

            if (ReadMetadata(metadataPath).OwnerHash != ownerHash)
                continue;
            if (File.Exists(mediaPath))
                File.Delete(mediaPath);
            if (File.Exists(metadataPath))
                File.Delete(metadataPath);
            deleted++;

            var jobDirectory = GetJobDirectory(jobId);
            if (Directory.Exists(jobDirectory) && !Directory.EnumerateFileSystemEntries(jobDirectory).Any())
                Directory.Delete(jobDirectory);
        }

        return deleted;
    }

    private async Task<double> GetDurationAsync(string inputPath, CancellationToken cancellationToken)
    {
        var result = await RunProcessAsync(
            options.FfprobePath,
            Path.GetDirectoryName(inputPath)!,
            new[]
            {
                "-v", "error", "-show_entries", "format=duration",
                "-of", "default=noprint_wrappers=1:nokey=1", inputPath
            },
            cancellationToken);

        if (result.ExitCode != 0 ||
            !double.TryParse(result.StandardOutput.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var duration) ||
            !double.IsFinite(duration))
            throw new InvalidDataException("Không đọc được thời lượng video. Hãy kiểm tra tệp đầu vào.");

        return duration;
    }

    private async Task<ProcessResult> RunProcessAsync(
        string executable,
        string workingDirectory,
        IEnumerable<string> arguments,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);

        using var process = new Process { StartInfo = startInfo };
        try
        {
            if (!process.Start())
                throw new VideoProcessingException($"Không thể khởi chạy {executable}.");
        }
        catch (Win32Exception exception)
        {
            throw new VideoProcessingException($"Không tìm thấy hoặc không khởi chạy được {executable}.", exception);
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(30));
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
        try
        {
            await process.WaitForExitAsync(linked.Token);
        }
        catch (OperationCanceledException) when (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync(CancellationToken.None);
            await Task.WhenAll(stdoutTask, stderrTask);
            if (timeout.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                throw new VideoProcessingException($"{executable} đã vượt quá thời gian xử lý tối đa.");
            throw;
        }

        return new ProcessResult(process.ExitCode, await stdoutTask, await stderrTask);
    }

    private VideoClipMetadata ReadMetadata(string path)
    {
        var metadata = JsonSerializer.Deserialize<VideoClipMetadata>(File.ReadAllText(path), JsonOptions);
        if (metadata is null || !TryParseId(metadata.Id, out _, out _))
            throw new InvalidDataException($"Metadata video không hợp lệ: {Path.GetFileName(path)}");
        return metadata;
    }

    private void CleanupExpiredJobs()
    {
        var cutoff = DateTime.UtcNow - ResultRetention;
        foreach (var directory in Directory.EnumerateDirectories(storagePath))
        {
            if (Directory.GetLastWriteTimeUtc(directory) < cutoff)
                Directory.Delete(directory, recursive: true);
        }
    }

    private string GetJobDirectory(Guid jobId) => Path.Combine(storagePath, jobId.ToString("N"));
    private string GetMetadataPath(Guid jobId, int clipNumber) =>
        Path.Combine(GetJobDirectory(jobId), $"clip_{clipNumber:D3}.json");
    private string GetMediaPath(Guid jobId, int clipNumber) =>
        Path.Combine(GetJobDirectory(jobId), $"clip_{clipNumber:D3}.mp4");

    private string GetMediaPath(string id)
    {
        if (!TryParseId(id, out var jobId, out var clipNumber))
            return string.Empty;
        return GetMediaPath(jobId, clipNumber);
    }

    private static bool TryParseId(string? id, out Guid jobId, out int clipNumber)
    {
        jobId = Guid.Empty;
        clipNumber = 0;
        if (id is null || id.Length != 36 || id[32] != '-' ||
            !Guid.TryParseExact(id.Substring(0, 32), "N", out jobId) ||
            !int.TryParse(id.Substring(33), NumberStyles.None, CultureInfo.InvariantCulture, out clipNumber) ||
            clipNumber is < 1 or > MaxClips ||
            clipNumber.ToString("D3", CultureInfo.InvariantCulture) != id.Substring(33))
            return false;
        return true;
    }

    private static string GetSafeDisplayName(string fileName)
    {
        var basename = Path.GetFileName(fileName.Replace('\\', '/'));
        var safeName = new string(basename.Where(character =>
            !char.IsControl(character) && !Path.GetInvalidFileNameChars().Contains(character)).ToArray()).Trim();
        if (safeName.Length > 150)
            safeName = safeName[..150];
        return string.IsNullOrWhiteSpace(safeName) ? "video" : safeName;
    }

    private static string GetOwnerHash(string ownerId) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(ownerId)));

    private static VideoClip ToClip(VideoClipMetadata metadata) => new()
    {
        Id = metadata.Id,
        OriginalFileName = metadata.OriginalFileName,
        ClipNumber = metadata.ClipNumber,
        DurationSeconds = metadata.DurationSeconds,
        CreatedUtc = metadata.CreatedUtc
    };

    private sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);
}

public sealed class VideoProcessingException : Exception
{
    public VideoProcessingException(string message) : base(message) { }
    public VideoProcessingException(string message, Exception innerException) : base(message, innerException) { }
    public VideoProcessingException(string message, string standardError) : base(message + " " + standardError) { }
}
