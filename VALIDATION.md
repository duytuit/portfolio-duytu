# Kiểm tra bản chuyển đổi

- .NET SDK 6.0.428: build thành công, 0 lỗi, 0 cảnh báo (bản cuối).
- `python3 tests/smoke.py http://127.0.0.1:5000`: 75 kiểm tra đạt; 16 đường dẫn trang, tài nguyên cục bộ, 404, chống CSRF, validation, lưu form và thông báo sau redirect.
- Chromium + Playwright: menu mobile mở/đóng, FAQ mở, không có lỗi JavaScript; không tràn ngang ở viewport 390×844 và 1440×900.
- `tests/browser-animations.cjs`: AOS xuất hiện khi cuộn, GSAP hero thay đổi vị trí, không chạy trùng CSS animation; reduced motion khi tải và khi thay đổi; nội dung vẫn hiện khi thư viện bị chặn hoặc tắt JavaScript. Menu mobile và không tràn ngang cũng được kiểm tra.
- CSS logo clients chạy bằng animation cục bộ; hover tạm dừng, hỗ trợ reduced motion.
- Chưa kiểm tra triển khai IIS, hosting production, gửi email hay khả năng tải font/Google Maps trong mạng của người dùng.

Mã nguồn MVC và assets độc lập với React. Các file JSX trong archive chỉ được dùng để lấy nội dung/giao diện khi chuyển đổi; không cần tại runtime.
