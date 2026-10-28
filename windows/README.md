# Gamma CNF Reader 0.3.0 — C# / WPF

App Windows 10/11 x64 đọc và phân tích phổ gamma Canberra/Genie 2000 `.CNF`. Bản self-contained không cần Python, Genie 2000 hay cài .NET riêng. Mở file, kéo thả hoặc truyền đường dẫn qua dòng lệnh.

## Cài đặt

Tải bộ cài tại [Releases](https://github.com/lehoangminhhcmus/LeHoangMinh/releases). Chạy `GammaCNFReader-CSharp-Setup.exe`, hoặc giải nén ZIP và chạy `GammaCNFReader.exe`. `Install.cmd` cài theo người dùng và tạo shortcut/mục gỡ cài đặt; không cần Administrator. Đóng app trước khi cập nhật/gỡ.

Bản thử nghiệm chưa ký số. Bộ cài cập nhật vào thư mục của bản C# trước đó. Không tự đổi ứng dụng mặc định cho đuôi `.cnf`.

## Nhập và lưu đường chuẩn năng lượng

Tab **Hiệu chuẩn**: nhập a0–a3 cho `E(ch) = a0 + a1·ch + a2·ch² + a3·ch³` (keV), kênh bắt đầu từ 0. Nhập dấu `.` hoặc `,` thập phân, không dùng dấu phân nhóm hàng nghìn. Bấm **Áp dụng hệ số** để cập nhật đồ thị và tất cả ROI. App từ chối hệ số không hữu hạn hoặc đường chuẩn không tăng trên miền kênh.

Bấm **Lưu hiệu chuẩn vào CNF…** rồi chọn lại file gốc để ghi chồng, hoặc chọn tên mới. Khi ghi đè, app xác nhận và tạo bản sao đầy đủ `.bak` với tên riêng, ghi file tạm, rồi thay file nguyên tử. App kiểm tra file gốc chưa bị chỉnh bên ngoài. Hệ số được lượng tử hóa theo số thực CAM 32-bit của CNF và đọc lại sau lưu.

Chỉ ghi trường hệ số/flags/số hệ số trong **ACQP chuẩn POLY/keV có layout đã kiểm chứng** (như file Test_spc.cnf đã cung cấp). Các biến thể khác có thể xem nhưng app sẽ từ chối ghi. **Số đếm và những byte khác giữ nguyên; số đếm sau trừ phông và ROI không được ghi vào CNF. Kết quả phân tích cũ đã có bên trong CNF không được tính lại và có thể không còn phù hợp sau thay hiệu chuẩn.** Cần phân tích lại khi mở trong Genie 2000.

## Trừ phông

Tab **Trừ phông**: chọn một file phông CNF độc lập, giữ chọn tỷ số thời gian sống hoặc nhập α thủ công, rồi bấm **Áp dụng trừ phông**.

- `α = live_mẫu / live_phông` theo mặc định; thiếu live time hợp lệ thì phải nhập α thủ công.
- `net[i] = mẫu[i] − α × phông[i]`.
- `variance[i] = mẫu[i] + α² × phông[i]`, giả định hai phép đo độc lập và α chính xác.
- Giữ nguyên số đếm âm, không làm tròn về uint32 hoặc ép về 0. Trên thang log, chỉ ẩn điểm ≤ 0 khi vẽ.
- Hai phổ phải cùng số kênh và lệch hiệu chuẩn không quá 0,05 kênh trên toàn phổ. Chưa tự rebin hay sửa trôi đỉnh. Nếu thiếu hiệu chuẩn, phải xác nhận cùng cấu hình MCA để trừ theo kênh.
- **Bỏ trừ phông** khôi phục phổ mẫu và tính lại ROI. Khi thay đường chuẩn, app kiểm tra lại tương thích với phông; nếu lệch, bỏ phông trước khi áp dụng.

CSV chứa kênh, keV, số đếm gốc, phông đã chuẩn hóa, số đếm net và sai số. Xử lý tại máy; không gửi file phổ lên dịch vụ ngoài.

## ROI và fit đỉnh

Giữ **Shift + kéo** trên phổ để thêm ROI, hoặc vào tab **ROI / Gaussian** nhập kênh đầu/cuối (bao gồm cả hai đầu). Kéo không Shift để zoom; con lăn zoom quanh con trỏ; Home/Toàn phổ để đặt lại. Mỗi ROI từ 9 đến 4096 kênh, tối đa 100 ROI.

App fit **một Gaussian tích phân theo từng kênh + nền tuyến tính** bằng bình phương tối thiểu có trọng số; trọng số lấy từ variance ở trên, có sàn 1 khi variance bằng 0. Có thể dùng trên phổ mẫu hoặc phổ sau trừ phông. Các thông số:

- Diện tích toàn Gaussian và sai số 1σ xấp xỉ từ ma trận hiệp phương sai của fit.
- Diện tích phần Gaussian nằm trong ROI (trong TXT), khác với tổng net của các kênh còn chứa nền liên tục.
- Vị trí tâm Gaussian theo kênh và keV; sai số tâm theo kênh trong TXT.
- `FWHM = 2√(2 ln 2) × σ` theo kênh; độ rộng keV bằng hiệu năng lượng tại hai điểm nửa chiều cao, phù hợp cả đường chuẩn phi tuyến.
- χ²/ν và trạng thái. χ²/ν > 3 được cảnh báo. Fit không hội tụ, ma trận suy biến, diện tích < 3σ hoặc đỉnh chạm giới hạn ROI được báo không đủ điều kiện, không hiển thị số liệu fit như kết quả hợp lệ.

Vùng ROI được tô nền và mô hình fit có đường đỏ. Thay hiệu chuẩn/phông sẽ tính lại tất cả ROI. **Xuất tất cả ROI ra TXT…** xuất báo cáo UTF-8 phân tách bằng tab, có cả ROI thất bại, đường dẫn mẫu/phông, thời gian sống, α, chế độ chuẩn hóa, hệ số hiệu chuẩn và mô tả phương pháp. ROI chỉ tồn tại trong phiên; xuất TXT trước khi đổi phổ/đóng app.

**Giới hạn:** mô hình đơn Gaussian không tách multiplet/đỉnh chồng, không mô tả tailing phức tạp; fit WLS và sai số chỉ là xấp xỉ, đặc biệt với số đếm thấp. Không tự nhận diện đồng vị hoặc tính hoạt độ. Cần kiểm chứng với phần mềm phân tích chuẩn trước khi dùng kết quả chính thức. Chưa đối chiếu với Genie 2000.

## Phát triển và kiểm tra

Cài .NET 10 SDK x64, chạy trong thư mục repository:

```console
dotnet run --project windows/GammaCnf.App
dotnet run --project windows/GammaCnf.Checks
```

Có file mẫu riêng:

```console
dotnet run --project windows/GammaCnf.Checks -- "D:\Data\Test_spc.cnf"
```

58 kiểm tra với file mẫu: đọc CNF, ghi/đọc lại hiệu chuẩn, bảo toàn byte/số đếm, backup/ghi nguyên tử, từ chối file bị đổi bên ngoài, kiểm tra căn chỉnh phông, bảo toàn net âm và variance, khôi phục tham số Gaussian có/không nhiễu và nền nghiêng, cảnh báo đỉnh chồng, từ chối ROI lỗi, xuất CSV/TXT. Không đưa file mẫu người dùng vào repository. Không có file mẫu thì chạy 49 kiểm tra chung và báo nhóm mẫu NOT RUN.

CI Windows xây dựng app, kiểm tra cú pháp script cài, chạy kiểm tra dữ liệu và smoke test WPF với CNF giả lập: mở phổ, hiệu chuẩn, trừ phông, ROI, vẽ log/keV và xuất CSV/TXT/PNG. Kết quả/ảnh nằm trong artifact `WPF-smoke-evidence`. Đây không thay thế kiểm tra thủ công trên PC của bạn hoặc kiểm thử cài/gỡ.

Cài thêm Inno Setup 6 và chạy `windows\build-windows.bat` để tạo portable exe và Setup.exe. GitHub Actions kiểm tra khi push main; tag `v*` sẽ tạo prerelease nếu các kiểm tra đạt.

Phạm vi CNF: một khối SPEC, 1–32.768 kênh uint32; thời gian giữ theo file, không tự suy diễn múi giờ. Tham khảo cấu trúc CAM/CNF: [SpecUtils CAMIO](https://github.com/sandialabs/SpecUtils/blob/master/src/CAMIO.cpp).
