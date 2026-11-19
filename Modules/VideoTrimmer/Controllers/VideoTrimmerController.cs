using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;
using PortfolioMvc.Modules.VideoTrimmer.Services;
using PortfolioMvc.Modules.VideoTrimmer.Models;

namespace PortfolioMvc.Modules.VideoTrimmer.Controllers;

public sealed class VideoTrimmerController : Controller
{
    private const long MaxRequestBytes = 525L * 1024 * 1024;
    private const string OwnerCookieName = "video-trimmer-owner";
    private readonly VideoTrimmerService service;
    private readonly ILogger<VideoTrimmerController> logger;

    public VideoTrimmerController(VideoTrimmerService service, ILogger<VideoTrimmerController> logger)
    {
        this.service = service;
        this.logger = logger;
    }

    [HttpGet("/tools/video-trimmer")]
    public IActionResult Index() => View("~/Modules/VideoTrimmer/Views/Index.cshtml",
        new VideoTrimmerPageModel { Clips = service.GetClips(GetOwnerId()) });

    [HttpPost("/tools/video-trimmer/cut")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes)]
    public async Task<IActionResult> Cut(IFormFile? video, int segmentSeconds, CancellationToken cancellationToken)
    {
        if (video is null || video.Length == 0)
        {
            logger.LogWarning("Video cut request rejected because no video file was uploaded");
            TempData["Error"] = "Vui lòng chọn một video trước khi cắt.";
            return RedirectToAction(nameof(Index));
        }

        var fileName = Path.GetFileName(video.FileName);
        logger.LogInformation(
            "Video cut requested for {FileName}, size {FileSizeBytes} bytes, segment length {SegmentSeconds} seconds",
            fileName,
            video.Length,
            segmentSeconds);

        try
        {
            var clips = await service.CutAsync(video, segmentSeconds, GetOwnerId(), cancellationToken);
            logger.LogInformation("Video cut completed for {FileName}; created {ClipCount} clips", fileName, clips.Count);
            TempData["Notice"] = $"Đã cắt video thành {clips.Count} đoạn.";
        }
        catch (InvalidDataException exception)
        {
            logger.LogWarning(exception, "Video cut request rejected for {FileName}", fileName);
            TempData["Error"] = exception.Message;
        }
        catch (VideoProcessingException exception)
        {
            logger.LogError(exception, "Video processing failed for upload {FileName}", fileName);
            TempData["Error"] = "Không cắt được video. Hãy kiểm tra cấu hình FFmpeg hoặc thử một tệp video khác.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/tools/video-trimmer/delete")]
    [ValidateAntiForgeryToken]
    public IActionResult Delete([FromForm] string[]? ids)
    {
        if (ids is null || ids.Length == 0)
        {
            TempData["Error"] = "Hãy chọn ít nhất một video cần xóa.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var deleted = service.DeleteClips(ids, GetOwnerId());
            logger.LogInformation("Deleted {ClipCount} video clips at user request", deleted);
            TempData["Notice"] = $"Đã xóa {deleted} video.";
        }
        catch (InvalidDataException exception)
        {
            logger.LogWarning(exception, "Video clip deletion request rejected");
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("/tools/video-trimmer/download/{id}")]
    public IActionResult Download(string id)
    {
        var download = service.GetDownload(id, GetOwnerId());
        return download is null
            ? NotFound()
            : PhysicalFile(download.Value.Path, "video/mp4", download.Value.DownloadName);
    }

    private string GetOwnerId()
    {
        if (Request.Cookies.TryGetValue(OwnerCookieName, out var ownerId) &&
            ownerId is not null &&
            ownerId.Length == 43 &&
            ownerId.All(character => char.IsLetterOrDigit(character) || character is '-' or '_'))
            return ownerId;

        ownerId = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        Response.Cookies.Append(OwnerCookieName, ownerId, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            IsEssential = true,
            MaxAge = TimeSpan.FromDays(7),
            Path = "/tools/video-trimmer"
        });
        return ownerId;
    }
}
