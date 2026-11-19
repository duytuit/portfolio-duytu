namespace PortfolioMvc.Modules.VideoTrimmer.Models;

public sealed class VideoClipMetadata
{
    public string Id { get; init; } = string.Empty;
    public string OwnerHash { get; init; } = string.Empty;
    public string OriginalFileName { get; init; } = string.Empty;
    public int ClipNumber { get; init; }
    public double DurationSeconds { get; init; }
    public DateTimeOffset CreatedUtc { get; init; }
}
