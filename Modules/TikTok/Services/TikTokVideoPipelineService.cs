using Microsoft.Extensions.Options;
using PortfolioMvc.Modules.TikTok.Models;

namespace PortfolioMvc.Modules.TikTok.Services;

public sealed class TikTokVideoPipelineService
{
    private readonly TikTokDownloadService downloader;
    private readonly TikTokAiService ai;
    private readonly TikTokFfmpegService ffmpeg;
    private readonly SemaphoreSlim aiSlots;

    public TikTokVideoPipelineService(TikTokDownloadService downloader, TikTokAiService ai,
        TikTokFfmpegService ffmpeg, SemaphoreSlim aiSlots)
    {
        this.downloader = downloader;
        this.ai = ai;
        this.ffmpeg = ffmpeg;
        this.aiSlots = aiSlots;
    }

    public async Task ProcessAsync(TikTokJobState state, CancellationToken cancellationToken)
    {
        var source = await downloader.DownloadSourceAsync(state, cancellationToken);
        string? repaired = null;
        if (state.ManualRegion is not null)
        {
            state.Job.Status = "WaitingForAI";
            await aiSlots.WaitAsync(cancellationToken);
            try { repaired = await ai.RemoveRegionAsync(state, source, cancellationToken); }
            finally { aiSlots.Release(); }
        }

        state.Job.Status = "Encoding";
        var output = Path.Combine(state.Directory, "result.mp4");
        await ffmpeg.MakeMp4Async(source, repaired, output, cancellationToken);
        state.Job.Progress = 100;
        state.Job.Status = "Completed";
        state.Job.CompletedUtc = DateTimeOffset.UtcNow;
        state.Job.DownloadUrl = $"/tiktok/video/{state.Job.Id:D}";

        foreach (var file in Directory.EnumerateFiles(state.Directory))
        {
            if (string.Equals(file, output, StringComparison.OrdinalIgnoreCase)) continue;
            try { File.Delete(file); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
        foreach (var directory in Directory.EnumerateDirectories(state.Directory))
        {
            try { Directory.Delete(directory, recursive: true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
