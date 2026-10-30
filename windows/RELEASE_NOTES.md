# Gamma CNF Reader v0.9.1 — ngưỡng amplitude tối thiểu

- Thêm ô **Amplitude tối thiểu (counts/kênh trên nền)** trong phần dò đỉnh tự động
  trên Windows/macOS. Mặc định 0: không lọc theo chiều cao.
- Amplitude là chiều cao Gaussian ước lượng phía trên nền liên tục tuyến tính,
  tính trên phổ đang dùng (sau trừ phông nếu có), không phải tổng diện tích ROI,
  tổng số đếm tại kênh hoặc giá trị trên trục log.
- Chỉ tạo ROI và fit tự động cho ứng viên có amplitude ≥ ngưỡng, đồng thời đạt
  ngưỡng SNR. Nhập ngưỡng mới rồi bấm dò lại để áp dụng; ROI thủ công giữ nguyên.
- Giữ riêng ngưỡng cho từng phổ; báo cáo TXT ghi ngưỡng, amplitude ước lượng và
  đơn vị để truy nguyên điều kiện dò đỉnh.
- Giữ toàn bộ chức năng v0.9.0: đa thức hiệu suất bậc 1–6, cảnh báo chất lượng,
  đồ thị chuẩn năng lượng và hiệu suất với biên sai số.

Kiểm tra: 207 kiểm tra dữ liệu; smoke test giao diện gồm lọc amplitude, nhập dấu
phẩy thập phân, lưu trạng thái từng phổ và các luồng phân tích hiện có.
