**Gamma CNF Reader v0.7.0 — Windows + macOS**, miễn phí cho sinh viên, giảng viên
và phòng thí nghiệm. Không cần tài khoản, kích hoạt, thuê bao hay cài .NET/Python.

| Máy | File |
| --- | --- |
| Windows 10/11 x64 | `GammaCNFReader-CSharp-Setup.exe` hoặc ZIP portable |
| macOS 14+ Apple Silicon (chip M) | `GammaCNFReader-macOS-osx-arm64.dmg` |
| macOS 14+ Intel | `GammaCNFReader-macOS-osx-x64.dmg` |

Bản macOS mới dùng giao diện Avalonia và **dùng chung toàn bộ phần đọc/tính toán C#**
với Windows: nhiều phổ, chuẩn năng lượng/lưu CNF có .bak, tự scale thời gian phông,
dò đỉnh/tạo ROI, tổng số đếm và Gaussian/FWHM, chuẩn hiệu suất GLS/JSON, hoạt độ Bq/Bq/kg,
xuất CSV/PNG/TXT. Windows giữ giao diện WPF và được nâng số phiên bản đồng bộ.

**Cài trên Mac:** mở DMG đúng chip → kéo ứng dụng vào Applications → eject DMG → mở
app. Bản macOS có chữ ký ad-hoc nhưng **chưa ký Apple Developer ID/notarize**. Nếu bị
chặn lần đầu, vào System Settings → Privacy & Security → Open Anyway để cho phép
riêng app; không cần tắt Gatekeeper toàn máy. Windows cũng chưa ký số nhà phát hành.

- [Hướng dẫn macOS](https://github.com/lehoangminhhcmus/LeHoangMinh/blob/main/macos/README.md)
- [Phương pháp tính và giới hạn](https://github.com/lehoangminhhcmus/LeHoangMinh/blob/main/windows/README.md)
- [Quyền sử dụng và phân phối miễn phí](https://github.com/lehoangminhhcmus/LeHoangMinh/blob/main/DISTRIBUTION-LICENSE.txt)

Gói macOS được build và kiểm tra native trên macOS 15 Apple Silicon/Intel. DMG được
verify, mount read-only và chạy kiểm tra giao diện từ ứng dụng bên trong. Kiểm tra
bao gồm phổ CNF tổng hợp, nhiều phổ, hiệu chuẩn/phông, ROI và tổng số đếm, dò/chỉnh/hủy,
GLS/JSON/hoạt độ, ảnh PNG và báo cáo. Core có 155 kiểm tra với phổ riêng tại môi trường
phát triển; CI chạy 144 kiểm tra chung. Windows chạy WPF smoke checks như trước.
`SHA256SUMS.txt` đi kèm để đối chiếu các file tải.

Chưa xác nhận hiệu suất/hoạt độ bằng nguồn chuẩn thực tế hoặc đối chiếu Genie 2000.
ROI tổng còn chứa nền liên tục; một Gaussian không tách multiplet; chưa tự nhận diện
đồng vị hoặc hiệu chỉnh tự hấp thụ/trùng phùng. Cần kiểm chứng kết quả trước khi dùng
chính thức. Không kèm phổ CNF của người dùng hoặc bộ chuẩn thực tế giả định.
