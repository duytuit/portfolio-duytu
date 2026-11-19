using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using PortfolioMvc.Modules.TikTok.Models;

namespace PortfolioMvc.Modules.TikTok.Services;

public sealed class TikTokDownloadService
{
    private static readonly Regex ProgressPattern = new(@"PROGRESS:(?<value>\d+(?:\.\d+)?)%", RegexOptions.Compiled);
    private readonly TikTokToolsOptions options;
    private readonly string contentRoot;
    private readonly ILogger<TikTokDownloadService> logger;
    private readonly ExternalProcessRunner processRunner;

    public TikTokDownloadService(IWebHostEnvironment environment, IOptions<TikTokToolsOptions> options,
        ExternalProcessRunner processRunner, ILogger<TikTokDownloadService> logger)
    {
        contentRoot = environment.ContentRootPath;
        this.options = options.Value;
        this.logger = logger;
        this.processRunner = processRunner;
    }

    public static bool IsValidTikTokUrl(string? value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
            !string.IsNullOrEmpty(uri.UserInfo) || !uri.IsDefaultPort)
            return false;
        var host = uri.IdnHost.TrimEnd('.');
        return host.Equals("tiktok.com", StringComparison.OrdinalIgnoreCase) ||
               host.EndsWith(".tiktok.com", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<TikTokVideoInfo> GetInfoAsync(string url, CancellationToken cancellationToken)
    {
        EnsureValidUrl(url);
        var result = await processRunner.RunAsync(Resolve(options.YtDlpPath), new[]
        {
            "--no-warnings", "--no-playlist", "--skip-download", "--dump-single-json", url
        }, contentRoot, cancellationToken, timeout: TimeSpan.FromMinutes(2));
        if (result.ExitCode != 0)
            throw new TikTokToolException("Không lấy được thông tin video. Video có thể riêng tư, đã bị xóa hoặc công cụ chưa được cài.");

        try
        {
            using var document = JsonDocument.Parse(result.Stdout);
            var root = document.RootElement;
            var duration = ReadDouble(root, "duration");
            if (duration is <= 0 || duration > options.MaxVideoDurationSeconds)
                throw new TikTokToolException($"Video phải dài hơn 0 và không vượt quá {options.MaxVideoDurationSeconds} giây.");
            var height = ReadInt(root, "height");
            var width = ReadInt(root, "width");
            return new TikTokVideoInfo
            {
                Title = ReadString(root, "title") ?? "TikTok video",
                Thumbnail = ReadString(root, "thumbnail"),
                DurationSeconds = duration,
                Resolution = width.HasValue && height.HasValue ? $"{width} × {height}" : null,
                Width = width,
                Height = height
            };
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "yt-dlp returned invalid metadata JSON");
            throw new TikTokToolException("Công cụ tải video trả về metadata không hợp lệ.");
        }
    }

    public TikTokJobState CreateJob(string url, string ownerId, bool removeWatermark,
        ManualWatermarkRegion? manualRegion, TikTokJobQueue queue, TikTokJobStore store)
    {
        EnsureValidUrl(url);
        var storage = Resolve(options.StoragePath);
        Directory.CreateDirectory(storage);
        CleanupExpired(storage);
        store.Prune(DateTimeOffset.UtcNow.AddHours(-Math.Clamp(options.JobRetentionHours, 1, 168)));
        if (store.History(ownerId).Count(job => job.Status is not ("Completed" or "Failed" or "Cancelled")) >= options.MaxQueueSize)
            throw new TikTokToolException("Bạn đã đạt giới hạn job đang chờ. Hãy đợi một job hoàn tất rồi thử lại.");
        if (removeWatermark && !IsValidRegion(manualRegion))
            throw new TikTokToolException("Hãy kéo chọn một vùng watermark hợp lệ trên ảnh video trước khi bật ProPainter.");
        if (removeWatermark)
        {
            var weights = Path.Combine(storage, "ProPainterRuntime", "weights");
            var repoWeights = Path.Combine(Resolve(options.ProPainterPath), "weights");
            var required = new[] { "raft-things.pth", "recurrent_flow_completion.pth", "ProPainter.pth" };
            var hasRuntimeWeights = required.All(name => File.Exists(Path.Combine(weights, name)));
            var hasRepoWeights = required.All(name => File.Exists(Path.Combine(repoWeights, name)));
            if (!hasRuntimeWeights && !hasRepoWeights)
            {
                Directory.CreateDirectory(weights);
                var missing = required.Where(name => !File.Exists(Path.Combine(weights, name)) &&
                    !File.Exists(Path.Combine(repoWeights, name))).ToArray();
                throw new TikTokToolException("Thiếu checkpoint ProPainter: " + string.Join(", ", missing) +
                    ". Đặt đủ ba tệp trong weights của repo ProPainter hoặc App_Data/TikTok/ProPainterRuntime/weights rồi thử lại.");
            }
        }

        var id = Guid.NewGuid();
        var directory = Path.Combine(storage, id.ToString("N"));
        Directory.CreateDirectory(directory);
        var state = new TikTokJobState
        {
            Job = new TikTokJob { Id = id, CreatedUtc = DateTimeOffset.UtcNow, WatermarkRemovalRequested = removeWatermark },
            Url = url,
            OwnerHash = TikTokJobStore.HashOwner(ownerId),
            Directory = directory,
            ManualRegion = removeWatermark ? manualRegion : null
        };
        if (!store.TryAdd(state) || !queue.TryEnqueue(state))
        {
            store.Remove(id);
            Directory.Delete(directory, recursive: true);
            throw new TikTokToolException("Hàng đợi đang đầy. Vui lòng thử lại sau.");
        }
        return state;
    }

