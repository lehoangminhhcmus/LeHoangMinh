# Gamma CNF Reader — macOS

Ứng dụng đọc và phân tích phổ gamma Canberra/Genie 2000 CNF, miễn phí cho sinh
viên, giảng viên và phòng thí nghiệm. Không cần đăng nhập, kích hoạt, thuê bao,
Python, Genie 2000 hay cài .NET riêng. Xem [quyền sử dụng](../DISTRIBUTION-LICENSE.txt).

## Chọn bản tải và cài

Tải tại https://github.com/lehoangminhhcmus/LeHoangMinh/releases/latest

| Máy | File tải |
| --- | --- |
| Apple Silicon (M1/M2/M3/M4 và các chip M tương thích ARM64) | GammaCNFReader-macOS-osx-arm64.dmg |
| Intel | GammaCNFReader-macOS-osx-x64.dmg |
| Windows 10/11 x64 | GammaCNFReader-CSharp-Setup.exe |

Yêu cầu macOS **14 Sonoma trở lên**. Xem **Apple → About This Mac**: mục Chip ghi
Apple M… thì chọn ARM64; mục Processor ghi Intel thì chọn x64. Hai gói macOS chạy
native; bản Apple Silicon không cần Rosetta.

1. Đóng phiên app đang chạy; mở DMG đúng loại máy.
2. Kéo **Gamma CNF Reader.app** vào **Applications**; nếu cập nhật, thay app cũ.
3. Eject ổ đĩa DMG, mở app từ Applications. Dữ liệu CNF/JSON của bạn lưu riêng.
4. Nếu macOS chặn vì nhà phát triển chưa được xác minh: sau lần mở đầu tiên,
   vào **System Settings → Privacy & Security → Open Anyway**, xác nhận cho riêng
   app này. Bản miễn phí hiện có chữ ký ad-hoc để kiểm tra tính toàn vẹn, **chưa
   được ký Apple Developer ID hoặc notarize**. Không cần tắt Gatekeeper toàn máy.

Gỡ cài đặt: đóng app rồi chuyển ứng dụng trong Applications vào Trash. Không xóa
các phổ/JSON/báo cáo bạn đã lưu ở thư mục riêng.

## Sử dụng

- **Mở file…** cho phép chọn nhiều CNF; kéo thả vào cửa sổ hoặc Finder **Open With**.
  Nút trước/sau và danh sách chuyển giữa các phổ, giữ riêng ROI, phông, hệ số và zoom.
- **⌘+O** mở file; **⌘+Tab** được macOS dành để đổi app, vì vậy dùng **Ctrl+Tab** /
  **Ctrl+Shift+Tab** hoặc nút trong app để đổi phổ. Home/Toàn phổ reset zoom.
- Kéo trên đồ thị để zoom, lăn chuột/pinch cuộn để zoom quanh vị trí con trỏ;
  **Shift+kéo** tạo ROI. Chọn keV hoặc Log khi cần; Log ẩn các bin ≤0.
- Tab **Hiệu chuẩn** nhập a0–a3, áp dụng hoặc lưu CNF. Khi ghi chồng, app tạo .bak;
  không thay số đếm, không ghi net/ROI vào CNF, không tính lại kết quả phân tích cũ
  có sẵn trong file. Chỉ ghi layout POLY/keV đã kiểm chứng.
- Tab **Trừ phông** chọn một phép đo CNF độc lập; α tự tính theo live time mẫu/phông,
  hoặc real time nếu chọn. Kiểm tra căn chỉnh; giữ net âm và phương sai thống kê.
- Tab **ROI/Gaussian** dò tự động hoặc nhập kênh thủ công, xem tổng gốc/phông/net,
  diện tích Gaussian, tâm, FWHM, χ²/ν. Nhóm đỉnh chồng không tự fit bằng một Gaussian.
  Sửa/xóa vùng và xuất tất cả ROI ra TXT. SNR dò không phải p-value.
- **Hiệu suất/Hoạt độ…** nhập chứng chỉ nguồn và dữ liệu hạt nhân có căn cứ, thêm ROI
  làm điểm chuẩn, fit GLS, xem phần dư, lưu/mở JSON; tính Bq hoặc Bq/kg cho phổ độc lập
  đúng hình học, trong miền chuẩn. Không kèm bộ chuẩn thực tế giả định.
- **Xuất CSV / Lưu PNG** lưu phổ đang xem; báo cáo ROI/chuẩn/hoạt độ lưu TXT.

Phần đọc/tính toán dùng chung mã C# với bản Windows; xem
[phương pháp và giới hạn](../windows/README.md). Chưa xác nhận hiệu suất/hoạt độ
bằng nguồn chuẩn thực tế hay đối chiếu Genie 2000. Không tự nhận diện đồng vị,
hiệu chỉnh tự hấp thụ/trùng phùng hoặc tách multiplet. Cần kiểm chứng kết quả
trước khi dùng chính thức. File CNF của người dùng không được đưa vào gói phát hành.

## Xây dựng và kiểm tra

Cài .NET 10 SDK trên Mac tương ứng, từ thư mục repo:

```sh
dotnet run --project macos/GammaCnf.Desktop
bash macos/build-macos.sh osx-arm64   # trên Apple Silicon
bash macos/build-macos.sh osx-x64     # trên Intel
```

Quy trình chạy kiểm tra Core, publish self-contained không trim, tạo .app, ký
ad-hoc, tạo/verify DMG, mount read-only rồi chạy smoke test giao diện native từ
chính DMG. Kiểm tra bao gồm CNF tổng hợp, hiệu chuẩn, trừ phông, nhiều phổ/zoom,
ROI/counts, dò/chỉnh/giữ ROI/hủy, ảnh PNG, CSV/TXT, GLS/JSON, hoạt độ. CI chạy trên
macOS 15 ARM64 và Intel. Evidence dùng dữ liệu tổng hợp, không phải phổ người dùng.

Kiểm tra giao diện headless khi phát triển trên Linux (không thay thế native Mac):

```sh
dotnet run --project windows/GammaCnf.Checks -- --write-fixture /tmp/smoke.cnf
dotnet run --project macos/GammaCnf.UiChecks -- /tmp/smoke.cnf /tmp/ui-evidence
```

Giao diện Avalonia được tách riêng để giữ bản Windows WPF ổn định. Mọi thay đổi
phương pháp tính phải thực hiện trong GammaCnf.Core và có kiểm tra dùng chung;
các thay đổi UI tương đương cần cập nhật cả hai ứng dụng.
