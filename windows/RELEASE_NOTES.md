# Gamma CNF Reader v0.10.0 — gợi ý đồng vị từ ROI và thư viện NLB

- Tab **Gợi ý đồng vị** bên cạnh **Kết quả ROI**, cập nhật theo phổ/ROI/hiệu chuẩn.
- Chọn thư mục thư viện NLB của Genie hoặc CSV; nhớ đường dẫn và nạp lại khi đổi dữ liệu.
- Hiển thị tên đồng vị khả dĩ, năng lượng đo/thư viện, ΔE, dung sai, Pγ nếu có,
  nguồn thư viện và các ROI hỗ trợ. Không coi gợi ý là xác suất nhận dạng.
- Dung sai kết hợp keV và FWHM; ưu tiên nhiều vạch ở ROI riêng biệt, rồi độ lệch
  năng lượng. ROI chồng/trùng không được đếm lại làm bằng chứng hỗ trợ.
- Bộ đọc NLB chỉ đọc, kiểm tra cấu trúc và liên kết nội bộ; đã xác minh trên
  cả 10 thư viện NLB người dùng cung cấp. Không đóng gói lại các file NLB đó.
- Giữ các tính năng amplitude tối thiểu, đa thức hiệu suất 1–6, đồ thị và thông tin tác giả.

[Hướng dẫn sử dụng](https://github.com/lehoangminhhcmus/LeHoangMinh/blob/main/docs/Goi_y_dong_vi_v0.10.0.md).

Kiểm tra tại môi trường phát triển: 224 kiểm tra chung, 255 kiểm tra khi kèm
10 thư viện NLB đã gửi; build Windows và smoke test giao diện Avalonia đạt.
