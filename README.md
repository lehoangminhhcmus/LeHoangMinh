# Gamma CNF Reader

Ứng dụng C# đọc và phân tích phổ gamma Canberra/Genie 2000 `.CNF` trên **Windows 10/11 x64** và **macOS 14+ (Apple Silicon / Intel)**. Miễn phí cho sinh viên, giảng viên và phòng thí nghiệm; không cần tài khoản, kích hoạt, thuê bao, Python, Genie 2000 hoặc cài .NET riêng. [Quyền sử dụng miễn phí](DISTRIBUTION-LICENSE.txt).

## Tải và cài đặt

Mở **[bản mới nhất](https://github.com/lehoangminhhcmus/LeHoangMinh/releases/latest)** để tải:

- **Mac Apple Silicon:** `GammaCNFReader-macOS-osx-arm64.dmg`.
- **Mac Intel:** `GammaCNFReader-macOS-osx-x64.dmg`.
- **Windows:** `GammaCNFReader-CSharp-Setup.exe`: bộ cài theo người dùng, có shortcut và mục gỡ cài đặt.
- `GammaCNFReader-CSharp-Windows-x64.zip`: giải nén và chạy `GammaCNFReader.exe`, hoặc dùng `Install.cmd`.

**v0.7.0** bổ sung hai bản macOS native; mở DMG rồi kéo app vào Applications. [Hướng dẫn cài macOS và mở lần đầu](macos/README.md). Bản Mac chưa ký Apple Developer ID/notarize; nếu macOS chặn, dùng Privacy & Security → Open Anyway cho riêng app. Windows chưa ký số nhà phát hành. Chỉ mới có một file Canberra thật để kiểm tra độ tương thích; phần hiệu suất/hoạt độ được kiểm tra với dữ liệu giả lập, chưa được xác nhận bằng nguồn chuẩn thực tế hoặc Genie 2000.

## Chức năng

- Mở/kéo thả nhiều CNF; nút trước/sau, danh sách và Ctrl+Tab để chuyển, giữ riêng trạng thái từng phổ. Zoom, thang tuyến tính/log, xuất CSV/PNG.
- **Hiệu chuẩn:** nhập a0–a3, áp dụng và lưu lại CNF; ghi đè có bản sao `.bak`. Chỉ ghi layout ACQP POLY/keV đã kiểm chứng, không thay số đếm gốc.
- **Trừ phông:** chọn CNF khác, tự chuẩn hóa theo live time của từng phổ mẫu (hoặc chọn real time); kiểm tra căn chỉnh và giữ net âm/sai số.
- **Hiệu suất và hoạt độ:** nhập nguồn chuẩn/chứng chỉ, hiệu chỉnh phân rã, fit GLS đường hiệu suất kèm phần dư, lưu JSON; tính Bq/Bq/kg theo ROI đúng hình học, trong miền chuẩn.
- **Dò đỉnh tự động:** quét nhiều độ rộng, tạo ROI và fit; chỉnh ngưỡng, giữ vùng thủ công, nhóm vùng chồng và hủy tác vụ.
- **ROI:** Shift+kéo hoặc nhập kênh; fit Gaussian + nền tuyến tính, tổng gốc/phông/net ± 1σ song song diện tích Gaussian, tâm, FWHM, χ²/ν; xuất toàn bộ ROI ra TXT.

[Hướng dẫn đầy đủ và phương pháp tính](windows/README.md). Một Gaussian không tách đỉnh chồng; chưa tự nhận diện đồng vị hoặc gộp hoạt độ từ nhiều vạch. Kết quả cần đối chiếu trước khi dùng chính thức. Lưu CNF chỉ ghi chuẩn năng lượng; chuẩn hiệu suất lưu JSON, số đếm net/ROI/hoạt độ xuất CSV/TXT. Các kết quả phân tích cũ bên trong CNF không được tính lại.

## Phát triển

Cài .NET 10 SDK x64; chạy từ thư mục repository:

```console
dotnet run --project windows/GammaCnf.App     # Windows WPF
dotnet run --project macos/GammaCnf.Desktop   # macOS Avalonia
dotnet run --project windows/GammaCnf.Checks
```

Kiểm tra file mẫu riêng:

```console
dotnet run --project windows/GammaCnf.Checks -- "D:\Data\Test_spc.cnf"
```

155 kiểm tra dữ liệu đạt với file mẫu trong môi trường phát triển. Không có file mẫu người dùng trong repository. Nếu không cung cấp file mẫu, chỉ chạy 144 kiểm tra chung và báo NOT RUN cho nhóm mẫu.

Cài thêm Inno Setup 6 và chạy `windows\build-windows.bat` để tạo bộ cài. Dùng `bash macos/build-macos.sh osx-arm64` trên Apple Silicon hoặc `osx-x64` trên Intel để tạo DMG. Workflow GitHub Actions xây dựng/kiểm tra Windows và cả hai Mac, chỉ phát hành khi tất cả đạt sau khi đẩy tag `v*`; xem trạng thái workflow để xác nhận build và phát hành thực sự thành công.

[Chi tiết kỹ thuật và giới hạn](windows/README.md).
