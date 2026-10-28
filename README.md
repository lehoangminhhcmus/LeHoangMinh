# Gamma CNF Reader

Ứng dụng **C# / WPF** xem phổ gamma Canberra/Genie 2000 `.CNF` trên **Windows 10/11 x64**. Không cần Python, Genie 2000 hoặc cài .NET riêng khi dùng bản self-contained.

## Tải và cài đặt

Mở **[Releases](https://github.com/lehoangminhhcmus/LeHoangMinh/releases)** và chọn bản mới nhất:

- `GammaCNFReader-CSharp-Setup.exe`: bộ cài theo người dùng, có shortcut và mục gỡ cài đặt.
- `GammaCNFReader-CSharp-Windows-x64.zip`: giải nén và chạy `GammaCNFReader.exe`, hoặc dùng `Install.cmd`.

Bản đầu được phát hành dưới dạng **prerelease**, chưa ký số. Windows có thể cảnh báo nhà phát hành chưa xác định. Chỉ mới có một file Canberra thật để kiểm tra độ tương thích; chưa đối chiếu với Genie 2000.

## Chức năng

- Mở hoặc kéo thả file CNF; chỉ đọc file gốc, xử lý tại máy.
- Xem phổ theo kênh hoặc keV khi có hiệu chuẩn năng lượng hợp lệ.
- Zoom bằng con lăn hoặc kéo chọn vùng; Home để xem toàn phổ; thang tuyến tính/log.
- Xem thời gian thực/sống, thời gian chết, số đếm và hệ số hiệu chuẩn.
- Xuất CSV và PNG.

Chưa nhận diện đồng vị, fit đỉnh hoặc tính hoạt độ. Hỗ trợ một khối SPEC, 1–32.768 kênh uint32, đa thức hiệu chuẩn tối đa bậc 3. Kênh bắt đầu từ 0. Thời gian giữ nguyên theo file, không tự suy diễn múi giờ.

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

18 kiểm tra dữ liệu đạt với file mẫu trong môi trường phát triển. Không có file mẫu người dùng trong repository. Nếu không cung cấp file mẫu, chỉ chạy 11 kiểm tra chung và báo NOT RUN cho nhóm mẫu.

Cài thêm Inno Setup 6 và chạy `windows\build-windows.bat` để tạo bộ cài. Workflow GitHub Actions xây dựng bản Windows và phát hành khi đẩy tag `v*`; xem trạng thái workflow để xác nhận build và phát hành thực sự thành công.

[Chi tiết kỹ thuật và giới hạn](windows/README.md).
