Bản 0.6.0 cho Windows 10/11 x64 bổ sung **dò đỉnh và ROI tự động**:

- Nút **Dò đỉnh và tạo ROI tự động** trong tab ROI / Gaussian; áp dụng cho phổ gốc hoặc net sau trừ phông.
- Quét đa độ rộng với ngưỡng SNR và FWHM theo kênh có thể chỉnh; tự ước lượng ROI có nền hai bên, đánh dấu ứng viên bằng vạch cam.
- Tự fit Gaussian và lấy tổng số đếm với ROI đơn; ROI tự động chồng nhau được gom nhóm, không tự định lượng nhóm bằng một Gaussian.
- Giữ ROI thủ công; tùy chọn thay vùng tự động cũ; sửa giới hạn ROI rồi tính lại. TXT có nguồn ROI và thông số dò.
- Chạy nền, có nút Hủy; không đưa kết quả vào nhầm phổ khi chuyển phổ hoặc khi dữ liệu đã thay đổi. Hiệu chuẩn/phông thay đổi sau lần dò sẽ đánh dấu thông tin dò cũ.

Mặc định FWHM 3–40 kênh, SNR 5, biên ROI ±3,5 sigma ước lượng. Cần kiểm tra/chỉnh ROI trước khi định lượng. SNR bộ lọc không phải p-value; chưa nhận diện đồng vị hoặc tách multiplet. Nền cong/cạnh Compton có thể tạo ứng viên giả và đỉnh yếu vẫn có thể bị bỏ sót.

Giữ các chức năng nhiều phổ, tự scale phông, tổng ROI, chuẩn năng lượng, hiệu suất và hoạt độ của bản trước. Tải Setup.exe để cài/cập nhật hoặc giải nén ZIP để chạy ngay; không cần Python hoặc cài .NET riêng.

Kiểm tra: 155 kiểm tra dữ liệu với CNF riêng/dữ liệu giả lập; CI chạy 144 kiểm tra chung và WPF smoke test Windows, gồm dò/fit/chỉnh/giữ ROI, hủy, provenance và xuất ảnh/TXT.

Chưa ký số; hiệu suất/hoạt độ chưa được xác nhận bằng nguồn chuẩn thực tế/Genie 2000. Các tổng ROI còn chứa nền liên tục; cần kiểm chứng kết quả trước khi dùng chính thức.
