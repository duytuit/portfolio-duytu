using Microsoft.Extensions.Options;
using System.Globalization;
using PortfolioMvc.Modules.TikTok.Models;

namespace PortfolioMvc.Modules.TikTok.Services;

public sealed class TikTokAiService
{
    private readonly TikTokToolsOptions options;
    private readonly string contentRoot;
    private readonly ExternalProcessRunner processes;

    public TikTokAiService(IWebHostEnvironment environment, IOptions<TikTokToolsOptions> options,
        ExternalProcessRunner processes)
    {
        contentRoot = environment.ContentRootPath;
        this.options = options.Value;
        this.processes = processes;
    }

    public async Task<string> RemoveRegionAsync(TikTokJobState job, string sourceVideo, CancellationToken cancellationToken)
    {
        if (job.ManualRegion is null)
            throw new TikTokToolException("Chưa có vùng mask thủ công cho tác vụ ProPainter.");

        var python = Resolve(options.PythonPath);
        var scripts = Resolve("Modules/TikTok/PythonAI");
        var masks = Path.Combine(job.Directory, "masks");
        var maskArgs = new[]
        {
            Path.Combine(scripts, "generate_masks.py"), "--video", sourceVideo, "--output", masks,
            "--x", job.ManualRegion.X.ToString(CultureInfo.InvariantCulture),
            "--y", job.ManualRegion.Y.ToString(CultureInfo.InvariantCulture),
            "--width", job.ManualRegion.Width.ToString(CultureInfo.InvariantCulture),
            "--height", job.ManualRegion.Height.ToString(CultureInfo.InvariantCulture),
            "--dilation", Math.Clamp(options.AiMaskDilationPixels, 0, 32).ToString(CultureInfo.InvariantCulture)
        };
        job.Job.Status = "GeneratingMasks";
        var maskResult = await processes.RunAsync(python, maskArgs, contentRoot, cancellationToken,
            timeout: TimeSpan.FromMinutes(10));
        if (maskResult.ExitCode != 0)
            throw new TikTokToolException("Không tạo được mask theo vùng đã chọn. Hãy kiểm tra video và Python/OpenCV.");

        var repo = Resolve(options.ProPainterPath);
        var runtime = Path.Combine(Resolve(options.StoragePath), "ProPainterRuntime");
        var output = Path.Combine(job.Directory, "propainter-output");
        var wrapper = Path.Combine(scripts, "run_propainter.py");
        var arguments = new[]
        {
            wrapper, "--repo", repo, "--video", sourceVideo, "--mask", masks, "--output", output,
            "--runtime-dir", runtime,
            "--max-dimension", Math.Clamp(options.AiMaximumDimension, 256, 1280).ToString(CultureInfo.InvariantCulture),
            "--subvideo-length", Math.Clamp(options.AiSubvideoLength, 20, 100).ToString(CultureInfo.InvariantCulture)
        };

        var checkpointDir = Path.Combine(runtime, "weights");
        var repositoryCheckpointDir = Path.Combine(repo, "weights");
        var expectedCheckpoints = new[] { "raft-things.pth", "recurrent_flow_completion.pth", "ProPainter.pth" };
        if (!expectedCheckpoints.All(name => File.Exists(Path.Combine(checkpointDir, name))) &&
            !expectedCheckpoints.All(name => File.Exists(Path.Combine(repositoryCheckpointDir, name))))
            throw new TikTokToolException("Thiếu checkpoint ProPainter. Đặt raft-things.pth, recurrent_flow_completion.pth và ProPainter.pth trong thư mục weights của repo hoặc App_Data/TikTok/ProPainterRuntime/weights.");

        job.Job.Status = "AIProcessing";
        var result = await processes.RunAsync(python, arguments, contentRoot, cancellationToken,
            timeout: TimeSpan.FromHours(4));
        if (result.ExitCode != 0)
        {
            var detail = result.Stderr.Split('\n', StringSplitOptions.RemoveEmptyEntries).LastOrDefault()?.Trim();
            throw new TikTokToolException(string.IsNullOrWhiteSpace(detail)
                ? "ProPainter xử lý không thành công. Kiểm tra Python, checkpoint và bộ nhớ GPU."
                : $"ProPainter xử lý không thành công: {detail}");
        }

        var resultVideo = Path.Combine(output, Path.GetFileNameWithoutExtension(sourceVideo), "inpaint_out.mp4");
        if (!File.Exists(resultVideo) || new FileInfo(resultVideo).Length == 0)
            throw new TikTokToolException("ProPainter không tạo được video kết quả inpaint_out.mp4.");
        return resultVideo;
    }

    private string Resolve(string path) => Path.IsPathRooted(path)
        ? path
        : path.Contains(Path.DirectorySeparatorChar) || path.Contains(Path.AltDirectorySeparatorChar)
            ? Path.GetFullPath(Path.Combine(contentRoot, path))
            : path;
}
