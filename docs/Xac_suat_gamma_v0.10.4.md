# Pγ từ thư viện hoặc nhập tay — v0.10.4

Áp dụng cho Windows/macOS, phổ CNF và ORTEC CHN.

1. Mở phổ có chuẩn năng lượng và tạo/fit ROI Gaussian.
2. Trong **Gợi ý đồng vị**, nạp Stdlib tích hợp hoặc thư mục NLB/CSV riêng.
3. Chọn đúng hàng gồm ROI, đồng vị và năng lượng vạch; kiểm tra các vạch đối chiếu.
   Bấm **Xác nhận vạch cho ROI**. Chỉ chọn hàng để vẽ vạch chưa phải xác nhận.
4. Mở **Hiệu suất/Hoạt độ**, chọn đúng ROI. Ở tab tạo điểm chuẩn hoặc tính hoạt độ,
   bật **Lấy Pγ từ vạch đã xác nhận cho ROI**.
5. Đồng vị, năng lượng, xác suất % và tham chiếu được lấy từ đúng vạch/đúng file đã
   xác nhận. Các ô này chỉ đọc trong chế độ thư viện. Không tự lấy xác suất của
   vạch gần nhất khác, hay trộn dữ liệu giữa các nguồn.
6. Bỏ chọn để **nhập tay**. Đây là chế độ mặc định; các giá trị nhập trước khi bật
   thư viện được khôi phục, có thể sửa tự do. Hai tab có lựa chọn độc lập.

Pγ được hiển thị theo %, đổi sang fraction đúng một lần khi tính
`ε = Nđỉnh / (A × live time × Pγ)` hoặc tính hoạt độ từ đường chuẩn hiệu suất.
Vẫn cần hoạt độ nguồn để tạo điểm chuẩn, hoặc bộ chuẩn/hình học/ngày giờ/bán rã
và các dữ liệu cần thiết khác để tính hoạt độ. Không tự lấy bán rã từ NLB.

## Thiếu dữ liệu và thay đổi ROI

- Vạch thiếu Pγ, Pγ bằng 0 hoặc ngoài (0,100]% không dùng được ở chế độ thư viện.
  Chuyển sang nhập tay với nguồn phù hợp; không tự coi thiếu Pγ là 100%.
- NLB hiện không cung cấp sai số Pγ qua bộ đọc. Sai số không được tự suy ra từ
  cường độ; có thể nhập tay. Nếu chưa nhập, điểm chuẩn ghi thiếu thành phần sai số,
  kết quả hoạt độ nhắc sai số hiển thị chưa gồm Pγ. Tham chiếu ghi rõ nguồn và tình trạng này.
- Xác nhận gắn với phép đo, ROI và phiên bản phân tích hiện tại. Thay chuẩn năng
  lượng/phông hoặc xóa/đổi ROI làm xác nhận cũ mất hiệu lực; cần xác nhận lại.
- Nạp lại/đổi thư viện xóa các xác nhận trong các phổ đang mở. Khi đổi sang ROI
  chưa xác nhận, chế độ thư viện xóa giá trị cũ và chặn tính; không dùng Pγ của ROI trước.
- Xác nhận chỉ lưu trong phiên. Điểm chuẩn đã tạo vẫn lưu Pγ và nguồn trong JSON/TXT
  để đối chiếu; mở JSON không tự xác nhận lại các đỉnh của phổ đang xem.

Tên file Stdlib không chứng minh dữ liệu là đánh giá hạt nhân hiện hành. Thông
báo nguồn/ngày cập nhật NLB chưa xác minh được giữ nguyên. Xác nhận là lựa chọn
của người dùng sau khi kiểm tra, không phải kết luận nhận dạng tự động.
