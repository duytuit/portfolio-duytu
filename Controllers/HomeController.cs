using Microsoft.AspNetCore.Mvc;
using PortfolioMvc.Models;
using PortfolioMvc.Services;
namespace PortfolioMvc.Controllers;
public class HomeController : Controller {
    private readonly SubmissionStore store;
    public HomeController(SubmissionStore store) { this.store = store; }
    [HttpGet("/")] public IActionResult Index() => View();
    [HttpGet("/hireus")] public IActionResult HireUs() => View();
    [HttpGet("/app-development")] public IActionResult AppDevelopment() => ServicePage("App Development", "app-1.jpg", "Lorem doloribus ex quisquam. Nesciunt laborum laboriosam consequuntur, eum id molestias in, Nesciunt laborum laboriosam consequuntur, eum id molestias in.");
    [HttpGet("/web-development")] public IActionResult WebDevelopment() => ServicePage("Web Development", "app-3.jpg");
    [HttpGet("/seo-optimization")] public IActionResult Seo() => ServicePage("SEO Optimization", "app-1.jpg");
    [HttpGet("/campagin-creation"), HttpGet("/campaign-creation")] public IActionResult Campaign() => ServicePage("Campaign Creation", "app-2.jpg");
    private IActionResult ServicePage(string title, string image, string description = "Lorem ipsum dolor sit, amet consectetur adipisicing elit. Amet necessitatibus laborum voluptatem at provident atque labore porro ipsam debitis quam, ad dolorem, corrupti aut praesentium minima sit deserunt maiores fuga?") => View("Service", new ServicePage(title, description, "/img/portfolio/" + image));
    [HttpGet("/portfolio/{id}")] public IActionResult Project(string id) {
        var images = new Dictionary<string, string> { ["app1"]="app-1.jpg", ["product1"]="product-1.jpg", ["brand1"]="branding-1.jpg", ["book1"]="books-1.jpg", ["app2"]="app-2.jpg", ["book3"]="books-3.jpg", ["product3"]="product-3.jpg", ["book2"]="books-2.jpg", ["app3"]="app-3.jpg" };
        return images.TryGetValue(id, out var image) ? View(new ProjectPage("Project " + id, "/img/portfolio/" + image)) : NotFound();
    }
    [HttpPost("/contact"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact(ContactForm form) {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        await store.SaveContact(form);
        TempData["Notice"] = "Your message has been saved. Thank you!";
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
