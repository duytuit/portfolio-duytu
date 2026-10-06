# Kiểm tra thiết kế portfolio

- Build project `net6.0` bằng SDK 6.0.428: 0 lỗi, 0 cảnh báo. Lệnh chạy từ `/tmp` để không thay đổi `global.json` do người dùng cập nhật; SDK 10.0.203 đang được ghim trong file đó chưa có trong môi trường kiểm tra.
- `python3 tests/smoke.py http://127.0.0.1:5001`: 62 kiểm tra đạt, gồm các trang, tài nguyên, lưu form, validation, chống CSRF và 404.
- `tests/browser-animations.cjs`: AOS khi cuộn, GSAP trên thẻ hero, thanh tiến trình, navigation theo section, dark mode, menu mobile, không tràn ngang, không có lỗi JavaScript.
- Reduced motion hoạt động khi khởi động và khi thay đổi; nội dung vẫn hiển thị khi thư viện bị chặn hoặc tắt JavaScript.
- Ảnh chụp trình duyệt tại viewport 1440×1000 và 390×844 đã được kiểm tra trực quan.

Hình trên các thẻ dự án là minh họa giao diện bằng HTML/CSS, có nhãn concept/interface study; không phải ảnh chụp sản phẩm thật. Không thay đổi số năm kinh nghiệm hoặc thông tin cá nhân của bản code người dùng cập nhật.

## Ảnh minh họa và hiệu ứng chữ

- `tests/browser-typography.cjs`: cỡ chữ hero 72px ở viewport 1440px, 35.2px ở viewport 390px; nội dung 16px như bản trước. Animation theo từng từ giữ nguyên nội dung và xuống dòng; reduced motion có thể bật/tắt trực tiếp.
- Năm liên kết ảnh Unsplash được thêm vào. Môi trường trả HTTP 403 nên chưa xác nhận việc tải ảnh thật. Đã kiểm tra hình dự phòng khi ảnh lỗi và cách hiện ảnh khi tải thành công bằng ảnh mock cục bộ. Nguồn và giới hạn ghi trong `docs/IMAGE-SOURCES.md`.
- Build, 62 kiểm tra HTTP và kiểm tra animation/menu/theme vẫn đạt.
