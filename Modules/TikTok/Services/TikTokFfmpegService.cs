using System.Text.Json;
using System.Globalization;
using Microsoft.Extensions.Options;

namespace PortfolioMvc.Modules.TikTok.Services;

public sealed class TikTokFfmpegService
{
    private readonly TikTokToolsOptions options;
    private readonly string contentRoot;
    private readonly ExternalProcessRunner processes;

    public TikTokFfmpegService(IWebHostEnvironment environment, IOptions<TikTokToolsOptions> options,
        ExternalProcessRunner processes)
    {
        contentRoot = environment.ContentRootPath;
        this.options = options.Value;
        this.processes = processes;
    }

    public async Task<string> MakeMp4Async(string originalVideo, string? processedVideo, string output,
        CancellationToken cancellationToken)
    {
        var temporaryOutput = Path.Combine(Path.GetDirectoryName(output)!, "encoded.tmp.mp4");
        var arguments = processedVideo is null
            ? new[]
            {
                "-hide_banner", "-loglevel", "error", "-nostdin", "-y", "-i", originalVideo,
                "-map", "0:v:0", "-map", "0:a?", "-sn", "-dn",
                "-c:v", "libx264", "-preset", "veryfast", "-crf", "21", "-pix_fmt", "yuv420p",
                "-c:a", "aac", "-b:a", "160k", "-movflags", "+faststart", temporaryOutput
            }
            : new[]
            {
                "-hide_banner", "-loglevel", "error", "-nostdin", "-y", "-i", processedVideo,
                "-i", originalVideo, "-map", "0:v:0", "-map", "1:a?", "-sn", "-dn",
                "-c:v", "libx264", "-preset", "veryfast", "-crf", "21", "-pix_fmt", "yuv420p",
                "-c:a", "aac", "-b:a", "160k", "-shortest", "-movflags", "+faststart", temporaryOutput
            };

        var result = await processes.RunAsync(Resolve(options.FFmpegPath), arguments, Path.GetDirectoryName(output)!,
            cancellationToken, timeout: TimeSpan.FromMinutes(30));
        if (result.ExitCode != 0 || !File.Exists(temporaryOutput) || new FileInfo(temporaryOutput).Length == 0)
            throw new TikTokToolException("FFmpeg không tạo được tệp MP4 hợp lệ. Hãy kiểm tra video và cấu hình FFmpeg.");

        var validation = await processes.RunAsync(Resolve(options.FFprobePath), new[]
        {
            "-v", "error", "-show_entries", "format=duration,size:stream=codec_type", "-of", "json", temporaryOutput
        }, Path.GetDirectoryName(output)!, cancellationToken, timeout: TimeSpan.FromMinutes(2));
        if (validation.ExitCode != 0 || !IsPlayableVideo(validation.Stdout) ||
            new FileInfo(temporaryOutput).Length > (long)options.MaxDownloadSizeMB * 1024 * 1024)
        {
            File.Delete(temporaryOutput);
            throw new TikTokToolException("FFprobe không đọc được video MP4 vừa tạo.");
        }
        File.Move(temporaryOutput, output, overwrite: true);
        return output;
    }

    private static bool IsPlayableVideo(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            var hasVideo = root.TryGetProperty("streams", out var streams) && streams.EnumerateArray()
                .Any(stream => stream.TryGetProperty("codec_type", out var type) && type.GetString() == "video");
            return hasVideo && root.TryGetProperty("format", out var format) &&
                format.TryGetProperty("duration", out var duration) &&
                double.TryParse(duration.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds) &&
                double.IsFinite(seconds) && seconds > 0;
        }
        catch (JsonException) { return false; }
    }

    private string Resolve(string path) => Path.IsPathRooted(path)
        ? path
        : path.Contains(Path.DirectorySeparatorChar) || path.Contains(Path.AltDirectorySeparatorChar)
            ? Path.GetFullPath(Path.Combine(contentRoot, path))
            : path;
}
