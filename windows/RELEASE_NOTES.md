# Gamma CNF Reader v0.9.0 — đa thức hiệu suất bậc 1–6 và đồ thị chuẩn

Windows 10/11 x64, macOS 14+ Apple Silicon và Intel.

- Ngưỡng diện tích điểm chuẩn <3σ, χ²/ν ROI >3, χ²/ν đường chuẩn >3 và phần dư
  vượt 3σ trở thành cảnh báo. Vẫn cho fit và tính hoạt độ; cảnh báo theo kết quả
  và báo cáo TXT để người dùng kiểm tra trước khi sử dụng.
- Chọn đa thức hiệu suất bậc 1–6, cần ít nhất bậc+2 năng lượng khác nhau.
  Chuẩn hóa biến khi fit GLS để ổn định số học; kiểm tra cực trị trong toàn miền
  năng lượng để ngăn hiệu suất dự đoán vượt 1 giữa các điểm chuẩn.
- Thêm đồ thị năng lượng theo kênh trong tab Hiệu chuẩn, cập nhật theo phổ và
  hệ số đã áp dụng. Ghi CNF tiếp tục dùng bốn hệ số a0–a3 đã kiểm chứng.
- Đồ thị hiệu suất có sai số điểm, hai biên sai số mô hình ±1σ trong không gian
  log và phần dư; điểm có cảnh báo chất lượng được tô đỏ.
- Giữ dữ liệu nhập gọn v0.8.0, đọc JSON schema 1/2 và tính lại từ điểm gốc.
  Dữ liệu sai vật lý, fit suy biến, hình học không khớp và ngoại suy vẫn bị từ chối.

Kiểm tra tại môi trường Linux: 196 kiểm tra dữ liệu đạt; build WPF Windows đạt;
smoke test giao diện Avalonia đạt và xuất được ảnh đồ thị năng lượng/hiệu suất.
Workflow phát hành tiếp tục kiểm tra WPF và đóng gói Windows/macOS trên máy native.

[Hướng dẫn sử dụng](https://github.com/lehoangminhhcmus/LeHoangMinh/blob/main/docs/Chuan_hieu_suat_nhap_gon_v0.8.0.md).
