using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PortfolioMvc.Modules.VideoTrimmer.Services;

namespace PortfolioMvc.Modules.VideoTrimmer;

public static class VideoTrimmerModule
{
    public static IServiceCollection AddVideoTrimmerModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<VideoTrimmerOptions>(configuration.GetSection("VideoTrimmer"));
        services.AddSingleton<VideoTrimmerService>();
        return services;
    }
}
