Bản 0.5.0 cho Windows 10/11 x64 bổ sung:

- **Nhiều phổ đang mở:** mở/chọn/kéo thả nhiều CNF; nút Phổ trước/Phổ sau, danh sách và Ctrl+Tab để chuyển. Mỗi phổ giữ riêng ROI, hiệu chuẩn/các ô chưa áp dụng, phông, chế độ thời gian, log/keV và zoom. Đóng từng phổ; kiểm tra hiệu chuẩn chưa lưu ở mọi phổ khi thoát.
- **Tự scale thời gian phông:** chọn phông sẽ tự áp dụng α=live mẫu/live phông; có thể chọn real time một cách rõ ràng. Mỗi phổ mẫu tính tỷ số riêng và hiển thị thời gian phông gốc → mục tiêu. Không sửa CNF phông hoặc tự dùng α=1 khi thiếu thời gian.
- **Tổng số đếm ROI:** hiển thị/xuất tổng gốc, phông đã scale và tổng net ±1σ bên cạnh diện tích Gaussian. Có tổng ngay cả khi ROI không fit được. Tổng trực tiếp chưa trừ nền liên tục, khác diện tích đỉnh Gaussian.

Giữ các chức năng chuẩn năng lượng, chuẩn hiệu suất, hoạt độ/Bq/kg, CSV/TXT/PNG của bản trước. Bộ chuẩn hiệu suất dùng chung trong phiên; hiệu chuẩn/phông/ROI giữ riêng từng phổ.

Tải `GammaCNFReader-CSharp-Setup.exe` để cài/cập nhật, hoặc giải nén ZIP chạy ngay. Không cần Python hoặc cài .NET riêng.

Kiểm tra: 127 kiểm tra dữ liệu với CNF riêng/dữ liệu giả lập; CI chạy 118 kiểm tra chung và smoke test WPF Windows, gồm chuyển nhiều phổ và tự scale phông theo thời gian từng mẫu.

Giới hạn: chưa ký số; hiệu suất/hoạt độ chưa xác nhận bằng nguồn chuẩn thực tế/Genie 2000. Không tự nhận diện đồng vị, tách đỉnh chồng hoặc tính hiệu chỉnh tự hấp thụ/trùng phùng tổng. Các tổng ROI còn chứa nền liên tục. ROI/phổ đang mở chỉ giữ trong phiên; xuất báo cáo và lưu hiệu chuẩn cần thiết trước khi đóng.
