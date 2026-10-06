# Kiểm tra thiết kế portfolio

- Build project `net6.0` bằng SDK 6.0.428: 0 lỗi, 0 cảnh báo. Lệnh chạy từ `/tmp` để không thay đổi `global.json` do người dùng cập nhật; SDK 10.0.203 đang được ghim trong file đó chưa có trong môi trường kiểm tra.
- `python3 tests/smoke.py http://127.0.0.1:5001`: 62 kiểm tra đạt, gồm các trang, tài nguyên, lưu form, validation, chống CSRF và 404.
- `tests/browser-animations.cjs`: AOS khi cuộn, GSAP trên thẻ hero, thanh tiến trình, navigation theo section, dark mode, menu mobile, không tràn ngang, không có lỗi JavaScript.
- Reduced motion hoạt động khi khởi động và khi thay đổi; nội dung vẫn hiển thị khi thư viện bị chặn hoặc tắt JavaScript.
- Ảnh chụp trình duyệt tại viewport 1440×1000 và 390×844 đã được kiểm tra trực quan.

Hình trên các thẻ dự án là minh họa giao diện bằng HTML/CSS, có nhãn concept/interface study; không phải ảnh chụp sản phẩm thật. Không thay đổi số năm kinh nghiệm hoặc thông tin cá nhân của bản code người dùng cập nhật.
