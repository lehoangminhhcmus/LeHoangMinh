Bản 0.4.0 cho Windows 10/11 x64 bổ sung **chuẩn hiệu suất đỉnh toàn phần và hoạt độ**:

- Khai báo nguồn/chứng chỉ, hoạt độ Bq, độ không đảm bảo và hệ số phủ k, ngày quy chiếu có múi giờ, bán rã và dữ liệu vạch gamma.
- Ghép ROI với vạch chuẩn; hiệu chỉnh phân rã tới ngày đo và trong thời gian đếm; K diện tích có căn cứ nếu cần.
- Fit GLS ln(ε) theo ln(E/1000), bậc 1–3; hiển thị sai số, phần dư, χ²/ν và giữ tương quan sai số chung của chứng chỉ/dữ liệu hạt nhân.
- Lưu JSON đầy đủ hoặc bản nháp để gom điểm từ nhiều phổ; xuất TXT có covariance và nguồn dữ liệu. Không ghi hiệu suất vào CNF.
- Tính Bq và Bq/kg theo ROI, hiệu chỉnh ngày quy chiếu, K/sai số do người dùng cung cấp; giữ covariance chung Pγ/bán rã với bộ chuẩn. Chặn sai hình học, phổ tái dùng, ngoại suy và bộ chuẩn/ROI chất lượng kém.

Bấm **Hiệu suất / Hoạt độ…** sau khi mở phổ và fit ROI. Không kèm đường chuẩn hay số liệu nguồn thực tế: cần nhập từ chứng chỉ, dữ liệu hạt nhân và phép đo nguồn của bạn. Hướng dẫn đầy đủ trong README.

Tải `GammaCNFReader-CSharp-Setup.exe` để cài/cập nhật, hoặc giải nén ZIP chạy ngay. Không cần Python hoặc cài .NET riêng.

Kiểm tra: 105 kiểm tra với file CNF riêng và dữ liệu hiệu suất giả lập; CI chạy 96 kiểm tra chung cùng smoke test WPF Windows (ROI, đường hiệu suất, JSON, đồ thị và hoạt độ).

Giới hạn: bản phát hành chưa ký số, chưa xác nhận hiệu suất/hoạt độ bằng nguồn chuẩn thực tế/Genie 2000. Không tự tính tự hấp thụ/trùng phùng tổng, không tự nhận diện đồng vị/gộp vạch; một Gaussian chưa tách đỉnh chồng. Sai số tuyến tính chỉ gồm các thành phần đã nhập; cần kiểm chứng độc lập trước khi dùng chính thức.
