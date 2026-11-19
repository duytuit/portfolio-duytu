namespace PortfolioMvc.Modules.TikTok.Models;

public sealed class TikTokJob
{
    public Guid Id { get; init; }
    public string Status { get; set; } = "Queued";
    public double Progress { get; set; }
    public string? Error { get; set; }
    public DateTimeOffset CreatedUtc { get; init; }
    public DateTimeOffset? CompletedUtc { get; set; }
    public string? DownloadUrl { get; set; }
    public bool WatermarkRemovalRequested { get; init; }
}

public sealed class TikTokJobState
{
    public TikTokJob Job { get; init; } = new();
    public string Url { get; init; } = string.Empty;
    public string OwnerHash { get; init; } = string.Empty;
    public string Directory { get; init; } = string.Empty;
    public ManualWatermarkRegion? ManualRegion { get; init; }
    public CancellationTokenSource Cancellation { get; } = new();
}

public sealed class ManualWatermarkRegion
{
    public double X { get; init; }
    public double Y { get; init; }
    public double Width { get; init; }
    public double Height { get; init; }
}
