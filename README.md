# Gamma CNF Reader

Ứng dụng **C# / WPF** xem phổ gamma Canberra/Genie 2000 `.CNF` trên **Windows 10/11 x64**. Không cần Python, Genie 2000 hoặc cài .NET riêng khi dùng bản self-contained.

## Tải và cài đặt

Mở **[Releases](https://github.com/lehoangminhhcmus/LeHoangMinh/releases)** và chọn bản mới nhất:

- `GammaCNFReader-CSharp-Setup.exe`: bộ cài theo người dùng, có shortcut và mục gỡ cài đặt.
- `GammaCNFReader-CSharp-Windows-x64.zip`: giải nén và chạy `GammaCNFReader.exe`, hoặc dùng `Install.cmd`.

Bản đầu được phát hành dưới dạng **prerelease**, chưa ký số. Windows có thể cảnh báo nhà phát hành chưa xác định. Chỉ mới có một file Canberra thật để kiểm tra độ tương thích; chưa đối chiếu với Genie 2000.

## Chức năng

- Mở/kéo thả CNF, zoom, thang tuyến tính/log, xuất CSV/PNG.
- **Hiệu chuẩn:** nhập a0–a3, áp dụng và lưu lại CNF; ghi đè có bản sao `.bak`. Chỉ ghi layout ACQP POLY/keV đã kiểm chứng, không thay số đếm gốc.
- **Trừ phông:** chọn CNF khác, chuẩn hóa theo live time hoặc α thủ công; kiểm tra căn chỉnh và giữ net âm/sai số.
- **ROI:** Shift+kéo hoặc nhập kênh; fit Gaussian + nền tuyến tính, diện tích ± 1σ, tâm, FWHM, χ²/ν; xuất toàn bộ ROI ra TXT.

[Hướng dẫn đầy đủ và phương pháp tính](windows/README.md). Một Gaussian không tách đỉnh chồng; chưa nhận diện đồng vị hoặc tính hoạt độ. Kết quả cần đối chiếu trước khi dùng chính thức. Lưu CNF chỉ ghi hiệu chuẩn; số đếm net và ROI xuất CSV/TXT. Các kết quả phân tích cũ bên trong CNF không được tính lại.

## Phát triển

Cài .NET 10 SDK x64; chạy từ thư mục repository:

```console
dotnet run --project windows/GammaCnf.App
dotnet run --project windows/GammaCnf.Checks
```

Kiểm tra file mẫu riêng:

```console
dotnet run --project windows/GammaCnf.Checks -- "D:\Data\Test_spc.cnf"
```

58 kiểm tra dữ liệu đạt với file mẫu trong môi trường phát triển. Không có file mẫu người dùng trong repository. Nếu không cung cấp file mẫu, chỉ chạy 49 kiểm tra chung và báo NOT RUN cho nhóm mẫu.

Cài thêm Inno Setup 6 và chạy `windows\build-windows.bat` để tạo bộ cài. Workflow GitHub Actions xây dựng bản Windows và phát hành khi đẩy tag `v*`; xem trạng thái workflow để xác nhận build và phát hành thực sự thành công.

[Chi tiết kỹ thuật và giới hạn](windows/README.md).