    public (string Path, string Name)? GetDownload(Guid id, string ownerId, TikTokJobStore store)
    {
        var state = store.GetState(id);
        if (state is null || state.OwnerHash != TikTokJobStore.HashOwner(ownerId) || state.Job.Status != "Completed")
            return null;
        var file = Directory.Exists(state.Directory)
            ? Directory.EnumerateFiles(state.Directory).FirstOrDefault(path =>
                string.Equals(Path.GetExtension(path), ".mp4", StringComparison.OrdinalIgnoreCase))
            : null;
        return file is null ? null : (file, "tiktok-video.mp4");
    }

    public async Task<string> DownloadSourceAsync(TikTokJobState state, CancellationToken cancellationToken)
    {
        state.Job.Status = "Downloading";
        var executable = Resolve(options.YtDlpPath);
        var output = Path.Combine(state.Directory, "source.%(ext)s");
        var result = await processRunner.RunAsync(executable, new[]
        {
            "--no-warnings", "--no-playlist", "--newline", "--progress-template", "download:PROGRESS:%(progress._percent_str)s",
            "--format", "bestvideo+bestaudio/best", "--merge-output-format", "mp4",
            "--ffmpeg-location", Resolve(options.FFmpegPath),
            "--max-filesize", $"{options.MaxDownloadSizeMB}M", "--match-filter", $"duration <= {options.MaxVideoDurationSeconds}",
            "--output", output, state.Url
        }, state.Directory, cancellationToken, line => ReportProgress(line, state));

        if (result.ExitCode != 0)
        {
            logger.LogWarning("yt-dlp failed with exit code {ExitCode}: {Error}", result.ExitCode, result.Stderr);
            var detail = GetDownloadDiagnostic(result.Stderr);
            throw new TikTokToolException(detail is null
                ? "yt-dlp không tải được video. Hãy kiểm tra video còn công khai, phiên bản yt-dlp và cấu hình FFmpeg."
                : $"yt-dlp báo lỗi: {detail}");
        }

        var downloaded = Directory.EnumerateFiles(state.Directory)
            .Where(path => !path.EndsWith(".part", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(path => string.Equals(Path.GetExtension(path), ".mp4", StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();
        if (downloaded is null || new FileInfo(downloaded).Length == 0)
            throw new TikTokToolException("yt-dlp không tạo được tệp video hợp lệ.");
        if (new FileInfo(downloaded).Length > (long)options.MaxDownloadSizeMB * 1024 * 1024)
            throw new TikTokToolException($"Video vượt quá giới hạn {options.MaxDownloadSizeMB} MB.");

        return downloaded;
    }

    private static string? GetDownloadDiagnostic(string stderr)
    {
        var diagnostic = stderr.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .LastOrDefault(line => line.StartsWith("ERROR:", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("HTTP Error", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("ffmpeg", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("denied", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("unavailable", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("private", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("sign in", StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrWhiteSpace(diagnostic)) return null;
        diagnostic = diagnostic.Replace('\r', ' ').Replace('\t', ' ');
        return diagnostic.Length <= 320 ? diagnostic : diagnostic[..320] + "…";
    }

    private static bool IsValidRegion(ManualWatermarkRegion? region) => region is not null &&
        double.IsFinite(region.X) && double.IsFinite(region.Y) && double.IsFinite(region.Width) && double.IsFinite(region.Height) &&
        region.X >= 0 && region.Y >= 0 && region.Width > 0 && region.Height > 0 &&
        region.X + region.Width <= 1 && region.Y + region.Height <= 1 && region.Width <= 0.8 && region.Height <= 0.8;

    private void ReportProgress(string line, TikTokJobState state)
    {
        var match = ProgressPattern.Match(line);
        if (match.Success && double.TryParse(match.Groups["value"].Value.Trim(), NumberStyles.Float,
                CultureInfo.InvariantCulture, out var value)) state.Job.Progress = Math.Clamp(value, 0, 99);
    }

    private string Resolve(string path)
    {
        if (Path.IsPathRooted(path)) return path;
        return path.Contains(Path.DirectorySeparatorChar) || path.Contains(Path.AltDirectorySeparatorChar)
            ? Path.GetFullPath(Path.Combine(contentRoot, path))
            : path;
    }

    private void CleanupExpired(string storage)
    {
        var cutoff = DateTime.UtcNow.AddHours(-Math.Clamp(options.JobRetentionHours, 1, 168));
        foreach (var directory in Directory.EnumerateDirectories(storage))
        {
            if (!Guid.TryParseExact(Path.GetFileName(directory), "N", out _) || Directory.GetLastWriteTimeUtc(directory) >= cutoff)
                continue;
            try { Directory.Delete(directory, recursive: true); }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            { logger.LogWarning(exception, "Could not remove expired TikTok job directory"); }
        }
    }
    private static void EnsureValidUrl(string url)
    {
        if (!IsValidTikTokUrl(url)) throw new TikTokToolException("Chỉ chấp nhận URL HTTPS thuộc tên miền TikTok.");
    }
    private static string? ReadString(JsonElement root, string name) => root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    private static double? ReadDouble(JsonElement root, string name) => root.TryGetProperty(name, out var value) && value.TryGetDouble(out var result) ? result : null;
    private static int? ReadInt(JsonElement root, string name) => root.TryGetProperty(name, out var value) && value.TryGetInt32(out var result) ? result : null;
}

public sealed class TikTokToolException : Exception
{
    public TikTokToolException(string message) : base(message) { }
    public TikTokToolException(string message, Exception inner) : base(message, inner) { }
}
