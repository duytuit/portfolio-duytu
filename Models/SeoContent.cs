namespace PortfolioMvc.Models;
public record ContentPage(string Slug, string Title, string Description, string Stack, string Image);
public record BlogSection(string Id, string Title, string Text, string? Code = null);
public record BlogPost(string Slug, string Title, string Description, string Author, string Published, string Updated, BlogSection[] Sections);
public static class SeoContent {
public const string Origin = "https://dextech.vn";
public static readonly ContentPage[] Projects = {new("logistics-management-platform","LOGISTICS MANAGEMENT PLATFORM","Nền tảng quản lý hoạt động logistics, vận chuyển, công nợ, thu chi và quy trình vận hành doanh nghiệp.",".NET · REACT · SQL SERVER · REDIS","/img/services-1000.webp"),
new("manufacturing-management-system","MANUFACTURING MANAGEMENT SYSTEM","Hệ thống hỗ trợ quản lý sản xuất, kế hoạch, dữ liệu sản phẩm và các quy trình vận hành trong nhà máy.",".NET · SQL SERVER · ORACLE · WINFORMS","/img/editorial/collaboration-900.webp"),
new("work-management-platform","WORK MANAGEMENT PLATFORM","Nền tảng quản lý công việc giúp doanh nghiệp tổ chức task, checklist, tài liệu, trao đổi, lịch sử hoạt động và quy trình làm việc.","ASP.NET CORE · REACT · SQL SERVER · REDIS","/img/editorial/code-900.webp"),
new("real-time-data-platform","REAL-TIME DATA PLATFORM","Hệ thống thu thập, xử lý và phân phối dữ liệu theo thời gian thực với background services và WebSocket.",".NET · WEBSOCKET · SQL SERVER · REDIS","/img/editorial/laptop-600.webp")};
public static readonly ContentPage[] Services = {new("software-engineering","SOFTWARE ENGINEERING","Thiết kế và phát triển các ứng dụng có khả năng vận hành ổn định, mở rộng và bảo trì lâu dài.",".NET · C# · ASP.NET CORE · REST API",""),
new("web-development","WEB DEVELOPMENT","Xây dựng web application và những trải nghiệm web hiện đại, nhanh và responsive.","REACT · JAVASCRIPT · RAZOR · HTML · CSS",""),
new("business-systems","BUSINESS SYSTEMS","Phát triển phần mềm dựa trên quy trình vận hành thực tế của doanh nghiệp.","LOGISTICS · MANUFACTURING · WORKFLOW · ERP",""),
new("ai-automation","AI & AUTOMATION","Khám phá và ứng dụng AI để tự động hóa những công việc lặp lại và tạo ra những sản phẩm thông minh hơn.","AI · LLM · RAG · AUTOMATION","")};
public static readonly BlogPost[] Posts = {
new("cau-truc-du-an-va-dich-vu", "Cấu trúc dự án và dịch vụ trong portfolio Duy Tú", "Khám phá các nhóm nội dung trong portfolio Duy Tú: logistics, sản xuất, quản lý công việc, dữ liệu thời gian thực và dịch vụ xây dựng ứng dụng phần mềm.", "Duy Tú", "2026-10-23", "2026-10-23", new BlogSection[] {
new("projects", "Bốn nhóm dự án", "Portfolio giới thiệu nền tảng quản lý logistics, hệ thống quản lý sản xuất, nền tảng quản lý công việc và hệ thống dữ liệu thời gian thực. Mỗi nhóm mô tả một phạm vi vận hành khác nhau, từ công nợ và kế hoạch đến task, tài liệu và phân phối dữ liệu."),
new("technologies", "Các công nghệ được giới thiệu", "Các nhóm dự án sử dụng .NET, ASP.NET Core, React, SQL Server, Redis, Oracle, WinForms và WebSocket theo nội dung đã công bố trong portfolio. Danh sách công nghệ của từng nhóm được ghi riêng tại trang chi tiết dự án."),
new("services", "Phạm vi dịch vụ", "Nội dung dịch vụ gồm software engineering, web development, business systems và AI automation. Trang dịch vụ giới thiệu phạm vi và công nghệ; biểu mẫu liên hệ dùng để trao đổi bài toán cụ thể trước khi xác định giải pháp."),
new("illustrations", "Phân biệt ảnh minh họa và sản phẩm", "Ảnh trong danh mục dự án là ảnh minh họa. Chúng giúp trình bày nội dung và không được dùng làm bằng chứng về giao diện sản phẩm, khách hàng hay kết quả triển khai. Các mô tả không bổ sung khách hàng hoặc số liệu ngoài nội dung hiện có.") }),
new("portfolio-aspnet-core-mvc", "Portfolio render HTML bằng ASP.NET Core MVC", "Tìm hiểu cách portfolio Duy Tú dùng ASP.NET Core MVC, Razor và JavaScript để render nội dung trên server, tổ chức giao diện và giữ tương tác phía trình duyệt.", "Duy Tú", "2026-10-23", "2026-10-23", new BlogSection[] {
new("server-rendering", "Render nội dung trên server", "Website sử dụng ASP.NET Core MVC. Controller chọn Razor View và máy chủ trả HTML cho trình duyệt. Nội dung chính không phụ thuộc vào việc thực thi React ở phía client."),
new("routing", "Tổ chức route và Razor View", "Các URL được khai báo bằng attribute routing. Layout dùng chung chứa navigation, metadata và các script; partial view chia nội dung thành những phần như dự án, dịch vụ và liên hệ.", "[HttpGet(\"/\")] public IActionResult Index() => View();"),
new("interaction", "JavaScript bổ sung tương tác", "GSAP điều khiển animation, AOS xử lý hiệu ứng xuất hiện khi cuộn. Nội dung vẫn nằm trong HTML ban đầu; JavaScript bổ sung menu, theme và các hiệu ứng thay vì tạo nội dung chính."),
new("explore", "Khám phá nội dung portfolio", "Xem các dự án và dịch vụ để tìm hiểu phạm vi công nghệ được giới thiệu trong portfolio. Trang liên hệ cung cấp biểu mẫu trao đổi về nhu cầu phần mềm.") })
};
}
