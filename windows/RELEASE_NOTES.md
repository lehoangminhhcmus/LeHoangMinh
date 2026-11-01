# Gamma CNF Reader v0.10.3 — đọc phổ ORTEC CHN

- Mở/kéo thả `.chn` cùng `.cnf` trên Windows/macOS; chọn CHN làm phổ mẫu/phông.
- Đọc UInt32 counts nguyên gốc, live/real time 20 ms, ngày bắt đầu và chuẩn tuyến
  tính/bậc hai. Giữ số đếm ở các kênh đầu/cuối.
- Dùng chung ROI/Gaussian, dò đỉnh, thư viện Stdlib, vạch đối chiếu và xuất CSV/PNG/TXT.
- CHN chỉ đọc; chỉnh chuẩn chỉ áp dụng trong phiên. Khóa ghi hiệu chuẩn CNF cho CHN.
- Nhận header 32 byte, offset kênh 0, 1–32.768 kênh, trailer vắng mặt/256/512 byte.
  Từ chối file thiếu dữ liệu hoặc offset chưa hỗ trợ; cảnh báo khi ngày/chuẩn không hợp lệ.

Kiểm tra: 257 kiểm tra chung (260 khi có file CHN mẫu); build Windows, smoke UI headless
bao gồm CHN, kiểm tra CNF hồi quy. File đo người dùng không được đưa vào gói phát hành.
