# Gamma CNF Reader — C# / WPF

Bản desktop Windows viết bằng C# và WPF trên .NET 10 LTS. Không sử dụng Python, matplotlib hoặc Genie 2000 khi chạy. Windows 10/11 **x64**. Phiên bản: 0.2.0.

## Chạy bản đã đóng gói

Giải nén `GammaCNFReader-CSharp-Windows-x64.zip` vào một thư mục trên máy Windows:

- Mở `GammaCNFReader.exe` để dùng ngay (portable).
- Hoặc chạy `Install.cmd` để cài theo người dùng: sao chép vào `%LOCALAPPDATA%\Programs\GammaCNFReader-CSharp`, tạo shortcut Start menu và mục gỡ cài đặt trong Settings > Apps. Không cần quyền Administrator. Đóng app trước khi cập nhật hoặc gỡ cài đặt.
- Máy sử dụng không cần cài Python hoặc .NET vì bộ chạy đã đi kèm trong exe.

Bản executable chưa ký số. Việc khởi chạy trên Windows và script cài/gỡ cài đặt chưa được kiểm tra thực tế trong môi trường Linux của phiên này. Không gọi file portable là bộ cài Setup.exe. Script cài không thay ứng dụng mặc định của `.cnf`.

## Chức năng

- Chọn file, kéo thả file, hoặc mở đường dẫn qua dòng lệnh `GammaCNFReader.exe "D:\Data\sample.cnf"`.
- Vẽ phổ theo kênh (bắt đầu từ 0) hoặc năng lượng keV nếu có hiệu chuẩn hợp lệ.
- Lăn chuột để zoom quanh vị trí con trỏ; kéo chuột chọn vùng; nút Toàn phổ hoặc Home để đặt lại.
- Thang số đếm tuyến tính/log; ẩn kênh 0 số đếm trên đồ thị log, giữ nguyên trong CSV.
- Hiển thị thông tin mẫu, thời gian thực/sống, thời gian chết và hệ số hiệu chuẩn.
- Di chuột xem số đếm; xuất CSV UTF-8 và PNG.

Xử lý dữ liệu tại máy, chỉ đọc file nguồn. Bản này dành cho xem và xuất phổ, chưa nhận diện đồng vị hoặc tính hoạt độ. Hỗ trợ một khối SPEC, 1–32.768 kênh uint32, hiệu chuẩn CAM tối đa bậc 3. File lỗi hoặc biến thể không hỗ trợ được báo lỗi. Ngày đo được giữ theo file, không tự quy đổi múi giờ.

## Phát triển và tạo bộ cài Setup.exe

Cài **.NET 10 SDK x64**. Có thể dùng Visual Studio hỗ trợ .NET 10 với workload .NET desktop development, hoặc VS Code và CLI.

Trong thư mục `windows`:

```bat
dotnet run --project GammaCnf.App
```

Để tạo portable exe và bộ cài: cài thêm **Inno Setup 6**, rồi chạy `build-windows.bat`.

- Exe: `dist\app\GammaCNFReader.exe`.
- Setup: `dist\installer\GammaCNFReader-CSharp-Setup.exe` nếu có Inno Setup.
- Workflow GitHub: `.github/workflows/windows-csharp.yml` (chưa chạy trong phiên này).

## Kiểm tra đã thực hiện

```bat
dotnet run --project GammaCnf.Checks -- "D:\Data\Test_spc.cnf"
```

18 kiểm tra đạt với file mẫu riêng: số đếm uint32, tổng uint64, thiếu hiệu chuẩn, file sai/cắt ngắn, chỉ mục lỗi, xuất CSV độc lập locale, đa thức, và dữ liệu mẫu (4.096 kênh; 256.546 số đếm; live 1.000 s; real ~1.005 s). Thời gian ghi trong file: 05/05/1983 08:53:00. Không đưa file mẫu riêng vào kho mã. Bỏ đường dẫn mẫu sẽ chỉ chạy 11 kiểm tra chung, nhóm mẫu được báo NOT RUN.

Đã cross-compile WPF Release thành công với 0 lỗi/0 cảnh báo và publish executable PE x64 self-contained trên Linux. Giao diện WPF, cài/gỡ trên Windows và Inno Setup chưa được chạy thực tế; cần kiểm tra trực tiếp trên Windows trước khi phát hành rộng rãi. Chưa đối chiếu dữ liệu với Genie 2000.

Tham khảo cấu trúc CAM/CNF: [SpecUtils CAMIO](https://github.com/sandialabs/SpecUtils/blob/master/src/CAMIO.cpp). Phần giải mã C# nằm riêng trong `GammaCnf.Core`, không phụ thuộc WPF.
