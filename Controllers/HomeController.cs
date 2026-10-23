using Microsoft.AspNetCore.Mvc;
using PortfolioMvc.Models;
using PortfolioMvc.Services;
namespace PortfolioMvc.Controllers;
public class HomeController : Controller {
    private readonly SubmissionStore store;
    private readonly TelegramNotifier telegram;
    public HomeController(SubmissionStore store, TelegramNotifier telegram) { this.store = store; this.telegram = telegram; }
    [HttpGet("/")] public IActionResult Index() => View();
    [HttpGet("/hireus")] public IActionResult HireUs() => RedirectPermanent("/contact");
    [HttpGet("/app-development")] public IActionResult AppDevelopment() => RedirectPermanent("/services/software-engineering");
    [HttpGet("/web-development")] public IActionResult WebDevelopment() => RedirectPermanent("/services/web-development");
    [HttpGet("/seo-optimization")] public IActionResult Seo() => RedirectPermanent("/services");
    [HttpGet("/campagin-creation"), HttpGet("/campaign-creation")] public IActionResult Campaign() => RedirectPermanent("/services");
    [HttpGet("/portfolio/{id}")] public IActionResult Project(string id) {
        var ids = new[]{"app1","product1","brand1","book1","app2","book3","product3","book2","app3"};
        return ids.Contains(id) ? RedirectPermanent("/projects") : NotFound();
    }
    [HttpPost("/contact"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact(ContactForm form) {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (await telegram.SendContact(form))
            TempData["Notice"] = "Đã gửi lời nhắn thành công. Cảm ơn bạn!";
        else
            TempData["Error"] = "Không gửi được lời nhắn. Vui lòng thử lại hoặc liên hệ qua Zalo/Messenger.";
        return LocalRedirect(SafeReturn(form.ReturnPath));
    }
    [HttpPost("/newsletter"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Newsletter(NewsletterForm form) {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        await store.SaveSubscriber(form);
        TempData["Notice"] = "Your newsletter registration has been saved.";
        return LocalRedirect(SafeReturn(form.ReturnPath));
    }
    private string SafeReturn(string path) => Url.IsLocalUrl(path) ? path : "/";
    [Route("/error")] public IActionResult Error() => Problem("Unable to process this request.");
}
