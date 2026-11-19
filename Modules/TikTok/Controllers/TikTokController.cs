using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using PortfolioMvc.Modules.TikTok.Models;
using PortfolioMvc.Modules.TikTok.Services;

namespace PortfolioMvc.Modules.TikTok.Controllers;

[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class TikTokController : Controller
{
    private const string OwnerCookie = "tiktok-owner";
    private readonly TikTokDownloadService downloader;
    private readonly TikTokJobQueue queue;
    private readonly TikTokJobStore store;

    public TikTokController(TikTokDownloadService downloader, TikTokJobQueue queue, TikTokJobStore store)
    { this.downloader = downloader; this.queue = queue; this.store = store; }

    [HttpGet("/tiktok")]
    public IActionResult Index() => View("~/Modules/TikTok/Views/Index.cshtml", store.History(GetOwnerId()));

    [HttpPost("/tiktok/info")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Info([FromForm] string? url, [FromForm] bool rights, CancellationToken cancellationToken)
    {
        if (!rights) return BadRequest(new { error = "Chỉ tiếp tục với video bạn sở hữu hoặc được phép sử dụng." });
        if (!TikTokDownloadService.IsValidTikTokUrl(url)) return BadRequest(new { error = "Chỉ chấp nhận URL HTTPS thuộc tên miền TikTok." });
        try { return Json(await downloader.GetInfoAsync(url!, cancellationToken)); }
        catch (TikTokToolException exception) { return BadRequest(new { error = exception.Message }); }
    }

    [HttpPost("/tiktok/download")]
    [ValidateAntiForgeryToken]
    public IActionResult Download([FromForm] string? url, [FromForm] bool rights,
        [FromForm] bool removeWatermark, [FromForm] double regionX = 0, [FromForm] double regionY = 0,
        [FromForm] double regionWidth = 0, [FromForm] double regionHeight = 0)
    {
        if (!rights) return BadRequest(new { error = "Chỉ tiếp tục với video bạn sở hữu hoặc được phép sử dụng." });
        if (!TikTokDownloadService.IsValidTikTokUrl(url)) return BadRequest(new { error = "Chỉ chấp nhận URL HTTPS thuộc tên miền TikTok." });
        try
        {
            var region = removeWatermark
                ? new ManualWatermarkRegion { X = regionX, Y = regionY, Width = regionWidth, Height = regionHeight }
                : null;
            var state = downloader.CreateJob(url!, GetOwnerId(), removeWatermark, region, queue, store);
            return Accepted(new { id = state.Job.Id, status = state.Job.Status });
        }
        catch (TikTokToolException exception) { return Conflict(new { error = exception.Message }); }
    }

    [HttpGet("/tiktok/status/{id:guid}")]
    public IActionResult Status(Guid id) => store.Get(id, GetOwnerId()) is { } job ? Json(job) : NotFound();

    [HttpPost("/tiktok/cancel/{id:guid}")]
    [ValidateAntiForgeryToken]
    public IActionResult Cancel(Guid id) => store.Cancel(id, GetOwnerId())
        ? Ok(new { status = "Cancelling" }) : Conflict(new { error = "Không thể hủy tác vụ này." });

    [HttpGet("/tiktok/video/{id:guid}")]
    public IActionResult Video(Guid id)
    {
        var download = downloader.GetDownload(id, GetOwnerId(), store);
        return download is null ? NotFound() : PhysicalFile(download.Value.Path, "video/mp4", download.Value.Name);
    }

    [HttpGet("/tiktok/history")]
    public IActionResult History() => Json(store.History(GetOwnerId()));

    private string GetOwnerId()
    {
        if (Request.Cookies.TryGetValue(OwnerCookie, out var id) && id is { Length: 43 } &&
            id.All(character => char.IsLetterOrDigit(character) || character is '-' or '_')) return id;
        id = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        Response.Cookies.Append(OwnerCookie, id, new CookieOptions
        {
            HttpOnly = true, Secure = Request.IsHttps, SameSite = SameSiteMode.Strict,
            IsEssential = true, MaxAge = TimeSpan.FromDays(7), Path = "/tiktok"
        });
        return id;
    }
}
