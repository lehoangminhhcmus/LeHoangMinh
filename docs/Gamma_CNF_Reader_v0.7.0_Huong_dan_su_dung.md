# Gamma CNF Reader v0.7.0 — Hướng dẫn sử dụng

TÀI LIỆU THỰC HÀNH

Windows 10/11 x64 • macOS 14+ Apple Silicon và Intel

Đọc và xử lý phổ gamma Canberra/Genie 2000 CNF

Dành cho sinh viên, giảng viên và người sử dụng trong phòng thí nghiệm. Tên nút, đơn vị nhập và điều kiện chấp nhận trong tài liệu được đối chiếu với mã nguồn của bản v0.7.0.

> TRỌNG TÂM: CHUẨN HIỆU SUẤT: Các mục 8–15 trình bày theo từng công đoạn: chuẩn bị nguồn và hình học, phân tích ROI, nhập chứng chỉ và dữ liệu gamma, thêm điểm, fit GLS, đánh giá phần dư, lưu JSON và kiểm chứng độc lập.

Phân biệt hai loại chuẩn

Chuẩn năng lượng chuyển số kênh thành keV. Các hệ số a0–a3 có thể lưu vào CNF theo layout được hỗ trợ.

Chuẩn hiệu suất đỉnh toàn phần chuyển diện tích đỉnh thành thông tin định lượng theo năng lượng và hình học đo. Bộ chuẩn này lưu riêng dưới dạng JSON.

> PHẠM VI SỬ DỤNG: Phần mềm hỗ trợ học tập và phân tích; kết quả hiệu suất/hoạt độ chưa được xác nhận bằng nguồn chuẩn thực tế hoặc đối chiếu Genie 2000. Kiểm chứng độc lập trước khi dùng kết quả chính thức. Các ví dụ số trong tài liệu được ghi rõ là giả định, không phải dữ liệu nguồn chuẩn.

Ngày biên soạn: 29/10/2026 • Tài liệu tiếng Việt

