Bản 0.3.0 cho Windows 10/11 x64 bổ sung:

- **Hiệu chuẩn năng lượng:** nhập a0–a3; áp dụng và lưu vào CNF cũ hoặc file mới. Ghi đè có xác nhận và sao lưu `.bak`; giữ số đếm gốc. Chỉ ghi layout ACQP POLY/keV đã kiểm chứng. Các kết quả phân tích cũ trong CNF không được tính lại.
- **Trừ phông CNF:** chuẩn hóa theo tỷ số thời gian sống hoặc α thủ công; kiểm tra tương thích kênh/hiệu chuẩn; giữ net âm và lan truyền variance.
- **ROI/Gaussian:** Shift+kéo hoặc nhập kênh để thêm ROI; fit một Gaussian + nền tuyến tính; diện tích ± 1σ, tâm, FWHM theo kênh/keV, χ²/ν; xuất tất cả ROI (kể cả lỗi) ra TXT có thông tin phép tính.

Tải `GammaCNFReader-CSharp-Setup.exe` để cài/cập nhật, hoặc giải nén ZIP chạy ngay. Không cần Python hoặc cài .NET riêng.

**Kiểm tra:** 58 kiểm tra dữ liệu với file mẫu riêng tại môi trường phát triển. CI chạy 49 kiểm tra chung, smoke test WPF trên Windows và tạo bộ cài; không đưa file mẫu người dùng vào repository.

**Giới hạn:** prerelease chưa ký số; một Gaussian không tách đỉnh chồng, WLS/sai số chỉ là xấp xỉ với thống kê thấp. Chưa đối chiếu với Genie 2000 hoặc kiểm thử thủ công cài/gỡ trên PC. CNF chỉ lưu hiệu chuẩn; phổ net và ROI xuất CSV/TXT.
