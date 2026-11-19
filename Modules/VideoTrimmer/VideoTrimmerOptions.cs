namespace PortfolioMvc.Modules.VideoTrimmer;

public sealed class VideoTrimmerOptions
{
    public string FfmpegPath { get; set; } = "ffmpeg";
    public string FfprobePath { get; set; } = "ffprobe";
}
