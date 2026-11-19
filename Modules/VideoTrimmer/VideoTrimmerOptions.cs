namespace PortfolioMvc.Modules.VideoTrimmer;

public sealed class VideoTrimmerOptions
{
    public string FfmpegPath { get; set; } = "ffmpeg";
    public string FfprobePath { get; set; } = "ffprobe";
    public int MaxConcurrentJobs { get; set; } = 1;
    public int EncoderThreads { get; set; } = 2;
}
