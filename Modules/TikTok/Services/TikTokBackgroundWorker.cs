using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using PortfolioMvc.Modules.TikTok.Models;

namespace PortfolioMvc.Modules.TikTok.Services;

public sealed class TikTokBackgroundWorker : BackgroundService
{
    private readonly TikTokJobQueue queue;
    private readonly TikTokVideoPipelineService pipeline;
    private readonly ILogger<TikTokBackgroundWorker> logger;
    private readonly TikTokToolsOptions options;

    public TikTokBackgroundWorker(TikTokJobQueue queue, TikTokVideoPipelineService pipeline,
        IOptions<TikTokToolsOptions> options, ILogger<TikTokBackgroundWorker> logger)
    {
        this.queue = queue;
        this.pipeline = pipeline;
        this.logger = logger;
        this.options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var workers = Enumerable.Range(0, Math.Clamp(options.MaxConcurrentDownloads, 1, 4))
            .Select(_ => ProcessQueueAsync(stoppingToken));
        await Task.WhenAll(workers);
    }

    private async Task ProcessQueueAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            TikTokJobState state;
            try { state = await queue.DequeueAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }

            try
            {
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, state.Cancellation.Token);
                await pipeline.ProcessAsync(state, linked.Token);
            }
            catch (OperationCanceledException)
            {
                state.Job.Status = "Cancelled";
                state.Job.CompletedUtc = DateTimeOffset.UtcNow;
                DeleteTemporaryFiles(state.Directory);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "TikTok job {JobId} failed", state.Job.Id);
                state.Job.Status = "Failed";
                state.Job.Error = exception is TikTokToolException ? exception.Message : "Tác vụ gặp lỗi khi xử lý video.";
                state.Job.CompletedUtc = DateTimeOffset.UtcNow;
                DeleteTemporaryFiles(state.Directory);
            }
        }
    }

    private void DeleteTemporaryFiles(string directory)
    {
        try { if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        { logger.LogWarning(exception, "Could not remove TikTok work directory"); }
    }
}
