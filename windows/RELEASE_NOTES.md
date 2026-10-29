# Gamma CNF Reader v0.8.0 — chuẩn hiệu suất nhập gọn

Windows 10/11 x64, macOS 14+ Apple Silicon và Intel; miễn phí như trước.

- Mặc định chỉ chọn ROI đã fit, nhập **hoạt độ tại đầu phép đo (Bq)** và **Pγ (%)**.
  Live time từ CNF; năng lượng bỏ trống dùng tâm ROI. Cơ bản ε=N/(A×t×Pγ).
- Thu gọn các phần thông tin bổ sung; chứng chỉ, mã nguồn, đồng vị, hình học,
  khoảng cách, tham chiếu và sai số không còn bắt buộc để tính hiệu suất.
- Chọn **Tự quy đổi A từ ngày chứng nhận** nếu cần; lúc đó chỉ yêu cầu thêm ngày
  có múi giờ, bán rã và real time. Không nhập bán rã nghĩa là chưa hiệu chỉnh phân
  rã trong phép đo; A ở chế độ cơ bản phải do người dùng quy đổi về đầu đo.
- K bỏ trống =1; sai số bỏ trống ghi **chưa cung cấp**. Khoảng ± chỉ gồm các
  thành phần đã có, không được coi là ngân sách sai số đầy đủ. Bỏ trống nhận dạng
  thì không tự gán tương quan nguồn/Pγ không có căn cứ.
- JSON schema 2 bảo toàn chế độ tính và thông tin còn thiếu; đọc tương thích
  schema 1 của v0.7.0. Không mở schema 2 bằng bản cũ. TXT ghi thêm chế độ hoạt độ
  và thông tin chưa cung cấp; sửa dòng phiên bản báo cáo về 0.8.0.
- Giữ kiểm tra số học/vật lý: hoạt độ và live time dương, Pγ hợp lệ, ROI fit đạt,
  đủ điểm/đúng miền năng lượng và chất lượng fit. Hình học phù hợp vẫn là điều kiện
  vật lý; bỏ trống tên không chứng minh phép đo khớp bộ chuẩn.

[Hướng dẫn nhập gọn](https://github.com/lehoangminhhcmus/LeHoangMinh/blob/main/docs/Chuan_hieu_suat_nhap_gon_v0.8.0.md).
Hướng dẫn Word v0.7.0 mô tả luồng cũ; xem hướng dẫn nhập gọn cho những thay đổi trên.

Kiểm tra: 179 kiểm tra dữ liệu với phổ riêng, 168 kiểm tra chung khi không có
phổ riêng; smoke test WPF và macOS native gồm luồng đầy đủ và luồng chỉ có đầu
vào tối thiểu, lưu/đọc JSON và các chức năng phổ/ROI hiện có.

Mac: tải đúng DMG, kéo app vào Applications. Chưa ký Apple Developer ID/notarize;
nếu bị chặn lần đầu, dùng Privacy & Security → Open Anyway cho riêng app. Windows
chưa ký số nhà phát hành. Kiểm chứng kết quả với nguồn chuẩn độc lập trước khi
sử dụng chính thức; chưa xác nhận bằng nguồn chuẩn thực tế/Genie 2000.
