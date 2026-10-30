# Chuẩn hiệu suất nhập gọn — v0.8.0

Áp dụng cho Windows và macOS (Apple Silicon/Intel). Đây là thay đổi so với hướng
dẫn Word v0.7.0; không cần điền đầy đủ chứng chỉ/hình học để thêm điểm hiệu suất.

## Cách dùng nhanh

1. Mở phổ, kiểm tra chuẩn năng lượng, chọn hoặc dò ROI rồi kiểm tra Gaussian fit.
2. Bấm **Hiệu suất / Hoạt độ…**. Live time và ROI lấy từ phổ.
3. Nhập **hoạt độ nguồn tại đầu phép đo (Bq)** và **xác suất phát gamma (%)**.
   Năng lượng để trống sẽ dùng tâm ROI; có thể nhập năng lượng vạch chính xác.
4. Bấm **Thêm ROI đã chọn làm điểm chuẩn**. Lặp cho các vạch tiếp theo.
5. Chọn đa thức hiệu suất bậc 1–6. Cần ít nhất bậc+2 năng lượng khác nhau
   (bậc 1 cần 3; bậc 6 cần 8), tối đa 500 điểm.
   **Fit đường chuẩn**, kiểm tra đường cong/phần dư, rồi **Lưu bộ chuẩn JSON…**.

Công thức cơ bản: `ε = N_Gaussian / (A_đầu_đo × live_time × Pγ_fraction)`.
Phần mềm tự chia giá trị % cho 100; nhập Pγ = `85.1` khi xác suất là 85,1%.
Không chia diện tích ROI cho thời gian trước khi nhập. Tổng số đếm ROI còn chứa
nền liên tục nên không thay thế diện tích Gaussian.

**Hoạt độ ở chế độ mặc định phải đã quy đổi về đầu phép đo.** Nếu bán rã để trống,
không áp dụng hiệu chỉnh phân rã trong phép đo; điều này phải phù hợp bài toán.
Không dùng hoạt độ cũ trên chứng chỉ làm A tại đầu đo nếu chưa quy đổi.

## Khi nào cần thêm thông tin?

- **Tự quy đổi A từ ngày chứng nhận**: chọn ô này, nhập A tại ngày chứng nhận,
  ngày chứng nhận và đầu đo có múi giờ, bán rã theo ngày, real time. Phần mềm
  tự giảm hoạt độ và tính biến thiên trong phép đo. Các tên/chứng chỉ vẫn tùy chọn.
- Đã có A tại đầu đo nhưng muốn hiệu chỉnh phân rã trong phép đo: mở phần bổ sung,
  nhập bán rã và real time; không cần nhập ngày chứng nhận.
- Chứng chỉ, mã nguồn, đồng vị, detector, hình học, khoảng cách, ghi chú và tham
  chiếu dữ liệu có thể để trống. Bỏ trống khoảng cách được lưu là **chưa cung cấp**,
  khác với nhập 0 mm. Hình học phù hợp vẫn là điều kiện vật lý khi áp dụng chuẩn;
  việc cho bỏ trống thông tin không chứng minh hai phép đo cùng điều kiện.
- Sai số hoạt độ, bán rã, Pγ, K có thể để trống. Sai số trống được ghi là **chưa
  cung cấp**, không phải kết luận sai số thật bằng 0. Fit vẫn có trọng số từ sai
  số diện tích ROI; khoảng ± chỉ gồm thành phần có dữ liệu.
- Khi có sai số hoạt độ trên chứng chỉ (%), nhập k tương ứng. k trống = 1,
  nghĩa là coi số nhập đã là 1σ; nếu chứng chỉ cho U với k=2 phải nhập k=2.
- Sai số Pγ nhập theo **điểm phần trăm**, không phải phần trăm tương đối.
- K trống = 1. Nếu nhập K, nên ghi căn cứ và sai số; ghi chú trống không chặn tính,
  nhưng được báo thiếu trong TXT/JSON. Không tự tính hiệu chỉnh tự hấp thụ/trùng phùng.
- Không có mã nguồn: dùng mã nội bộ và coi sai số hoạt độ giữa các điểm độc lập.
  Không có đồng vị/tham chiếu: không tự gán tương quan Pγ giữa các điểm. Khi cần
  ngân sách sai số đầy đủ/tương quan đúng, hãy bổ sung các nhận dạng tương ứng.

Ô có số nhập nhưng sai định dạng, âm, hoạt độ bằng 0, Pγ ngoài (0,100%], ROI fit
không hợp lệ hoặc thời gian không hợp lý vẫn bị từ chối. Không thể bỏ các đầu vào
thực sự cần cho phép tính đã chọn; các mục tùy chọn trống không gây lỗi.

## Lưu và dùng lại

JSON mới dùng **schema 2**, ghi rõ A ở đầu đo hay ở ngày chứng nhận, các thông tin
còn thiếu và các giả định. v0.8.0 đọc được JSON schema 1 của v0.7.0 mà giữ đúng công
thức cũ. Không dùng JSON nhập gọn schema 2 trong ứng dụng v0.7.0.

Báo cáo TXT có `Input_completeness`, `Activity_basis`, `Missing_inputs`; ngày/real
time/khoảng cách chưa có được ghi `NA`. Sau sửa ROI/phông/năng lượng, xóa và thêm
lại các điểm đã nhập vì chúng là bản chụp tại lúc thêm. Kiểm chứng đường chuẩn
bằng phép đo độc lập trước khi dùng định lượng chính thức.

Các chức năng hoạt độ và điều kiện chất lượng ROI/đường chuẩn vẫn giữ như trước;
những dữ liệu cần để quy đổi hoạt độ về ngày khác vẫn phải được cung cấp.

## Cập nhật tiếp theo từ v0.8.0

- Diện tích điểm chuẩn <3σ, χ²/ν ROI >3, χ²/ν đường chuẩn >3 hoặc phần dư
  vượt 3σ được hiển thị thành cảnh báo. Vẫn cho fit và tính hoạt độ; cảnh báo
  đi kèm kết quả và báo cáo TXT. Cần kiểm tra ý nghĩa vật lý trước khi dùng kết quả.
- Đa thức **hiệu suất** bậc 1–6 dùng GLS trên ln ε theo ln(E/1000 keV).
  Fit dùng biến được dịch và chuẩn hóa để ổn định số học; hệ số và covariance
  xuất ra vẫn theo công thức trên. JSON cũ được mở và tính lại từ điểm gốc.
- Tab **Hiệu chuẩn** có đồ thị năng lượng theo kênh cho hệ số đang áp dụng.
  Nhập hệ số rồi bấm **Áp dụng hệ số** để cập nhật. Khi đổi phổ, đồ thị theo phổ
  đang chọn. Định dạng ghi CNF vẫn dùng bốn hệ số a0–a3 đã được kiểm chứng.
- Tab **Đường chuẩn và phần dư** hiển thị hiệu suất trên trục log, sai số điểm
  ±1σ, đường GLS, hai biên sai số mô hình ±1σ trong không gian log và phần dư.
  Điểm có cảnh báo chất lượng được tô đỏ. Cả Windows và macOS dùng cùng cách tính.
- Dữ liệu không hợp lệ, fit suy biến, hiệu suất ngoài (0,1], hình học khác nhau,
  ROI trùng/chồng và ngoại suy vẫn bị từ chối.
