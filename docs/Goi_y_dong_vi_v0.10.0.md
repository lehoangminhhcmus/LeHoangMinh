# Gợi ý đồng vị từ ROI — v0.10.0

Áp dụng cho Windows/macOS. Kết quả là danh sách **đồng vị khả dĩ theo năng lượng**,
không phải nhận dạng chắc chắn, độ tin cậy thống kê hoặc kết quả hoạt độ.

## Cách dùng với thư viện NLB của Genie

1. Tạo một thư mục trên máy, chẳng hạn `D:\GammaLibraries` hoặc
   `~/Documents/GammaLibraries`. Chép các file `.nlb` cần dùng vào thư mục này.
   Chương trình chỉ đọc file; không thay đổi thư viện gốc.
2. Mở phổ CNF có chuẩn năng lượng đúng; chọn ROI thủ công hoặc dò đỉnh tự động.
3. Mở tab **Gợi ý đồng vị**, cạnh tab **Kết quả ROI** phía dưới đồ thị.
4. Bấm **Chọn thư mục NLB/CSV…** và chọn thư mục ở bước 1. Chương trình nạp tất
   cả NLB/CSV ngay trong thư mục, không tìm trong thư mục con.
5. Xem ứng viên theo từng số ROI: tên đồng vị, năng lượng đo và thư viện, ΔE,
   dung sai, xác suất phát nếu đọc được và các ROI hỗ trợ.
6. Khi thay/thêm thư viện, bấm **Nạp lại**. Khi đổi dung sai, bấm **Cập nhật gợi ý**.
   ROI, hiệu chuẩn, phông hoặc phổ thay đổi sẽ tự tính lại gợi ý.

Đường dẫn thư viện được nhớ cho lần mở chương trình tiếp theo. Nếu không lưu
được thiết lập, thư viện vẫn dùng được trong phiên hiện tại và có thông báo.
Chương trình không kèm sẵn dữ liệu NLB của người dùng; tự chọn thư viện có nguồn
phù hợp. NLB không hỗ trợ hoặc lỗi cấu trúc sẽ báo lỗi, không đoán liên kết vạch.

## Dung sai và thứ tự ứng viên

Điều kiện khớp: `|E đo − E thư viện| ≤ max(dung sai keV, hệ số × FWHM keV)`.
Mặc định 2 keV và 0,5 × FWHM; cần điều chỉnh theo detector và chất lượng chuẩn.
Hệ số FWHM bằng 0 cho phép dùng riêng dung sai keV.

Chỉ xét ROI fit thành công, có năng lượng dương hữu hạn. ROI lỗi, nhóm đỉnh tự
động chưa tách và phổ chưa có chuẩn năng lượng không được nhận dạng bằng cách
lấy trung điểm ROI. Thứ tự ưu tiên: nhiều ROI khác khớp vạch của cùng đồng vị,
độ lệch năng lượng nhỏ hơn, rồi xác suất phát lớn hơn khi có dữ liệu.

Vạch hỗ trợ phải ở ROI không chồng vùng và khác năng lượng vạch đang xét. Không
đếm lại một ROI cho nhiều vạch trùng nhau. Không xét tỷ lệ diện tích theo hiệu
suất, bán rã, đỉnh thoát, trùng phùng tổng, cân bằng chuỗi phân rã hoặc sự vắng
mặt của vạch khác; vì vậy một đỉnh đơn lẻ vẫn có thể có nhiều ứng viên.

## Các thư viện NLB đã kiểm tra

Đã kiểm tra đọc cả 10 file: Naidemo, Ptcal, Demolib, Stdlib, A_g, H_o, P_r, s_s,
t_t, u_z. Có kiểm tra các liên kết vạch quen thuộc Co-60, Cs-137, Am-241, Na-22,
K-40 trong những file có các đồng vị này.

Định dạng hỗ trợ: CAM NLB với bảng đồng vị/vạch và cấu trúc bản ghi như các file
trên. Thứ tự khối trong directory có thể theo lịch sử cấp phát: bộ đọc ghép các
khối vạch theo thứ tự năng lượng, giữ nguyên thứ tự các vạch bằng năng lượng,
rồi giải tham chiếu đồng vị. Kiểm tra chữ ký, biên khối, độ dài/số bản ghi,
trật tự năng lượng và giới hạn tham chiếu trước khi nhận dữ liệu.

NLB có thể là thư viện demo/cũ. Tên file không chứng minh dữ liệu là chuẩn hiện
hành; trường tham chiếu hiển thị rõ chưa xác minh nguồn hạt nhân. Yield >100 được
để trống cột Pγ và chỉ giữ năng lượng để gợi ý, không chuyển thành xác suất gamma.
Các hậu tố trạng thái như `m`, `x` được giữ theo tên gốc, không tự diễn giải lại.

## CSV bổ sung

Có thể đặt CSV UTF-8 trong cùng thư mục. Tiêu đề bắt buộc:

```csv
Nuclide,EnergyKeV,IntensityPercent,Reference
```

Mỗi dòng là một vạch. Dùng dấu phẩy phân cột, dấu chấm thập phân; nếu nguồn tham
chiếu có dấu phẩy thì đặt trong dấu ngoặc kép. Xác suất phát tính theo %, từ 0
đến 100 hoặc để trống nếu không có; năng lượng là keV. Vạch xác suất bằng 0 không
được gợi ý. Vạch trùng hoàn toàn được gộp, nguồn khác nhau được giữ để đối chiếu.
Không nạp trực tiếp các bảng trích NLB có danh sách đồng vị và vạch tách rời.

Giới hạn: 100 file, tổng 32 MB, 200.000 vạch và tối đa 50.000 kết quả khớp.
Nếu quá nhiều kết quả, giảm dung sai hoặc chọn thư viện gọn hơn.
