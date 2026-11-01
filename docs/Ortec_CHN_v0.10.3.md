# Đọc phổ ORTEC CHN — v0.10.3

Windows/macOS: bấm **Mở file…**, chọn CNF/CHN, hoặc kéo thả `.chn` vào cửa sổ.
Có thể mở đồng thời CNF và CHN, giữ riêng ROI/hiệu chuẩn/phông theo từng phổ.
Nút chọn phông cũng nhận CHN; vẫn yêu cầu cùng số kênh, chuẩn năng lượng phù hợp
và phép đo độc lập. Live/real time được đọc từ đơn vị 20 ms của CHN.

Dùng ROI, dò đỉnh và thư viện Stdlib tích hợp như CNF. Xuất CSV/PNG/TXT được hỗ trợ.
CHN chỉ đọc; nút **Lưu hiệu chuẩn vào CNF…** bị khóa khi chọn CHN. Có thể chỉnh
chuẩn để phân tích trong phiên, nhưng không sửa hoặc chuyển đổi file CHN gốc.

## Phạm vi

- CHN nhị phân ORTEC có chữ ký -1, header 32 byte, 1–32.768 kênh UInt32 little-endian.
- Kênh bắt đầu phải bằng 0. File khai báo 0 kênh hoặc offset khác 0 bị từ chối để
  không tự đoán số kênh hoặc lệch vị trí đỉnh.
- Giữ nguyên cả số đếm ở các kênh đầu/cuối; không tự xóa chúng.
- Nhận file không trailer hoặc trailer 256/512 byte. Trailer -101 đọc chuẩn tuyến
  tính, -102 đọc chuẩn bậc hai (IEEE float32); chỉ nhận chuẩn tăng trên miền kênh.
- Trailer lạ hoặc chuẩn không hợp lệ: giữ số đếm và cảnh báo, hiển thị theo kênh.
  File thiếu header/số đếm hoặc trailer dở dang bị từ chối.
- Ngày dùng cờ thế kỷ 0 = 1900, 1 = 2000; không tự suy diễn múi giờ. Ngày lỗi hoặc
  cờ thế kỷ không rõ được để trống và cảnh báo.
- Chưa nhập chuẩn độ phân giải/FWHM hay kết quả phân tích của ORTEC.

Đã đối chiếu với file CHN do người dùng cung cấp: 16.384 kênh, tổng 6.623.170 counts,
live time 86.400 s, real time 88.470,14 s, bắt đầu 16/11/2024 17:09:06.
File đo người dùng không được đưa vào repository hay bộ cài. CI sử dụng CHN tổng hợp.

Tham khảo cấu trúc: [SpecUtils CHN reader](https://github.com/sandialabs/SpecUtils/blob/master/src/SpecFile_chn.cpp).
