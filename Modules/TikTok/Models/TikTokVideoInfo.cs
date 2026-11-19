namespace PortfolioMvc.Modules.TikTok.Models;

public sealed class TikTokVideoInfo
{
    public string Title { get; init; } = string.Empty;
    public string? Thumbnail { get; init; }
    public double? DurationSeconds { get; init; }
    public string? Resolution { get; init; }
    public int? Width { get; init; }
    public int? Height { get; init; }
}
