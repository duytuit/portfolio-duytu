# Portfolio — ASP.NET Core 6 MVC

Chuyển từ ReactJS trong `portfolio-duytu.rar` sang MVC: Controller C#, Razor Views, CSS biên dịch sẵn và JavaScript thuần. Không cần React, Vite hay Node.js để chạy.

## Chạy ứng dụng

Cài .NET SDK 6.0.428, mở terminal tại thư mục chứa `PortfolioMvc.csproj`:

```sh
dotnet restore
dotnet build
dotnet run
```

Mở `http://localhost:5000`. Có thể mở project bằng Visual Studio 2022.

## Cấu trúc

- `Controllers/HomeController.cs`: trang chủ, dịch vụ, dự án và xử lý form.
- `Models/`: dữ liệu trang và validation form.
- `Views/Home/`: Razor Views; `Views/Shared/`: layout và partial tương ứng component gốc.
- `wwwroot/`: ảnh gốc, CSS Tailwind đã biên dịch, JavaScript menu mobile.
- `Services/SubmissionStore.cs`: lưu liên hệ/đăng ký vào `App_Data/*.jsonl` (không công khai qua web).

Giữ các URL gốc, bao gồm `/campagin-creation`; bổ sung `/campaign-creation`. Dự án không hợp lệ trả 404. FAQ dùng HTML details/summary, menu dùng JavaScript thuần, hiệu ứng cuộn dùng AOS 3.0.0-beta.6, ảnh hero dùng GSAP 3.12.5. Hai thư viện được lưu cục bộ trong `wwwroot/lib/`; không cần CDN. Hỗ trợ reduced motion và giữ nội dung hiển thị khi JavaScript không tải.

Các form có kiểm tra dữ liệu phía máy chủ và chống CSRF. Bản React chỉ console.log và thông báo; bản MVC lưu thông tin thật vào file cục bộ, **chưa gửi email hoặc newsletter**. Dữ liệu là thông tin cá nhân: giới hạn quyền thư mục, sao lưu và đặt chính sách lưu trữ phù hợp khi triển khai. Với nhiều máy chủ, thay file cục bộ bằng database/dịch vụ dùng chung.

Font Google và iframe Google Maps giữ từ bản gốc, cần Internet; các tài nguyên ứng dụng còn lại được phục vụ cục bộ. .NET 6 đã hết hỗ trợ; phiên bản này theo yêu cầu chuyển đổi, nên nâng cấp lên phiên bản được hỗ trợ trước khi triển khai production.

Chạy kiểm tra HTTP sau khi khởi động: `python3 tests/smoke.py http://localhost:5000`. Kiểm tra POST dùng dữ liệu giả và dọn đúng dữ liệu kiểm thử do nó tạo; nên chạy trên bản development riêng.

Kiểm tra animation bằng Playwright (tùy chọn, cần package `playwright` và Chromium trong môi trường kiểm thử): `node tests/browser-animations.cjs`. Có thể đặt `BASE_URL` và `CHROMIUM_EXECUTABLE` để dùng server/trình duyệt đã cài. Các công cụ này không cần để chạy ứng dụng MVC.
