# SEO dextech.vn

## Phạm vi và lỗi xác nhận

Kiểm tra source ASP.NET Core 6 MVC, controller, Razor views/partials, forms, static assets, CSS, JavaScript và bài kiểm tra sẵn có. Đây là HTML render trên server, không phải React SPA; Googlebot không cần JavaScript để lấy nội dung chính. Không cần thay framework hoặc thêm prerender. Chưa audit server production, DNS, CDN hoặc Google Search Console.

Trước sửa: thiếu canonical, robots.txt, sitemap.xml và JSON-LD; description mặc định giống nhau; social URL lấy từ host request; hai alias campaign đều trả 200. Trang chi tiết dự án cũ có nhiều H1, Lorem ipsum, thông tin khách hàng USA, example.com và ngày tự sinh không có căn cứ. Các route about/projects/services/blog/contact GET chưa tồn tại. Font được import hai lần qua CSS. Hero chưa khai báo kích thước intrinsic; ảnh minh họa chưa có nguồn responsive.

## Thay đổi

- Giữ MVC/Razor, form liên hệ/CSRF/Telegram, menu, theme, responsive, AOS và GSAP. Không thêm ScrollTrigger; source hiện tại không dùng thư viện này. Script vẫn defer, hiệu ứng dựa IntersectionObserver và reduced motion như trước.
- Title trang chủ theo yêu cầu; meta description tiếng Việt 154 ký tự. H1 đúng “Duy Tú – Software Engineer & Full-stack Developer”; slogan tiếng Anh giữ nguyên, chuyển sang phần tử trình bày riêng và giữ selector CSS/GSAP tương ứng. H1 được hiển thị thêm trong hero theo yêu cầu nội dung SEO.
- Metadata riêng, canonical HTTPS dextech.vn không query, OG/Twitter, Person/WebSite, ProfilePage ở about, BreadcrumbList ở trang con, BlogPosting ở bài viết. Serializer mặc định escape JSON để tránh chèn script.
- 16 URL indexable: trang chủ, 5 trang danh mục/thông tin, 4 dự án, 4 dịch vụ, 2 bài blog. Robots và sitemap XML sinh từ catalog chung, không chứa URL redirect, form POST, lỗi hoặc URL chưa biết.
- Hai bài blog mới tổng hợp từ nội dung/source của repo, không phải bài cũ đã được xác nhận xuất bản. Ngày khởi tạo nội dung 2026-10-23, tác giả theo danh tính portfolio. Slug/description/author/published/updated quản lý trong Models/SeoContent.cs; chỉ sửa updated khi thực sự sửa bài. Thêm bài vào catalog sẽ tự xuất hiện ở danh sách, sitemap và bài liên quan. Mục lục tự động từ section ID khi nội dung hơn 700 ký tự; code được Razor encode trong pre/code và có language class.
- Chi tiết dự án/dịch vụ chỉ dùng mô tả và stack hiện có, không bịa case study, khách hàng hoặc KPI. Ảnh ghi rõ minh họa. Các trang chi tiết còn ngắn vì source không có tài liệu triển khai sâu hơn.
- Dùng WebP responsive 400/600/640/900/1000/1536 tùy kích thước thật, không upscale ảnh nhỏ; ảnh dưới fold giữ lazy loading, hero eager với fetchpriority high. Bổ sung dimensions, giữ nguyên file ảnh cũ để tương thích. Font chuyển từ CSS import trùng sang một stylesheet có display=swap, giữ bộ font/weight hiện hành để hạn chế thay đổi thiết kế.
- Bổ sung liên kết danh mục ở footer và liên kết chi tiết từ card. Thứ bậc card đổi sang H3, giữ nguyên class trình bày.

## URL và vận hành

301: /hireus → /contact; /app-development → /services/software-engineering; /web-development → /services/web-development; /seo-optimization, /campagin-creation, /campaign-creation → /services. Các ID /portfolio cũ đã biết → /projects vì nội dung mẫu không đủ căn cứ để ánh xạ dự án thật. Không nhận cung cấp SEO/campaign như dịch vụ mới khi portfolio hiện tại không xác nhận. GET/HEAD URL không extension được chuẩn hóa chữ thường và bỏ slash cuối; query được giữ khi redirect nhưng canonical bỏ query. URL không tồn tại trả 404 thật với X-Robots-Tag: noindex. POST không bị middleware redirect.

Canonical cố định không tin Host header. Production cần cấu hình ở IIS/reverse proxy redirect HTTP và www sang https://dextech.vn, bật nén Brotli/gzip cho text assets, kiểm tra TLS và host bindings. Không bật HTTPS redirect trong ứng dụng khi chưa biết cấu hình TLS termination/forwarded headers của máy chủ. Không đưa dữ liệu cấu hình Telegram hay contact vào metadata.

## Kiểm tra

```powershell
dotnet build --no-restore
dotnet run --no-build --no-launch-profile --urls http://localhost:5085
python tests/seo.py http://localhost:5085
```

Build thành công; HTTP regression kiểm tra toàn bộ URL sitemap, title trang chủ, description 154 ký tự, một H1, canonical, schema parse được, metadata không trùng, liên kết nội bộ, asset, redirect và 404. Kiểm tra CSRF rejection không gửi Telegram hoặc ghi subscriber. tests/smoke.py đã cập nhật để chấp nhận 301 hợp lệ ở URL cũ; chưa chạy bộ smoke đầy đủ vì có gửi form, có thể tạo thông báo Telegram thật. Không thay đổi các service nhận form.

Chưa chạy browser-layout/animations vì môi trường không có Playwright; chưa có ảnh so sánh trực quan. Cần kiểm tra desktop/mobile, menu, theme, hero, reduced motion, animation khi cuộn, form và thông báo trên staging. Không khẳng định giao diện pixel-identical: có H1 và link SEO mới theo yêu cầu, các class thiết kế cũ được giữ.

Sau deploy: mở HTML source không JavaScript, kiểm tra /robots.txt, /sitemap.xml, /about và /blog/portfolio-aspnet-core-mvc; dùng Rich Results Test/Schema Validator rồi gửi sitemap và URL Inspection trong Search Console. Chạy PageSpeed Insights mobile/desktop và đo RUM/CrUX. Chưa có dữ liệu thực địa p75 nên chưa xác nhận LCP ≤2.5s, INP ≤200ms, CLS ≤0.1 hoặc điểm SEO. Structured data hợp lệ cú pháp không đảm bảo Google hiển thị rich result. .NET 6 hiện là ràng buộc của repo; nâng runtime cần công việc tương thích riêng.

## File thay đổi

Controllers/HomeController.cs, Controllers/SeoController.cs; Models/SeoContent.cs; Program.cs; Views/Home/Index.cshtml; Views/Seo/{Landing,Detail,Article}.cshtml; Views/Shared/{_Layout,_Hero,_Footer,_Portfolio,_Services,_Intro}.cshtml; wwwroot/css/{site,interactions}.css; wwwroot/js/site.js; ảnh WebP mới trong wwwroot/img và wwwroot/img/editorial; tests/{seo,smoke}.py; docs/SEO-REPORT.md.
