using Microsoft.AspNetCore.Mvc;
using PortfolioMvc.Models;
using System.Xml.Linq;
namespace PortfolioMvc.Controllers;
public class SeoController : Controller {
[HttpGet("/about")] public IActionResult About() => Landing("Về Duy Tú", "Tìm hiểu Duy Tú, Software Engineer tập trung phát triển phần mềm, hệ thống doanh nghiệp, ứng dụng web và những sản phẩm công nghệ sử dụng trong thực tế.", "about");
[HttpGet("/projects")] public IActionResult Projects() => Landing("Dự án phần mềm", "Khám phá các nền tảng logistics, quản lý sản xuất, quản lý công việc và xử lý dữ liệu thời gian thực trong portfolio phát triển phần mềm của Duy Tú.", "projects");
[HttpGet("/services")] public IActionResult Services() => Landing("Dịch vụ phát triển phần mềm", "Duy Tú giới thiệu dịch vụ phát triển phần mềm .NET, ứng dụng web React, hệ thống doanh nghiệp và AI automation dựa trên nhu cầu vận hành trong thực tế.", "services");
[HttpGet("/contact")] public IActionResult Contact() => Landing("Liên hệ Duy Tú", "Liên hệ Duy Tú để trao đổi ý tưởng sản phẩm, nhu cầu xây dựng ứng dụng web, phần mềm doanh nghiệp và giải pháp tự động hóa phù hợp với quy trình của bạn.", "contact");
[HttpGet("/blog")] public IActionResult Blog() => Landing("Blog lập trình", "Đọc bài viết kỹ thuật về cấu trúc portfolio, ASP.NET Core MVC, Razor và JavaScript; khám phá cách tổ chức nội dung, render HTML và tương tác trên website.", "blog");
IActionResult Landing(string title,string description,string section) { ViewData["Title"]=title+" | Duy Tú"; ViewData["Description"]=description; ViewData["Section"]=section; return View("Landing"); }
[HttpGet("/projects/{slug}")] public IActionResult Project(string slug) => Detail(SeoContent.Projects,slug,"projects");
[HttpGet("/services/{slug}")] public IActionResult Service(string slug) => Detail(SeoContent.Services,slug,"services");
IActionResult Detail(ContentPage[] pages,string slug,string section) { var page=pages.FirstOrDefault(x=>x.Slug==slug); if(page is null)return NotFound(); ViewData["Title"]=page.Title+" | Duy Tú"; ViewData["Description"]=page.Description; ViewData["Section"]=section; return View("Detail",page); }
[HttpGet("/blog/{slug}")] public IActionResult Article(string slug) { var post=SeoContent.Posts.FirstOrDefault(x=>x.Slug==slug); if(post is null)return NotFound(); ViewData["Title"]=post.Title+" | Duy Tú"; ViewData["Description"]=post.Description; ViewData["Post"]=post; ViewData["Section"]="blog"; return View(post); }
[HttpGet("/robots.txt")] public IActionResult Robots() => Content("User-agent: *\nAllow: /\nSitemap: "+SeoContent.Origin+"/sitemap.xml\n", "text/plain; charset=utf-8");
[HttpGet("/sitemap.xml")] public IActionResult Sitemap() {
XNamespace ns="http://www.sitemaps.org/schemas/sitemap/0.9";
var paths=new[]{"/","/about","/projects","/services","/blog","/contact"}.Concat(SeoContent.Projects.Select(x=>"/projects/"+x.Slug)).Concat(SeoContent.Services.Select(x=>"/services/"+x.Slug)).Concat(SeoContent.Posts.Select(x=>"/blog/"+x.Slug));
return Content(new XDocument(new XElement(ns+"urlset",paths.Select(p=>new XElement(ns+"url",new XElement(ns+"loc",SeoContent.Origin+p))))).ToString(),"application/xml; charset=utf-8"); }
}
