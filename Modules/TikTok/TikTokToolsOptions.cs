namespace PortfolioMvc.Modules.TikTok;

public sealed class TikTokToolsOptions
{
    public string YtDlpPath { get; set; } = "Tools/yt-dlp.exe";
    public string FFmpegPath { get; set; } = "Tools/ffmpeg.exe";
    public string FFprobePath { get; set; } = "Tools/ffprobe.exe";
    public string PythonPath { get; set; } = "python";
    public string ProPainterPath { get; set; } = "ProPainter";
    public string StoragePath { get; set; } = "App_Data/TikTok";
    public int MaxConcurrentDownloads { get; set; } = 2;
    public int MaxConcurrentAIJobs { get; set; } = 1;
    public int MaxQueueSize { get; set; } = 20;
    public int MaxVideoDurationSeconds { get; set; } = 180;
    public int MaxDownloadSizeMB { get; set; } = 300;
    public int JobRetentionHours { get; set; } = 24;
    public int AiMaximumDimension { get; set; } = 432;
    public int AiSubvideoLength { get; set; } = 50;
    public int AiMaskDilationPixels { get; set; } = 6;
}