[Trang tải bản v0.7.0 trên GitHub](https://github.com/lehoangminhhcmus/LeHoangMinh/releases/tag/v0.7.0)


<a name="section_1"></a>

## 1. Tra cứu nhanh và quy trình sử dụng

Có thể bấm vào tên mục dưới đây hoặc dùng Navigation Pane của Word để chuyển đến phần cần đọc. Giao diện Windows và macOS có thể khác cách bố trí, nhưng tên chức năng và phương pháp tính tương đương.

[02  Cài đặt và mở ứng dụng](#section_2)

[03  Mở nhiều phổ và xem đồ thị](#section_3)

[04  Chuẩn năng lượng và lưu CNF](#section_4)

[05  Trừ phông và chuẩn hóa thời gian](#section_5)

[06  ROI, diện tích Gaussian và FWHM](#section_6)

[07  Dò đỉnh và tạo ROI tự động](#section_7)

[08  Chuẩn hiệu suất: chuẩn bị phép đo](#section_8)

[09  Thông tin hình học và thời gian](#section_9)

[10  Nhập chứng chỉ nguồn chuẩn](#section_10)

[11  Ghép vạch gamma và hệ số K](#section_11)

[12  Thêm điểm và phối hợp nhiều phổ](#section_12)

[13  Fit đường chuẩn và đánh giá chất lượng](#section_13)

[14  Công thức và ví dụ một điểm chuẩn](#section_14)

[15  Lưu JSON và kiểm chứng bộ chuẩn](#section_15)

[16  Tính hoạt độ mẫu và Bq/kg](#section_16)

[17  Xuất dữ liệu và lưu hồ sơ](#section_17)

[18  Lỗi thường gặp và cách xử lý](#section_18)

[19  Phiếu ghi chép và kiểm tra cuối](#section_19)

> QUY TRÌNH THÔNG THƯỜNG: Mở CNF → kiểm tra thông tin phép đo → kiểm tra/áp dụng chuẩn năng lượng → trừ phông nếu phù hợp → dò/chọn ROI → kiểm tra fit → xuất kết quả. Nếu định lượng: tạo hoặc mở bộ chuẩn hiệu suất đã kiểm chứng → tính hoạt độ mẫu độc lập → lưu báo cáo.

Quy ước nhập liệu: dùng dấu chấm hoặc dấu phẩy thập phân; không dùng dấu phân nhóm hàng nghìn. Trong ví dụ, nhập “10000” cho mười nghìn; không nhập “10.000”. Mọi giá trị sai số cần đúng quy ước của từng ô.


<a name="section_2"></a>

## 2. Cài đặt và mở ứng dụng

| Hệ máy | Gói cài v0.7.0 | Cách sử dụng |
| --- | --- | --- |
| Windows 10/11 x64 | GammaCNFReader-CSharp-Setup.exe | Chạy bộ cài theo người dùng; mở ứng dụng từ Start menu. |
| Windows x64, portable | GammaCNFReader-CSharp-Windows-x64.zip | Giải nén toàn bộ rồi chạy GammaCNFReader.exe; Install.cmd là lựa chọn cài theo người dùng. |
| Mac Apple Silicon | GammaCNFReader-macOS-osx-arm64.dmg | Dành cho chip Apple M; yêu cầu macOS 14 trở lên. |
| Mac Intel | GammaCNFReader-macOS-osx-x64.dmg | Dành cho bộ xử lý Intel; yêu cầu macOS 14 trở lên. |

[Tải đúng phiên bản v0.7.0](https://github.com/lehoangminhhcmus/LeHoangMinh/releases/tag/v0.7.0)

### Cài trên macOS

1. Vào Apple → About This Mac. Nếu thấy Chip Apple M… chọn ARM64; nếu thấy Processor Intel chọn x64.

2. Mở file DMG, kéo Gamma CNF Reader.app vào Applications. Khi cập nhật, đóng app cũ trước rồi thay ứng dụng.

3. Eject ổ DMG và mở ứng dụng trong Applications. Không cần cài thêm .NET, Python hoặc Genie 2000.

4. Ứng dụng chưa được ký Apple Developer ID/notarize. Nếu bị chặn lần đầu, sau khi thử mở hãy vào System Settings → Privacy & Security → Open Anyway và xác nhận cho riêng ứng dụng.

### Cài trên Windows và cập nhật

Bộ cài không yêu cầu quyền Administrator. Bản chưa ký số nhà phát hành nên Windows có thể hiển thị cảnh báo; chỉ sử dụng gói tải từ trang GitHub nêu trên và kiểm tra nguồn file trước khi cho phép chạy.

Giữ CNF gốc, JSON chuẩn và báo cáo trong thư mục dữ liệu riêng, không đặt cùng thư mục cài đặt. Khi gỡ app, chỉ gỡ chương trình; chủ động lưu giữ dữ liệu thí nghiệm của mình.

> SỬ DỤNG MIỄN PHÍ: Không cần tài khoản, thuê bao, kích hoạt hoặc trả phí sử dụng. Có thể chia sẻ các gói phát hành chính thức không sửa đổi kèm thông báo quyền sử dụng. Xem DISTRIBUTION-LICENSE.txt trong repository/gói phát hành.


<a name="section_3"></a>

## 3. Mở nhiều phổ và xem đồ thị

1. Bấm Mở file… và chọn một hoặc nhiều file .CNF, hoặc kéo thả file vào cửa sổ. Định dạng hỗ trợ là phổ gamma Canberra/Genie 2000, không phải mọi file có đuôi CNF.

2. Kiểm tra tên phổ, số kênh, tổng số đếm gốc, ngày bắt đầu, Live time và Real time. Không tự suy diễn múi giờ từ ngày trong CNF.

3. Chuyển phổ bằng ◀ Phổ trước / Phổ sau ▶ hoặc danh sách phổ đang mở. Tối đa 32 phổ trong một phiên; xem đường dẫn để phân biệt các file trùng tên.

4. Dùng Đóng phổ này để đóng riêng phổ đang chọn. Xuất kết quả trước khi đóng; ROI và trạng thái xử lý không được tự lưu thành một dự án để mở lại.

| Thao tác | Windows | macOS |
| --- | --- | --- |
| Mở file | Ctrl+O | ⌘+O hoặc Ctrl+O |
| Chuyển phổ | Ctrl+Tab / Ctrl+Shift+Tab | Ctrl+Tab / Ctrl+Shift+Tab; ⌘+Tab là đổi ứng dụng của macOS |
| Phóng to vùng | Kéo chuột trên đồ thị | Kéo chuột trên đồ thị |
| Zoom quanh con trỏ | Con lăn chuột | Cuộn chuột/trackpad |
| Thêm ROI bằng chuột | Giữ Shift rồi kéo | Giữ Shift rồi kéo |
| Trở về toàn phổ | Toàn phổ hoặc Home | Toàn phổ; Home nếu bàn phím hỗ trợ |

### Hiển thị keV và Log

Chọn keV khi phổ có đường chuẩn năng lượng hợp lệ; nếu chưa có chuẩn, dùng trục kênh. Di chuột lên phổ để xem kênh, năng lượng, số đếm gốc và giá trị đang hiển thị.

Chọn Log để quan sát các đỉnh ở nhiều mức số đếm. Những bin có giá trị ≤0 bị ẩn khi vẽ log; không bị xóa khỏi dữ liệu hay báo cáo. Sau trừ phông, nên kiểm tra thêm ở thang tuyến tính để thấy số đếm net âm.

> TRẠNG THÁI RIÊNG CỦA TỪNG PHỔ: Mỗi phổ giữ riêng hệ số và bản nháp ô nhập, phông, ROI, chế độ trục và zoom. Bộ chuẩn hiệu suất được dùng chung trong phiên. Mở lại cùng đường dẫn chỉ chuyển đến phổ đã có trong bộ nhớ; nếu file được sửa bên ngoài, đóng phổ rồi mở lại để đọc mới.


<a name="section_4"></a>

## 4. Chuẩn năng lượng và lưu CNF

Thực hiện trong tab Hiệu chuẩn. Bản v0.7.0 cho nhập hệ số có sẵn; chưa có chức năng tự fit đường chuẩn năng lượng từ danh sách kênh–năng lượng.

> PHƯƠNG TRÌNH: E(ch) = a0 + a1 × ch + a2 × ch² + a3 × ch³. E tính bằng keV; kênh ch bắt đầu từ 0. Chuẩn tuyến tính dùng a2 = 0 và a3 = 0.

1. Lấy hệ số từ phép chuẩn đã được kiểm tra trên đúng detector/cấu hình MCA. Kiểm tra quy ước kênh 0 để tránh sai lệch một kênh.

2. Nhập a0, a1, a2, a3; bấm Áp dụng hệ số. Phần mềm kiểm tra hệ số hữu hạn và đường chuẩn tăng trên miền kênh.

3. Kiểm tra lại vị trí các đỉnh đã biết ở nhiều vùng năng lượng. Áp dụng hệ số chỉ thay dữ liệu trong phiên, chưa ghi vào file CNF.

4. Nếu cần ghi, bấm Lưu hiệu chuẩn vào CNF…; chọn tên mới để giữ bản gốc, hoặc chọn đúng file gốc để lưu chồng rồi xác nhận.

| Sau thao tác | Điều cần nhớ |
| --- | --- |
| Áp dụng hệ số | Đồ thị và các ROI được tính lại; tên phổ có dấu * khi chuẩn chưa lưu. |
| Lưu chồng CNF gốc | Tạo bản sao đầy đủ .bak trước khi thay file. Không xóa bản sao này cho đến khi kiểm tra lại. |
| Lưu thành CNF mới | Giữ số đếm gốc và lưu chuẩn mới; không được chọn đích là một phổ khác đang mở. |
| Định dạng không cho ghi | Chỉ layout ACQP POLY/keV đã kiểm chứng được hỗ trợ ghi. Có thể xem được phổ nhưng vẫn bị từ chối sửa CNF. |
| File đã đổi bên ngoài | Dừng thao tác, đóng/mở lại bản cần xử lý; không cố ghi đè lên dữ liệu không còn khớp bản đã đọc. |

Các hệ số khi lưu được mã hóa theo số thực CAM 32-bit, sau đó đọc lại; có thể có sai khác làm tròn rất nhỏ so với số nhập ban đầu.

> LƯU CNF KHÔNG LƯU TOÀN BỘ PHÂN TÍCH: Chỉ ghi chuẩn năng lượng, không ghi số đếm sau trừ phông, ROI, chuẩn hiệu suất hay hoạt độ. Kết quả phân tích cũ có sẵn trong CNF không được tính lại; cần phân tích lại khi dùng phần mềm khác.


<a name="section_5"></a>

## 5. Trừ phông và chuẩn hóa thời gian

Phổ phông phải là phép đo độc lập phù hợp detector, cấu hình, số kênh và điều kiện đo. Không dùng chính phổ mẫu hoặc bản sao cùng phép đo làm phông.

1. Chọn đúng phổ mẫu trong danh sách đang mở. Vào tab Trừ phông → Chọn phổ phông CNF… và chọn file phông.

2. Mặc định để Live time (khuyến nghị). Khi dữ liệu hợp lệ, ứng dụng tự áp dụng trừ và hiển thị thời gian phông gốc → thời gian mục tiêu cùng hệ số α.

3. Nếu cần dùng Real time, chọn rõ chế độ này. Ứng dụng tính lại tỷ số; không tự thay Live time thiếu bằng hệ số 1.

4. Kiểm tra tên phông đang áp dụng trong phần thông tin phổ. Bấm Bỏ trừ phông để phục hồi số đếm gốc và tính lại ROI của riêng phổ đó.

> CÔNG THỨC: α = t_mẫu / t_phông; net[i] = mẫu[i] − α × phông[i]; variance[i] = mẫu[i] + α² × phông[i]. Giả định hai phép đo độc lập và hệ số thời gian chính xác.

Ví dụ: mẫu live = 1000 s, phông live = 500 s thì α = 2. Mẫu khác live = 2000 s dùng cùng phông sẽ có α = 4. File CNF phông gốc không bị sửa số đếm hoặc thời gian.

### Điều kiện để áp dụng

Hai phổ cùng số kênh và hiệu chuẩn lệch không quá 0,05 kênh theo kiểm tra căn chỉnh của ứng dụng. Chưa tự rebin hoặc hiệu chỉnh trôi đỉnh.

Thời gian dùng để chuẩn hóa phải dương và hữu hạn. Live time được ưu tiên để chuẩn hóa số đếm khi có dead time; chỉ chọn Real time khi có căn cứ phù hợp phép đo.

Nếu thiếu chuẩn năng lượng, chỉ đánh dấu xác nhận cùng cấu hình MCA khi đã kiểm tra thực tế, rồi bấm Áp dụng trừ phông. Xác nhận này không thay cho chuẩn năng lượng khi định lượng.

Số đếm net âm được giữ lại. Không ép về 0 vì sẽ làm sai tổng số đếm và thống kê.

> KHI PHÔNG MỚI BỊ TỪ CHỐI: Phép trừ hợp lệ trước đó vẫn có thể đang được giữ. Tên file vừa chọn ở tab không nhất thiết là phông đang áp dụng; hãy đọc dòng thông tin phông trên phổ. Sau khi thay phông, kiểm tra lại ROI và các điểm hiệu suất đã thêm trước đó.


<a name="section_6"></a>

## 6. ROI, diện tích Gaussian và FWHM

1. Giữ Shift rồi kéo vùng quanh một đỉnh, hoặc vào ROI / Gaussian, nhập Kênh đầu và Kênh cuối rồi bấm Thêm ROI và fit. Hai kênh biên đều được tính.

2. Chọn vùng đủ chứa đỉnh và phần nền hai bên, tránh lấy thêm đỉnh lân cận hoặc vùng nền cong rõ. Fit dùng một Gaussian tích phân theo kênh cộng nền tuyến tính.

3. Xem đường mô hình màu đỏ, tâm, FWHM, χ²/ν và trạng thái. Chọn dòng ROI, sửa hai giới hạn rồi bấm Cập nhật giới hạn ROI đang chọn nếu cần.

4. Có thể Xóa ROI đang chọn, Xóa tất cả ROI hoặc Xuất tất cả ROI ra TXT…. Tối đa 100 ROI/phổ; cộng trực tiếp từ 1 kênh, fit Gaussian cần 9–4096 kênh.

| Cột/kết quả | Ý nghĩa |
| --- | --- |
| Tổng gốc ROI | Cộng số đếm gốc của phổ trong vùng, gồm cả nền liên tục và các thành phần khác. |
| Phông đã scale | α nhân tổng số đếm phổ phông trong cùng vùng. |
| Tổng net ± 1σ | Tổng gốc trừ phông đã scale; sai số bằng căn tổng variance. Vẫn còn nền liên tục dưới đỉnh. |
| Gauss ± 1σ | Diện tích toàn Gaussian sau khi mô hình tách nền tuyến tính; dùng cho hiệu suất/hoạt độ khi fit đạt. |
| Đỉnh (ch), keV | Tâm Gaussian theo kênh và năng lượng; không nhất thiết trùng kênh có số đếm lớn nhất. |
| FWHM (ch), FWHM (keV) | Độ rộng nửa chiều cao: FWHM ≈ 2,35482 × sigma Gaussian. Đơn vị keV cần chuẩn năng lượng. |
| χ²/ν, Trạng thái | Đánh giá độ phù hợp mô hình; χ²/ν >3 được cảnh báo. Fit đạt về số học vẫn cần kiểm tra vật lý. |

> DIỆN TÍCH ĐỂ CHUẨN HIỆU SUẤT: Dùng Gauss ± 1σ, không dùng Tổng gốc ROI hay Tổng net. Không chia diện tích cho thời gian trước khi thêm điểm chuẩn: phần mềm tự dùng Live time. Không trừ nền liên tục thêm lần nữa khỏi diện tích Gaussian.

Fit không hội tụ, diện tích quá yếu (<3σ), ma trận suy biến hoặc đỉnh bị cắt ở biên không được xem là định lượng hợp lệ. ROI lỗi vẫn có tổng số đếm và được xuất TXT. Mô hình một Gaussian không giải quyết multiplet hoặc tailing phức tạp.


<a name="section_7"></a>

## 7. Dò đỉnh và tạo ROI tự động

1. Chọn phổ cần dò. Nếu có trừ phông, bảo đảm đang áp dụng đúng phông; thuật toán dùng net và phương sai đã lan truyền.

2. Trong ROI / Gaussian, kiểm tra các thông số dưới đây rồi bấm Dò đỉnh và tạo ROI tự động. Thuật toán quét toàn bộ phổ đang chọn, không chỉ vùng đang zoom.

| Thiết lập | Mặc định | Cách điều chỉnh |
| --- | --- | --- |
| FWHM nhỏ nhất / lớn nhất | 3 / 40 kênh | Đặt theo độ phân giải detector và độ rộng đỉnh theo kênh. Miền cho phép 2–256 kênh. |
| Ngưỡng SNR bộ lọc | 5 | Miền 3–30. Hạ ngưỡng có thể tìm thêm đỉnh yếu nhưng tăng ứng viên giả. |
| Nửa rộng ROI | 3,5 sigma ước lượng | Miền 2,5–6. Tăng nếu cần vùng nền hai bên, nhưng tránh kéo vào đỉnh khác. |
| Thay ROI tự động cũ | Được chọn | Dò lại thay các vùng tự dò cũ, luôn giữ vùng thủ công. Bỏ chọn để bổ sung vùng mới không chồng. |

3. Quan sát vạch cam đứt là ứng viên đỉnh; nền tô là ROI, đường đỏ là mô hình fit nếu hợp lệ. Kiểm tra các vùng trước khi dùng để định lượng.

4. Nếu vùng tự động cần chỉnh, chọn dòng ROI, sửa giới hạn và bấm Cập nhật giới hạn ROI đang chọn. Vùng đã sửa trở thành thủ công và được giữ ở lần dò sau.

### Nhóm đỉnh, hủy tác vụ và thay dữ liệu

Các đề xuất ROI tự động chồng nhau được gom nhóm. Nhóm nhiều ứng viên chỉ có tổng số đếm; không tự công nhận một Gaussian cho cả nhóm.

ROI thủ công luôn được giữ; đề xuất chồng với vùng đang có bị bỏ. Giới hạn 100 ứng viên và 100 ROI được thông báo khi chạm ngưỡng.

Bấm Hủy dò để dừng mà giữ ROI trước đó. Có thể chuyển phổ trong lúc chạy; kết quả chỉ được đưa vào đúng phổ đã khởi chạy nếu dữ liệu không đổi.

Sau thay chuẩn năng lượng/phông, fit và thống kê được tính lại, nhưng thông tin dò cũ bị đánh dấu. Dò lại và kiểm tra vùng trước khi sử dụng.

> ỨNG VIÊN KHÔNG PHẢI NHẬN DIỆN ĐỒNG VỊ: SNR bộ lọc không phải p-value. Cạnh Compton, nền cong và đuôi đỉnh có thể gây phát hiện giả; đỉnh yếu có thể bị bỏ sót. FWHM ước lượng để dựng ROI khác FWHM fit dùng trong bảng định lượng.


<a name="section_8"></a>

## 8. Chuẩn hiệu suất: chuẩn bị phép đo

Mục tiêu là tạo đường hiệu suất đỉnh toàn phần tuyệt đối ε(E) cho một detector và một hình học đo xác định. Không nhập “hiệu suất tương đối” danh định của HPGe (ví dụ 25% trên thông số máy) làm hiệu suất của phép đo.

> ĐIỀU KIỆN QUYẾT ĐỊNH: Bộ chuẩn chỉ dùng cho điều kiện detector, khoảng cách, bình chứa/thể tích, vị trí nguồn, cấu hình và đặc tính suy giảm phù hợp. Đổi tên hình học để vượt kiểm tra không làm hiệu suất trở nên đúng.

### Hồ sơ phải có trước khi thao tác

Phổ CNF nguồn chuẩn và nhật ký đo: ngày/giờ/múi giờ, Live time, Real time, detector, cấu hình MCA, hình học và vị trí đặt nguồn.

Chứng chỉ nguồn: đồng vị, mã nhận dạng, hoạt độ tại ngày chứng nhận, đơn vị, độ không đảm bảo và hệ số phủ k. Nếu chứng chỉ cho Bq/g hoặc Bq/mL, phải xác định tổng Bq của phần nguồn được đo và độ không đảm bảo phù hợp trước khi nhập.

Tài liệu dữ liệu hạt nhân có phiên bản/tham chiếu: năng lượng vạch, xác suất gamma và độ không đảm bảo 1σ, bán rã và độ không đảm bảo 1σ.

Phông độc lập phù hợp nếu quy trình cần trừ; kết quả đánh giá trôi đỉnh, chất lượng chuẩn năng lượng và đỉnh chồng/nhiễu vạch.

Căn cứ các hệ số hiệu chỉnh tự hấp thụ, trùng phùng tổng hoặc suy giảm nếu cần; phần mềm không tự tính các hiệu chỉnh này.

### Chuẩn bị phổ nguồn trong cửa sổ chính

1. Mở phổ nguồn; kiểm tra thời gian và đường chuẩn năng lượng bằng các vạch đã biết.

2. Áp dụng phông phù hợp nếu cần. Dò hoặc chọn ROI cho các đỉnh đơn, có nền hai bên; kiểm tra mô hình fit và χ²/ν.

3. Chỉ giữ các ROI có diện tích Gaussian và sai số tin cậy. Phân bố các vạch trên miền năng lượng dự kiến sử dụng; không chỉ tập trung tại một vùng hẹp.

4. Bấm Hiệu suất / Hoạt độ… để mở cửa sổ chuẩn. Phần mềm không tự điền nguồn chuẩn hoặc dữ liệu hạt nhân thực tế.

Chọn ít nhất bậc+2 năng lượng khác nhau: bậc 1 cần ≥3, bậc 2 cần ≥4, bậc 3 cần ≥5. Đây là mức tối thiểu của phần mềm, không phải bảo đảm chất lượng chuẩn. Nên có thêm điểm và phép kiểm chứng độc lập.


<a name="section_9"></a>

## 9. Thông tin hình học và thời gian

Nhập/kiểm tra vùng thông tin chung ở đầu cửa sổ Chuẩn hiệu suất và hoạt độ trước khi thêm điểm. Các mục dưới đây thuộc phép đo đang mở.

| Ô trên giao diện | Cách điền và kiểm tra |
| --- | --- |
| Detector / số máy | Mã detector hoặc số nhận dạng duy nhất; dùng cách viết nhất quán giữa các phổ. |
| Mã hình học / cấu hình mẫu | Mã cấu hình có định nghĩa trong sổ tay phòng thí nghiệm, ví dụ GEOM-A. Ví dụ mã chỉ mang tính đặt tên, không tự thiết lập hình học. |
| Khoảng cách (mm) | Nhập khoảng cách theo cùng quy ước đo đã ghi trong quy trình; không nhầm cm với mm. |
| Ghi chú: bình chứa, thể tích, nền mẫu… | Ghi đặc tính xác định cấu hình: bình, thể tích, vị trí, thành phần nền… Nội dung này cũng tham gia so khớp hình học. |
| Tên bộ chuẩn | Tên dễ truy xuất detector–hình học–ngày, ví dụ HPGe01_GEOM-A_20261029. |
| Bắt đầu đo ISO + múi giờ | Bắt buộc có múi giờ: ví dụ 2026-10-29T10:30:00+07:00 cho giờ Việt Nam. Không tự thêm +07:00 nếu phép đo thực hiện ở múi giờ khác. |
| Live time (s) | Thời gian sống, dương, tính bằng giây. Được lấy từ CNF nếu có; cần đối chiếu nhật ký. |
| Real time (s) | Thời gian thực, dương, tính bằng giây; không được nhỏ hơn Live time. |
| ROI của phổ đang mở… | Chọn đúng ROI của vạch nguồn. Danh sách chỉ đưa ROI fit thành công; vẫn phải kiểm tra điều kiện định lượng khi thêm điểm. |

### Múi giờ và ngày chứng nhận

Ngày trong CNF không xác định múi giờ nên ứng dụng không tự thêm offset. Phải bổ sung đúng múi giờ cho cả đầu phép đo và ngày chứng nhận. “Ngày quy chiếu” dùng ở công đoạn hoạt độ mẫu là mục khác, không thay cho ngày chứng nhận nguồn.

> THÔNG TIN TRÙNG PHẢI NHẤT QUÁN: Bộ chuẩn chỉ gộp điểm có detector, cấu hình, khoảng cách và ghi chú phù hợp. Phần mềm so khớp tên/ghi chú không phân biệt hoa thường, khoảng cách sai khác <0,01 mm; đây là quy tắc nhận dạng dữ liệu, không phải dung sai vật lý đã thẩm định cho phép đo.

Không sửa Live time/Real time chỉ để ép một điểm hiệu suất về đường mong muốn. Nếu CNF thiếu hoặc sai thời gian, xác minh nhật ký đo và lưu dấu vết mọi bổ sung trong hồ sơ.


<a name="section_10"></a>

## 10. Nhập chứng chỉ nguồn chuẩn

Vào tab 1. Nguồn và điểm chuẩn. Các số dưới đây phải đến từ chứng chỉ/tài liệu được kiểm chứng, không từ ví dụ minh họa trong tài liệu này.

| Ô nhập | Dữ liệu và đơn vị cần nhập |
| --- | --- |
| Mã nguồn duy nhất (mỗi đồng vị/chứng chỉ) | Giữ nguyên mã cho cùng nguồn/đồng vị/chứng chỉ ở nhiều vạch và nhiều phổ. Nguồn hỗn hợp dùng mã riêng cho từng đồng vị, không gán một hoạt độ chung cho mọi đồng vị. |
| Đồng vị | Tên đồng vị của nguồn; dùng nhất quán giữa chứng chỉ, dữ liệu gamma và các lần thêm điểm. |
| Chứng chỉ / số hiệu, đơn vị cấp | Ghi số chứng chỉ, đơn vị cấp và thông tin đủ truy xuất; không dùng tên file phổ thay chứng chỉ. |
| Hoạt độ tại ngày chứng nhận (Bq) | Tổng hoạt độ của đồng vị tại ngày trên chứng chỉ, đơn vị Bq. Không nhập kBq, Bq/kg hoặc hoạt độ đã tự giảm về ngày đo mà vẫn giữ ngày chứng nhận cũ. |
| Độ không đảm bảo hoạt độ trên chứng chỉ (%) | Nhập độ không đảm bảo tương đối được công bố (%). Nếu chứng chỉ ghi ±U Bq, đổi thành 100 × U/A trước khi nhập. |
| Hệ số phủ k | Nhập hệ số phủ tương ứng với độ không đảm bảo trên chứng chỉ, k ≥1. Giá trị 1 dùng khi số nhập đã là 1σ. |
| Ngày chứng nhận ISO + múi giờ | Thời điểm mà hoạt độ chứng chỉ có giá trị. Nếu chứng chỉ chỉ ghi ngày, cần xác minh quy ước giờ/múi giờ với hồ sơ/chuyên trách; không tự coi đó là thông tin chính xác đã biết. |
| Chu kỳ bán rã (ngày) | Đổi về ngày trước khi nhập; ví dụ T năm phải đổi theo quy ước ngày/năm được tài liệu nguồn nêu. |
| Sai số bán rã 1σ (ngày) | Độ không đảm bảo chuẩn tuyệt đối của bán rã theo ngày; không nhập % hoặc giá trị mở rộng khi chưa quy đổi. |

> VÍ DỤ QUY ĐỔI ĐỘ KHÔNG ĐẢM BẢO — GIẢ ĐỊNH: Chứng chỉ ghi A = 10000 Bq, U = 400 Bq với k = 2: nhập Hoạt độ = 10000; Độ không đảm bảo (%) = 4; k = 2. Phần mềm dùng u(A) = 200 Bq = 2% (1σ). Nếu đã chia U cho k trước để được 2%, phải nhập k = 1, tránh chia k hai lần.

Không nhập 0 chỉ vì chưa tìm được sai số: 0 nghĩa là coi thành phần đó chính xác. Cùng mã nguồn phải giữ nguyên cả chứng chỉ, hoạt độ, ngày, bán rã, sai số và tham chiếu; số liệu không nhất quán sẽ bị từ chối khi fit.


<a name="section_11"></a>

## 11. Ghép vạch gamma và hệ số K

Với mỗi ROI được chọn, nhập đúng dữ liệu của vạch gamma đang ghép. Phần mềm chưa tự nhận diện đồng vị hoặc quyết định vạch bị nhiễu.

| Ô nhập | Quy ước |
| --- | --- |
| Năng lượng vạch (keV) | Năng lượng phát gamma theo tài liệu hạt nhân. Kiểm tra với tâm Gaussian đã chuẩn năng lượng, không chỉ vị trí cực đại thô. |
| Xác suất phát gamma (%) | Nhập số theo phần trăm: Pγ = 85,1% thì nhập 85.1 hoặc 85,1; không nhập 0,851. |
| Sai số xác suất 1σ (điểm phần trăm) | Độ không đảm bảo chuẩn tuyệt đối trên thang %. Nếu Pγ = 85,1 ±0,2 điểm phần trăm, nhập 0.2; nội bộ là 0,851 ±0,002. |
| Nguồn dữ liệu Pγ và bán rã / phiên bản | Ghi cơ sở dữ liệu/tài liệu và phiên bản hoặc ngày truy cập. Ô này được dùng làm tham chiếu cho cả Pγ và bán rã; dùng nhất quán, không tạo tên tham chiếu giả để né kiểm tra. |
| K nhân diện tích đỉnh chuẩn | Hệ số nhân vào diện tích Gaussian. Mặc định 1; phải có căn cứ nếu dùng hiệu chỉnh. |
| Sai số tuyệt đối của K (1σ) | Nhập độ không đảm bảo chuẩn tuyệt đối; K = 1,02 với sai số tương đối 1% thì u(K) = 0,0102, không nhập 1. |
| Căn cứ K… | Mô tả hiệu chỉnh, phương pháp và tài liệu. Bắt buộc khi K khác 1 hoặc sai số K khác 0. |

### Nếu tài liệu cho sai số Pγ tương đối

Chuyển sang điểm phần trăm trước khi nhập: u_điểm% = Pγ(%) × u_tương đối(%)/100. Ví dụ giả định: Pγ = 50%, u tương đối = 1% (1σ) ⇒ nhập Pγ = 50 và Sai số = 0.5. Nếu là độ không đảm bảo mở rộng, chia hệ số phủ thích hợp trước.

### Hiểu đúng hệ số K

Ứng dụng không tự tính tự hấp thụ, trùng phùng tổng, suy giảm hay chuyển hình học. Nếu một tài liệu định nghĩa hệ số làm nhân vào tốc độ/diện tích để thu được giá trị đã hiệu chỉnh, dùng theo đúng chiều đó. Nếu hệ số được định nghĩa ngược, phải xác định phép nghịch đảo và lan truyền sai số trước; không đoán dấu/chiều hiệu chỉnh.

> TRÁNH HIỆU CHỈNH HAI LẦN: Không vừa dùng diện tích đã hiệu chỉnh từ ngoài, vừa nhập lại cùng K. Nguồn gần detector có thể chịu trùng phùng tổng; K = 1 chỉ là không áp dụng hiệu chỉnh, không phải chứng minh hiệu ứng không tồn tại.


<a name="section_12"></a>

## 12. Thêm điểm và phối hợp nhiều phổ

1. Chọn ROI trong danh sách ở đầu cửa sổ, kiểm tra toàn bộ thông tin nguồn, vạch gamma, thời gian và hình học.

2. Bấm Thêm ROI đã chọn làm điểm chuẩn. Kiểm tra dòng mới: Nguồn, keV, ε (fraction), σ (%), ROI / phổ. ε = 0,01 nghĩa là hiệu suất 1%, không phải 0,01%.

3. Đổi sang ROI tiếp theo, cập nhật năng lượng, Pγ, sai số và K/căn cứ nếu khác. Giữ nguyên dữ liệu nguồn khi vẫn cùng đồng vị/chứng chỉ; bấm thêm cho từng vạch.

4. Muốn thêm phổ khác: lưu JSON nháp để dự phòng, đóng cửa sổ hiệu suất, chọn/mở phổ nguồn tiếp theo, xử lý ROI, rồi mở lại Hiệu suất / Hoạt độ…. Các điểm trước vẫn còn trong phiên; kiểm tra thời gian/hình học mới trước khi thêm.

5. Nếu đổi sang nguồn/đồng vị khác, cập nhật mã nguồn và chứng chỉ tương ứng, nhưng chỉ gộp nếu cùng hình học bộ chuẩn. Dùng Xóa điểm đang chọn để bỏ một dòng sai rồi thêm lại đúng.

| Điều kiện của một điểm | Mức kiểm tra trong v0.7.0 |
| --- | --- |
| ROI định lượng | Fit thành công, có năng lượng và FWHM keV; diện tích dương và ≥3 lần sai số; χ²/ν ≤3. |
| Ghép năng lượng | Độ lệch tâm với vạch khai báo ≤ max(0,2 keV; FWHM_keV/2). Đây chỉ là kiểm tra gần năng lượng, không xác nhận đồng vị. |
| Hiệu suất | Dương, hữu hạn và không vượt 1 theo fraction. Giá trị bất thường cần kiểm tra đơn vị, thời gian, diện tích, phân rã và ghép vạch. |
| Độc lập của điểm | Không dùng lặp hoặc chồng ROI trong cùng phép đo làm điểm độc lập; tối đa 500 điểm. |
| Nguồn và cấu hình | Cùng mã nguồn phải cùng dữ liệu; các điểm trong bộ chuẩn phải cùng detector/hình học/khoảng cách/ghi chú. |

> ĐIỂM CHUẨN LÀ BẢN CHỤP DỮ LIỆU LÚC THÊM: Sau khi sửa ROI, chuẩn năng lượng hoặc phông trong cửa sổ chính, các điểm đã thêm không tự cập nhật. Phải xóa điểm cũ và thêm lại từ ROI mới, sau đó fit/lưu lại. Các trường ROI/phông/thời gian lưu trong JSON cho biết dữ liệu đã dùng khi thêm điểm.

Không đổi tên file hoặc sửa chuẩn năng lượng để giả làm phép đo độc lập. Dấu nhận dạng dùng số đếm và thời gian; việc thêm lặp không tạo thêm thông tin thống kê.


<a name="section_13"></a>

## 13. Fit đường chuẩn và đánh giá chất lượng

1. Mở tab 2. Đường chuẩn và phần dư. Kiểm tra số điểm, miền năng lượng và tính nhất quán nguồn trước khi chọn bậc.

| Bậc | Tối thiểu năng lượng khác nhau | Lưu ý |
| --- | --- | --- |
| 1 | 3 | Thử mô hình đơn giản khi phù hợp miền năng lượng. Không ép đường thẳng log–log lên dữ liệu có độ cong rõ. |
| 2 | 4 | Mặc định trên giao diện; cần đủ điểm và kiểm tra độ cong, đặc biệt gần hai đầu miền. |
| 3 | 5 | Chỉ dùng khi dữ liệu đủ dày và có căn cứ; bậc cao có thể dao động giữa các điểm. |

2. Bấm Fit đường chuẩn. Mô hình: ln ε = c0 + c1x + c2x² + c3x³, với x = ln(E_keV/1000); chỉ dùng các hệ số đến bậc đã chọn.

3. Đọc χ²/ν trong phần tóm tắt và quan sát đồ thị log–log: điểm ±1σ, đường fit và đồ thị phần dư phía dưới. Kiểm tra điểm lệch đơn lẻ, xu thế cong hoặc thay đổi bất thường theo năng lượng.

4. Nếu có điểm đáng ngờ, kiểm tra lại chứng chỉ, đơn vị %, ngày/giờ, phông, diện tích, ghép vạch và hiệu chỉnh. Chỉ loại điểm khi có căn cứ và ghi lại lý do; không xóa điểm chỉ để làm đẹp đường fit.

5. Nếu đổi tên, bậc hoặc hình học sau khi fit, phải fit lại trước khi tính hoạt độ hoặc xuất báo cáo chuẩn.

### Phần dư và ngưỡng của phần mềm

Phần dư hiển thị là r = ln(ε_đo/ε_fit) / σ_lnε đầu vào, không phải phần dư studentized. Khi χ²/ν >3 hoặc có |r| >3, ứng dụng cảnh báo và không cho dùng bộ chuẩn đó tính hoạt độ. Fit không suy biến và vượt ngưỡng không có nghĩa chuẩn đã được thẩm định.

### Sai số và tương quan được xử lý thế nào?

Fit dùng GLS trong miền log. Các điểm cùng mã nguồn giữ tương quan của hoạt độ chứng chỉ; cùng dữ liệu bán rã và cùng vạch gamma giữ tương quan theo nhận dạng/tham chiếu. Sai số diện tích ROI và K từng điểm được coi độc lập. Sai số hiển thị là 1σ tuyến tính hóa, không phải tự động là độ không đảm bảo mở rộng.

> MIỀN HIỆU LỰC: Chỉ dùng giữa năng lượng nhỏ nhất và lớn nhất của các điểm chuẩn; v0.7.0 không cho ngoại suy. Thêm vạch chuẩn thực tế ngoài miền nếu cần mở rộng, không tự nhập hệ số kéo dài đường cong. Một khoảng năng lượng lớn nhưng có ít điểm vẫn cần kiểm chứng giữa các điểm.


<a name="section_14"></a>

## 14. Công thức và ví dụ một điểm chuẩn

Công thức dưới đây giải thích cách phần mềm tính mỗi điểm; thao tác thông thường không cần tự tính trước. Luôn nhập hoạt độ tại ngày chứng nhận để tránh hiệu chỉnh phân rã hai lần.

> CÔNG THỨC ĐIỂM CHUẨN: λ = ln(2)/T1/2;  A_start = A_cert × exp(−λΔt)
f_decay = [1 − exp(−λR)]/(λR)
ε(E) = K_standard × N_peak / [A_start × L × f_decay × Pγ]

A_cert: hoạt độ trên chứng chỉ (Bq); Δt: từ ngày chứng nhận đến đầu phép đo; T1/2: bán rã, dùng cùng đơn vị thời gian với Δt. L: Live time (s); R: Real time (s); khi tính λR phải đổi bán rã về giây. N_peak: diện tích Gaussian, đơn vị số đếm; Pγ ở dạng fraction trong công thức.

f_decay xử lý biến thiên hoạt độ trong phép đo, giả định tỷ lệ live/real phân bố đều theo thời gian. Sai số thời điểm và Live/Real time chưa được nhập trong v0.7.0, được coi chính xác trong tính sai số.

### Ví dụ giả định — không phải chứng chỉ hoặc dữ liệu đồng vị thật

| Thông số giả định | Số nhập/giá trị |
| --- | --- |
| Hoạt độ tại ngày chứng nhận | A_cert = 10000 Bq; U = 4%, k = 2 ⇒ u(A) = 200 Bq (1σ). |
| Thời gian | Δt = 30 ngày; T1/2 = 1000 ngày; L = 1000 s; R = 1020 s. |
| Một vạch DEMO tại 500 keV | Pγ = 50%; u(Pγ) = 0,5 điểm phần trăm ⇒ 0,5 ±0,005 theo fraction. |
| ROI giả định đã fit đạt | N_peak = 50000 số đếm; u(N) = 500 số đếm. K_standard = 1; u(K) = 0. |

Tính tay: A_start ≈ 9794,203 Bq; f_decay ≈ 0,99999591; ε ≈ 0,0102102 (fraction) = 1,02102%.

Nếu tạm bỏ sai số bán rã trong ví dụ, u_r(ε) ≈ √[(500/50000)² + (200/10000)² + (0,005/0,5)²] ≈ 2,45% (1σ). Đây là sai số một điểm, không phải sai số đường chuẩn đã fit bằng GLS.

> MỘT ĐIỂM CHƯA TẠO THÀNH BỘ CHUẨN: Ví dụ chỉ kiểm tra phép tính/đơn vị. Không dùng năng lượng, bán rã hay xác suất DEMO để phân tích mẫu thật. Cần nhiều vạch nguồn thực, phân bố năng lượng phù hợp, fit và kiểm chứng độc lập theo mục 13 và 15.


<a name="section_15"></a>

## 15. Lưu JSON và kiểm chứng bộ chuẩn

### Lưu và tiếp tục công việc

1. Sau khi thêm điểm hoặc fit đạt, bấm Lưu bộ chuẩn JSON…. Đặt tên có detector, hình học, ngày và lần sửa, ví dụ HPGe01_GEOM-A_20261029_r01.json.

2. Có thể lưu bản nháp chưa đủ điểm để tiếp tục ở phổ khác. JSON nháp không phải bộ chuẩn định lượng; phải đủ điểm và fit đạt mới dùng tính hoạt độ.

3. Bấm Xuất báo cáo chuẩn TXT… sau fit để lưu điểm, sai số, hệ số, covariance, phần dư, nguồn và tham chiếu. Xuất TXT không thay cho lưu JSON để mở lại.

4. Dùng Mở bộ chuẩn JSON… để khôi phục. Phần mềm tính lại fit từ dữ liệu điểm đã lưu, không chỉ đọc các hệ số có sẵn. Xác minh đúng bộ chuẩn, hình học và miền năng lượng trước khi dùng.

> DỮ LIỆU NÀO ĐƯỢC GIỮ?: Điểm và bộ chuẩn được chia sẻ trong phiên app, còn khi đóng cửa sổ hiệu suất. Đóng app mà chưa lưu JSON có thể làm mất các thay đổi. JSON không nhúng vào CNF và không thay thế việc lưu CNF nguồn gốc, nhật ký, chứng chỉ, phông và báo cáo ROI.

### Tạo bộ chuẩn mới cho cấu hình khác

Trước hết lưu JSON bộ chuẩn đang có. Vào tab 1. Nguồn và điểm chuẩn → Xóa các điểm và xác nhận; cập nhật tên, detector/hình học/khoảng cách/ghi chú rồi bắt đầu thêm điểm mới. Việc xóa chỉ bỏ điểm trong phiên, không xóa file JSON đã lưu. Không gộp hai hình học trong cùng bộ chuẩn.

### Kiểm chứng trước khi đưa vào bài đo

Dùng phép đo kiểm chứng độc lập, vật liệu/nguồn có giá trị tham chiếu và độ không đảm bảo phù hợp. Không dùng lại chính phép đo đã tạo đường chuẩn làm mẫu kiểm chứng trong chức năng hoạt độ.

Giữ đúng hình học và cấu hình; kiểm tra chuẩn năng lượng, chất lượng ROI và phông. Thử nhiều năng lượng trong miền sử dụng thay vì chỉ một điểm gần giữa miền.

So sánh kết quả với giá trị tham chiếu kèm độ không đảm bảo theo tiêu chí đã quy định của phòng thí nghiệm/bài thực hành. v0.7.0 không tự đánh giá chứng nhận phù hợp hay tự đặt tiêu chí chấp nhận đo lường.

Khi thay detector, cấu hình hoặc hình học, phải đánh giá lại và tạo bộ chuẩn riêng khi cần. Không đổi ghi chú/nhãn để giả tạo sự trùng khớp.

### Các thành phần sai số còn cần người dùng đánh giá

Chưa mô hình hóa tương quan do dùng chung phông giữa nhiều phép đo, tương quan của các K có chung nguồn sai số, covariance hạt nhân giữa các vạch khác nhau, sai số thời điểm và Live/Real time. Nền mẫu, tự hấp thụ, trùng phùng và chuyển hình học không được tự hiệu chỉnh. Cần bổ sung đánh giá ngoài ứng dụng nếu quy trình định lượng yêu cầu.


<a name="section_16"></a>

## 16. Tính hoạt độ mẫu và Bq/kg

1. Mở phổ mẫu độc lập với các phổ đã tạo chuẩn. Kiểm tra năng lượng, áp dụng phông phù hợp và phân tích ROI đạt.

2. Mở Hiệu suất / Hoạt độ…, mở JSON chuẩn đã kiểm chứng. Kiểm tra detector, hình học, khoảng cách và ghi chú; năng lượng vạch phải nằm trong miền chuẩn.

3. Vào tab 3. Tính hoạt độ mẫu. Đánh dấu xác nhận khớp hình học chỉ sau khi kiểm tra điều kiện vật lý. Chọn đúng ROI ở danh sách phía trên.

4. Nhập đồng vị, năng lượng keV, xác suất gamma %, sai số điểm phần trăm 1σ, tham chiếu dữ liệu, bán rã và sai số theo ngày. Giữ nhất quán dữ liệu của cùng đồng vị/vạch/tham chiếu đã dùng tạo chuẩn.

5. Kiểm tra giờ đầu phép đo có múi giờ và Live/Real time. Để trống Ngày quy chiếu nếu muốn kết quả tại đầu phép đo; nếu nhập, phải dùng ISO có múi giờ.

6. Nhập K hoạt độ và sai số tuyệt đối/căn cứ nếu có. K mặc định 1 không tự bù sai hình học. Nhập khối lượng kg và sai số khối lượng kg nếu cần Bq/kg; để trống khối lượng nếu chỉ cần Bq.

7. Bấm Tính hoạt độ ROI, kiểm tra Bq tại đầu phép đo và tại ngày quy chiếu, ε, sai số và Bq/kg. Bấm Xuất báo cáo hoạt độ TXT… để lưu; thao tác xuất tính lại từ các ô hiện tại.

> CÔNG THỨC HOẠT ĐỘ: A_start = N_peak × K_sample / [L × ε(E) × Pγ × f_decay]
A_report = A_start × exp[λ(t_start − t_report)]
Bq/kg = A_report / m_kg

Ngày quy chiếu sau đầu phép đo thường làm hoạt độ giảm theo mô hình phân rã; ngày trước đầu phép đo làm giá trị quy về lớn hơn. Không đưa ngày chứng nhận nguồn chuẩn vào ô ngày quy chiếu mẫu nếu không có mục đích phù hợp.

### Cách đọc sai số

Các giá trị ± là sai số 1σ tuyến tính hóa của các thành phần đã nhập: diện tích, covariance đường hiệu suất, Pγ, bán rã, K và khối lượng; có xét tương quan Pγ/bán rã với bộ chuẩn khi cùng tham chiếu. Không tự xem ± là khoảng tin cậy 95% hoặc ngân sách độ không đảm bảo đã đầy đủ.

> GIỚI HẠN ĐỊNH LƯỢNG: Kết quả theo từng vạch ROI, chưa gộp nhiều vạch thành hoạt độ chung của một đồng vị. Chưa có chức năng xác định MDA/giới hạn phát hiện hoặc báo cáo mẫu “không phát hiện” theo một quy trình thống kê đã xác nhận.


<a name="section_17"></a>

## 17. Xuất dữ liệu và lưu hồ sơ

| Chức năng/file | Nội dung và cách sử dụng |
| --- | --- |
| Xuất CSV | Phổ đang chọn: kênh, keV nếu có, số đếm gốc, phông đã scale, net và sai số. Không phải bảng diện tích đỉnh. |
| Lưu PNG | Ảnh đồ thị ở chế độ/vùng đang hiển thị; kiểm tra tên, trục và zoom trước khi lưu. |
| Xuất tất cả ROI ra TXT… | Tất cả ROI, kể cả vùng fit lỗi; tổng gốc/phông/net, diện tích Gaussian, tâm/FWHM, trạng thái, thông tin dò và chuẩn hóa phông. |
| Lưu hiệu chuẩn vào CNF… | Chỉ chuẩn năng lượng; giữ số đếm gốc. Tạo .bak khi lưu chồng. |
| Lưu bộ chuẩn JSON… | Dữ liệu nguồn/chứng chỉ, điểm chuẩn, thời gian, hình học và thiết lập fit để mở lại; có thể là bản nháp. |
| Xuất báo cáo chuẩn TXT… | Báo cáo đường chuẩn đã fit: điểm, hệ số, covariance, phần dư và nguồn dữ liệu. |
| Xuất báo cáo hoạt độ TXT… | Điều kiện mẫu, ROI, ε, dữ liệu hạt nhân, phân rã, K, Bq/Bq/kg và các sai số được sử dụng. |

### Bộ hồ sơ tối thiểu cho một bài thực hành

CNF nguồn và mẫu gốc, CNF phông gốc, bản sao CNF sau chuẩn năng lượng nếu có và .bak liên quan.

Chứng chỉ nguồn, tài liệu hạt nhân/phiên bản, nhật ký detector/hình học/thời gian, căn cứ K và biên bản kiểm chứng bộ chuẩn.

Bộ chuẩn JSON được dùng, TXT đường chuẩn, TXT ROI, TXT hoạt độ và ảnh minh họa cần thiết. Lưu cùng tên phiên đo để dễ đối chiếu.

Ví dụ cấu trúc tên: 20261029_Nhom01_MauA_ROI.txt; HPGe01_GEOM-A_20261029_r01.json; 20261029_Nhom01_MauA_Activity.txt. Dùng tên riêng cho các lần xử lý để không nhầm bộ chuẩn cũ với bộ chuẩn đã cập nhật.

> LƯU Ý CỦA v0.7.0: Một số báo cáo TXT có dòng tiêu đề nội bộ “Gamma CNF Reader 0.6.0” do tái sử dụng mô-đun báo cáo từ bản trước. Điều này không tự chứng minh bạn đang chạy nhầm bản; xác minh phiên bản gói cài/cửa sổ ứng dụng và ghi rõ v0.7.0 trong hồ sơ. Không sửa nội dung báo cáo để che dấu nguồn gốc.

TXT là UTF-8, phân tách bằng tab; khi mở bằng bảng tính, chọn đúng UTF-8 và dấu tab, kiểm tra dấu thập phân để tránh biến cột số thành văn bản. Ứng dụng xử lý phổ tại máy, không tự gửi CNF lên dịch vụ ngoài.


<a name="section_18"></a>

## 18. Lỗi thường gặp và cách xử lý

| Hiện tượng/thông báo | Kiểm tra và xử lý |
| --- | --- |
| Không đọc được CNF | Xác minh là Canberra/Genie 2000, file không hỏng. Không phải mọi biến thể CNF đều được hỗ trợ; giữ bản gốc để đối chiếu. |
| Không lưu được chuẩn vào CNF | Layout không được hỗ trợ ghi hoặc file đã đổi bên ngoài/không có quyền ghi. Không ép sửa byte bằng tay; lưu báo cáo và xử lý trên bản được hỗ trợ. |
| Phông không áp dụng | Xem số kênh, căn chỉnh năng lượng, thời gian dương và tính độc lập. Kiểm tra tên phông thực sự đang áp dụng nếu vẫn còn phép trừ cũ. |
| Không thấy số đếm net âm | Tắt Log để xem tuyến tính; số đếm âm chỉ bị ẩn khi vẽ log. |
| ROI không có trong danh sách chuẩn | Fit phải thành công, có keV/FWHM và thống kê phù hợp. Kiểm tra ROI có phải nhóm nhiều đỉnh hoặc vùng bị cắt. |
| Diện tích <3σ hoặc χ²/ν >3 | Kiểm tra biên ROI, nền, đỉnh chồng và thống kê; không sửa sai số chỉ để vượt kiểm tra. Cần đo thêm hoặc phương pháp fit khác khi phù hợp. |
| Hiệu suất >1 hoặc quá bất thường | Kiểm tra Bq/kBq, Pγ theo %, diện tích đếm chứ không phải cps, Live time, ngày chứng nhận/đầu đo, K và ghép vạch. |
| Chưa đủ điểm/ma trận suy biến | Đủ bậc+2 năng lượng khác nhau; thêm điểm độc lập, phân bố rộng hơn hoặc giảm bậc có căn cứ. |
| Cùng mã nguồn nhưng dữ liệu khác | Đối chiếu chứng chỉ, ngày/giờ, bán rã, tham chiếu và sai số; giữ dữ liệu nhất quán. Không đổi mã để che sự không nhất quán. |
| ROI trùng/chồng bị từ chối | Xóa điểm đã nhập hoặc chọn ROI độc lập; bản sao file hoặc sửa chuẩn năng lượng không tạo phép đo mới. |
| Ngoài miền chuẩn | Dùng bộ chuẩn có miền chứa vạch mẫu, hoặc bổ sung nguồn/vạch thực tế và kiểm chứng lại; không ngoại suy. |
| Khác detector/hình học/ghi chú | Xác minh điều kiện thực. Chỉ sửa lỗi nhập nếu có bằng chứng; khác điều kiện thì dùng/tạo bộ chuẩn đúng. |
| Không cho tính hoạt độ từ bộ chuẩn | Kiểm tra χ²/ν, phần dư, fit cũ sau sửa thông tin, điều kiện ROI, miền năng lượng và mẫu có phải phổ đã tạo chuẩn. |
| K không được chấp nhận | K phải dương; sai số không âm; có căn cứ khi K ≠1 hoặc u(K) ≠0. Kiểm tra định nghĩa chiều nhân/chia. |
| Ngày giờ không hợp lệ | Dùng dạng 2026-10-29T10:30:00+07:00 hoặc Z nếu thực sự là UTC. Điền đúng múi giờ phép đo. |
| Mac chặn hoặc không chạy | Đúng chip, macOS 14+, chép app vào Applications. Với gói chính thức chưa notarize, dùng Privacy & Security → Open Anyway cho riêng app. |

Khi cần báo lỗi: ghi hệ điều hành/chip, phiên bản app, chuỗi thao tác, nội dung thông báo, trạng thái phông và loại đầu vào. Chỉ chia sẻ CNF/chứng chỉ khi được phép; không đưa thông tin nguồn nhạy cảm vào kênh công khai.


<a name="section_19"></a>

## 19. Phiếu ghi chép và kiểm tra cuối

Phiếu dùng kèm sổ tay đo; không thay chứng chỉ nguồn hoặc quy trình của phòng thí nghiệm. Có thể in trang này hoặc nhập trực tiếp trong Word.

| Thông tin hồ sơ chuẩn hiệu suất | Giá trị ghi chép |
| --- | --- |
| Người thực hiện / nhóm / ngày | ____________________________________________ |
| Detector / cấu hình MCA | ____________________________________________ |
| Mã hình học / khoảng cách (mm) | ____________________________________________ |
| Bình chứa / thể tích / nền mẫu / vị trí | ____________________________________________ |
| Nguồn / đồng vị / số chứng chỉ | ____________________________________________ |
| A_cert (Bq) / U (%) / hệ số phủ k | ____________________________________________ |
| Ngày chứng nhận + múi giờ | ____________________________________________ |
| CNF nguồn / CNF phông / ngày giờ đầu đo | ____________________________________________ |
| Live time (s) / Real time (s) | ____________________________________________ |
| Nguồn dữ liệu Pγ, bán rã / phiên bản | ____________________________________________ |
| Số điểm / bậc fit / miền keV | ____________________________________________ |
| χ²/ν / phần dư lớn nhất / điểm loại và lý do | ____________________________________________ |
| K, sai số, căn cứ / phép kiểm chứng độc lập | ____________________________________________ |
| Tên JSON / TXT chuẩn / người rà soát | ____________________________________________ |

### Kiểm tra trước khi chấp nhận bộ chuẩn

☐  Đúng chuẩn năng lượng; phông đúng; ROI đỉnh đơn; dùng diện tích Gaussian và sai số 1σ.

☐  Hoạt độ Bq tại ngày chứng nhận; U và k đúng; Pγ % và sai số điểm phần trăm đúng; bán rã theo ngày.

☐  Hình học, thời gian/múi giờ và dữ liệu cùng nguồn nhất quán; K có căn cứ, không hiệu chỉnh hai lần.

☐  Đủ năng lượng khác nhau; đã kiểm tra đường cong/phần dư; không ngoại suy; điểm thay đổi đã được thêm lại.

☐  Đã lưu JSON, TXT và hồ sơ gốc; đã kiểm chứng độc lập theo tiêu chí phòng thí nghiệm.

Căn cứ biên soạn: bản mã nguồn gắn tag v0.7.0 (giao diện Windows/macOS và GammaCnf.Core). Các tài liệu README có thể được cập nhật sau phát hành; ưu tiên đối chiếu tag khi cần truy nguyên chính xác.

[Mã nguồn và tài liệu cố định theo tag v0.7.0](https://github.com/lehoangminhhcmus/LeHoangMinh/tree/v0.7.0)
