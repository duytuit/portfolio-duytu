using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PortfolioMvc.Modules.TikTok.Services;

namespace PortfolioMvc.Modules.TikTok;

public static class TikTokModule
{
    public static IServiceCollection AddTikTokModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TikTokToolsOptions>(configuration.GetSection("TikTokTools"));
        services.AddSingleton<TikTokJobStore>();
        services.AddSingleton<TikTokJobQueue>(_ => new TikTokJobQueue(
            configuration.GetValue<int?>("TikTokTools:MaxQueueSize") ?? 20));
        services.AddSingleton<ExternalProcessRunner>();
        services.AddSingleton<TikTokDownloadService>();
        services.AddSingleton<TikTokAiService>();
        services.AddSingleton<TikTokFfmpegService>();
        services.AddSingleton<SemaphoreSlim>(provider => new SemaphoreSlim(Math.Clamp(
            provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<TikTokToolsOptions>>().Value.MaxConcurrentAIJobs, 1, 2)));
        services.AddSingleton<TikTokVideoPipelineService>();
        services.AddHostedService<TikTokBackgroundWorker>();
        return services;
    }
}
