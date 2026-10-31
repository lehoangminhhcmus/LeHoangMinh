# Gamma CNF Reader v0.10.2 — thư viện đồng vị tích hợp

- Nhúng nguyên file `Stdlib.nlb` được cung cấp: 109 đồng vị, 511 vạch.
- Tự nạp Stdlib khi mở lần đầu trên Windows/macOS, dùng ngoại tuyến và không cần
  chép file hoặc chọn thư mục thư viện.
- Nút **Thư viện tích hợp** trong tab **Gợi ý đồng vị** chuyển về Stdlib; vẫn hỗ trợ
  chọn thư mục NLB/CSV riêng và nhớ lựa chọn. Khi thư mục đã lưu không nạp được,
  thông báo và dùng Stdlib cho phiên hiện tại.
- Giữ vạch đối chiếu xanh lá của v0.10.1. NLB vẫn được ghi rõ chưa xác minh nguồn
  dữ liệu hạt nhân/ngày cập nhật; gợi ý theo năng lượng chưa xác nhận đồng vị.

Kiểm chứng Linux: 237 kiểm tra chung, gồm đọc thư viện nhúng và các vạch Co-60,
Cs-137, Am-241, Na-22, K-40; build Windows và smoke test Avalonia headless.
Build bộ cài Windows và DMG/macOS native do GitHub Actions kiểm chứng khi phát hành.
