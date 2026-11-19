namespace PortfolioMvc.Modules.VideoTrimmer.Models;

public sealed class VideoClip
{
    public string Id { get; init; } = string.Empty;
    public string OriginalFileName { get; init; } = string.Empty;
    public int ClipNumber { get; init; }
    public double DurationSeconds { get; init; }
    public DateTimeOffset CreatedUtc { get; init; }
}

public sealed class VideoTrimmerPageModel
{
    public IReadOnlyList<VideoClip> Clips { get; init; } = Array.Empty<VideoClip>();
}
