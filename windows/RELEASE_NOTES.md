# Gamma CNF Reader v0.10.4 — Pγ từ thư viện và nhập tay

- Thêm **Xác nhận vạch cho ROI** trong tab Gợi ý đồng vị; gắn một vạch thư viện
  cụ thể với đỉnh Gaussian đang phân tích.
- Tab tạo điểm chuẩn và tính hoạt độ có lựa chọn **Lấy Pγ từ vạch đã xác nhận cho ROI**.
  Lấy đúng đồng vị/năng lượng/Pγ/tham chiếu; vẫn giữ chế độ nhập tay mặc định và
  khôi phục các giá trị đã nhập khi bỏ chọn thư viện.
- Chặn Pγ thiếu, bằng 0, ngoài phạm vi và xác nhận cũ sau thay đổi phân tích/ROI.
  Không tự dùng xác suất từ ứng viên gần nhất hoặc ROI khác.
- Ghi nguồn thư viện vào JSON/TXT; không tự tạo sai số Pγ hay lấy bán rã từ NLB.
- Hỗ trợ đồng nhất Windows/macOS và phổ CNF/CHN.

Kiểm tra: 271 kiểm tra Core về phần trăm/fraction, nguồn dữ liệu, vạch thiếu Pγ và xác nhận cũ;
smoke UI về xác nhận, hiệu suất/hoạt độ, nhập tay, đổi ROI và lưu JSON.
