# Gamma CNF Reader 0.8.0 — C# / WPF

App Windows 10/11 x64 đọc và phân tích phổ gamma Canberra/Genie 2000 `.CNF`. Bản self-contained không cần Python, Genie 2000 hay cài .NET riêng. Mở file, kéo thả hoặc truyền đường dẫn qua dòng lệnh.

## Cài đặt

Tải bộ cài tại [Releases](https://github.com/lehoangminhhcmus/LeHoangMinh/releases). Chạy `GammaCNFReader-CSharp-Setup.exe`, hoặc giải nén ZIP và chạy `GammaCNFReader.exe`. `Install.cmd` cài theo người dùng và tạo shortcut/mục gỡ cài đặt; không cần Administrator. Đóng app trước khi cập nhật/gỡ.

Bản phát hành mới nhất tải tại trang Releases, chưa ký số. Bộ cài cập nhật vào thư mục của bản C# trước đó. Không tự đổi ứng dụng mặc định cho đuôi `.cnf`.

## Chuyển giữa các phổ đang mở (mới ở v0.5.0)

Nút **Mở file…** cho chọn nhiều CNF cùng lúc; có thể kéo thả nhiều file hoặc truyền nhiều đường dẫn khi chạy app. Dùng **◀ Phổ trước / Phổ sau ▶**, danh sách phổ đang mở, hoặc **Ctrl+Tab / Ctrl+Shift+Tab** để chuyển. Tối đa 32 phổ cùng phiên; đường dẫn đầy đủ giúp phân biệt các file trùng tên.

Mỗi phổ giữ riêng: hiệu chuẩn đã áp dụng và các ô hệ số chưa áp dụng, phông/chuẩn hóa thời gian, ROI/kết quả, thang log/keV, vùng zoom, giới hạn ROI đang nhập. Chuyển phổ không đóng hoặc ghi file. Mở lại cùng đường dẫn sẽ chuyển tới bản đang có trong bộ nhớ, không tự nạp lại file ngoài app; muốn đọc lại dữ liệu thay đổi bên ngoài, đóng phổ đó rồi mở lại.

**Đóng phổ này** chỉ đóng phổ được chọn, có xác nhận khi còn hiệu chuẩn chưa lưu hoặc ROI trong phiên. Khi đóng app, hiệu chuẩn chưa lưu của tất cả phổ đều được kiểm tra. Lưu CNF thành tên của một phổ khác đang mở bị chặn để tránh làm dữ liệu trên màn hình sai lệch. Bộ chuẩn hiệu suất vẫn dùng chung trong phiên để gom các điểm từ nhiều phổ đúng hình học.

## Nhập và lưu đường chuẩn năng lượng

Tab **Hiệu chuẩn**: nhập a0–a3 cho `E(ch) = a0 + a1·ch + a2·ch² + a3·ch³` (keV), kênh bắt đầu từ 0. Nhập dấu `.` hoặc `,` thập phân, không dùng dấu phân nhóm hàng nghìn. Bấm **Áp dụng hệ số** để cập nhật đồ thị và tất cả ROI. App từ chối hệ số không hữu hạn hoặc đường chuẩn không tăng trên miền kênh.

Bấm **Lưu hiệu chuẩn vào CNF…** rồi chọn lại file gốc để ghi chồng, hoặc chọn tên mới. Khi ghi đè, app xác nhận và tạo bản sao đầy đủ `.bak` với tên riêng, ghi file tạm, rồi thay file nguyên tử. App kiểm tra file gốc chưa bị chỉnh bên ngoài. Hệ số được lượng tử hóa theo số thực CAM 32-bit của CNF và đọc lại sau lưu.

Chỉ ghi trường hệ số/flags/số hệ số trong **ACQP chuẩn POLY/keV có layout đã kiểm chứng** (như file Test_spc.cnf đã cung cấp). Các biến thể khác có thể xem nhưng app sẽ từ chối ghi. **Số đếm và những byte khác giữ nguyên; số đếm sau trừ phông và ROI không được ghi vào CNF. Kết quả phân tích cũ đã có bên trong CNF không được tính lại và có thể không còn phù hợp sau thay hiệu chuẩn.** Cần phân tích lại khi mở trong Genie 2000.

## Trừ phông

Tab **Trừ phông**: chọn CNF phông độc lập; app **tự áp dụng ngay** khi đủ điều kiện. Hệ số được tính từ thời gian của **phổ mẫu đang chọn**, không phải phổ mở gần nhất trước đó:

- Mặc định dùng **live time**: `α = live_mẫu / live_phông`.
- Có thể chọn **Real time** một cách rõ ràng: `α = real_mẫu / real_phông`. Thay chế độ sẽ tự tính lại; live time được khuyến nghị vì phù hợp chuẩn hóa số đếm khi có dead time.
- Ví dụ mẫu live=1.000 s, phông live=500 s: phông được nhân 2; mẫu live=2.000 s dùng cùng phông thì nhân 4. App hiển thị thời gian phông gốc → thời gian mục tiêu cùng α, không sửa thời gian/số đếm trong file CNF phông.
- Không cần nhập α thủ công. Nếu thời gian thiếu, bằng 0 hoặc không hữu hạn, app báo lỗi thay vì tự dùng α=1. Chọn loại thời gian có dữ liệu hợp lệ hoặc bổ sung thông tin phép đo trước khi trừ.
- `net[i] = mẫu[i] − α × phông[i]`; `variance[i] = mẫu[i] + α² × phông[i]` với giả định hai phép đo độc lập và α chính xác.
- Giữ net âm; trên thang log chỉ ẩn điểm ≤0 khi vẽ. File phông trùng chính phép đo mẫu bị từ chối.
- Hai phổ cần cùng số kênh và lệch hiệu chuẩn không quá 0,05 kênh. Chưa tự rebin/sửa trôi đỉnh. Khi thiếu hiệu chuẩn, cần xác nhận cùng cấu hình MCA rồi bấm **Áp dụng trừ phông** để thử lại.
- Khi chọn phông mới không hợp lệ, phép trừ hợp lệ trước đó vẫn được giữ; kiểm tra tên phông đang áp dụng ở phần thông tin phổ. **Bỏ trừ phông** bỏ cả phép trừ và file phông đang chọn của phổ này, không ảnh hưởng phổ khác.

CSV chứa kênh, keV, số đếm gốc, phông đã chuẩn hóa, số đếm net và sai số. Xử lý tại máy; không gửi file phổ lên dịch vụ ngoài.

## ROI và fit đỉnh

Giữ **Shift + kéo** trên phổ để thêm ROI, hoặc vào tab **ROI / Gaussian** nhập kênh đầu/cuối (bao gồm cả hai đầu). Kéo không Shift để zoom; con lăn zoom quanh con trỏ; Home/Toàn phổ để đặt lại. Mỗi ROI từ 1 kênh đến toàn phổ để cộng số đếm, tối đa 100 ROI; fit Gaussian cần 9–4096 kênh.

Bảng và TXT hiển thị song song:

- **Tổng gốc ROI**: tổng số đếm nguyên của phổ mẫu, gồm cả hai kênh biên (dùng uint64 để tránh tràn).
- **Phông đã scale**: α nhân tổng số đếm phông trong cùng ROI.
- **Tổng net ±1σ**: tổng gốc trừ phông đã scale; sai số bằng căn tổng `mẫu[i] + α² phông[i]`. Không có phông thì net bằng tổng gốc, σ=√tổng gốc. Giữ cả giá trị net âm/phân số.
- **Gauss ±1σ**: diện tích đỉnh thuần theo mô hình fit, như bản trước.

Tổng cộng trực tiếp **chưa trừ nền liên tục**, nên khác với diện tích Gaussian và không tự được dùng thay diện tích fit để tính hiệu suất/hoạt độ. Khi fit thất bại, ROI quá hẹp/rộng cho Gaussian hoặc không có đỉnh, vẫn xem/xuất được các tổng số đếm. TXT thêm các cột `Gross_ROI_sum`, `Scaled_background_ROI_sum`, `Net_ROI_sigma` và thời gian chuẩn hóa phông gốc/mục tiêu; `Net_ROI_sum` vẫn là tổng net trực tiếp.

App fit **một Gaussian tích phân theo từng kênh + nền tuyến tính** bằng bình phương tối thiểu có trọng số; trọng số lấy từ variance ở trên, có sàn 1 khi variance bằng 0. Có thể dùng trên phổ mẫu hoặc phổ sau trừ phông. Các thông số:

- Diện tích toàn Gaussian và sai số 1σ xấp xỉ từ ma trận hiệp phương sai của fit.
- Diện tích phần Gaussian nằm trong ROI (trong TXT), khác với tổng net của các kênh còn chứa nền liên tục.
- Vị trí tâm Gaussian theo kênh và keV; sai số tâm theo kênh trong TXT.
- `FWHM = 2√(2 ln 2) × σ` theo kênh; độ rộng keV bằng hiệu năng lượng tại hai điểm nửa chiều cao, phù hợp cả đường chuẩn phi tuyến.
- χ²/ν và trạng thái. χ²/ν > 3 được cảnh báo. Fit không hội tụ, ma trận suy biến, diện tích < 3σ hoặc đỉnh chạm giới hạn ROI được báo không đủ điều kiện, không hiển thị số liệu fit như kết quả hợp lệ.

Vùng ROI được tô nền và mô hình fit có đường đỏ. Thay hiệu chuẩn/phông sẽ tính lại tất cả ROI. **Xuất tất cả ROI ra TXT…** xuất báo cáo UTF-8 phân tách bằng tab, có cả ROI thất bại, đường dẫn mẫu/phông, thời gian sống, α, chế độ chuẩn hóa, hệ số hiệu chuẩn và mô tả phương pháp. ROI giữ riêng khi chuyển phổ trong phiên; xuất TXT trước khi đóng phổ hoặc đóng app.

**Giới hạn:** mô hình đơn Gaussian không tách multiplet/đỉnh chồng, không mô tả tailing phức tạp; fit WLS và sai số chỉ là xấp xỉ, đặc biệt với số đếm thấp. Không tự nhận diện đồng vị hoặc tính hoạt độ. Cần kiểm chứng với phần mềm phân tích chuẩn trước khi dùng kết quả chính thức. Chưa đối chiếu với Genie 2000.

## Dò đỉnh và tạo ROI tự động (mới ở v0.6.0)

Vào tab **ROI / Gaussian**, bấm **Dò đỉnh và tạo ROI tự động**. App dò toàn bộ **phổ đang chọn**, dùng số đếm gốc khi chưa trừ phông và dùng net/variance đã lan truyền khi có phông. Không cần chuẩn năng lượng để dò theo kênh. Dữ liệu gốc không bị làm trơn hoặc ghi lại; việc làm trơn chỉ dùng nội bộ để tìm ứng viên.

Thiết lập mặc định:

- FWHM tìm kiếm **3–40 kênh**; có thể chỉnh trong khoảng 2–256 kênh theo độ phân giải detector và số kênh phổ.
- Ngưỡng **SNR bộ lọc = 5**; có thể chỉnh 3–30. Giảm ngưỡng có thể tìm thêm đỉnh yếu nhưng cũng tăng ứng viên giả.
- Nửa rộng ROI khoảng **3,5 sigma** từ độ rộng ước lượng, có phần nền hai bên; có thể chọn 2,5–6 sigma. Giới hạn ROI được chặn trong phổ và có ít nhất 9 kênh để thử fit.

Thuật toán quét Gaussian nhiều độ rộng sau khi loại thành phần nền hằng/tuyến tính cục bộ khỏi kernel, dùng variance của số đếm để đánh giá SNR, loại phát hiện lặp giữa các độ rộng rồi ước lượng biên ROI. **SNR này không phải p-value hoặc kiểm định có hiệu chỉnh thử nhiều lần.** Nền cong, cạnh Compton, đuôi đỉnh và nhiễu có thể tạo ứng viên giả; đỉnh quá yếu/hẹp/rộng hoặc không tách được vẫn có thể bị bỏ sót.

Vạch đứt màu cam đánh dấu kênh ứng viên, nền ROI thể hiện vùng đã tạo và đường đỏ là Gaussian fit khi hợp lệ. Cột **Nguồn ROI** phân biệt Tự dò/Thủ công; rê chuột vào trạng thái để xem tham số và các kênh ứng viên.

- Luôn **giữ ROI thủ công**; đề xuất chồng ROI đang có sẽ bị bỏ và được đếm trong thông báo.
- Mặc định **Thay ROI tự động cũ** khi dò lại; bỏ chọn để chỉ bổ sung các vùng chưa chồng. Không tạo trùng ROI khi bấm dò nhiều lần. Giới hạn 100 ứng viên và 100 ROI mỗi phổ; chạm giới hạn được thông báo.
- Các ROI tự động chồng nhau được **gom nhóm** để tránh tính đếm lặp giữa các ROI tự động. Nhóm có nhiều ứng viên chỉ xuất tổng gốc/phông/net, **không tự công nhận fit một Gaussian**. Đây là nhóm ứng viên gần nhau, không phải kết quả tách multiplet. Có thể chỉnh/tách thủ công nếu biết mô hình phù hợp.
- Chọn dòng ROI trong bảng để nạp hai giới hạn vào ô nhập. Chỉnh rồi bấm **Cập nhật giới hạn ROI đang chọn** để tính lại; ROI đã chỉnh trở thành thủ công và được giữ khi dò lại. Có thể xóa từng ROI hoặc tất cả.
- Dò chạy nền; có nút **Hủy dò**. Có thể chuyển sang phổ khác trong lúc dò: kết quả chỉ áp dụng cho phổ đã khởi chạy, nếu phổ đó vẫn mở và dữ liệu chưa đổi. Đóng phổ hoặc xóa toàn bộ ROI sẽ hủy tác vụ tương ứng; hủy không thay ROI trước đó.
- Nếu thay hiệu chuẩn/phông trong lúc dò, kết quả cũ bị bỏ. Nếu thay sau khi đã dò, giữ vùng cũ và tính lại thống kê/fit nhưng đánh dấu thông tin dò là cũ; bấm dò lại để cập nhật. Tham số dò và nguồn ROI giữ riêng theo từng phổ trong phiên.

TXT bổ sung `Origin` và `Detection_note` chứa phương pháp, ngưỡng, độ rộng, kênh/SNR/FWHM ước lượng của ứng viên và cảnh báo nhóm/biên. Phân biệt FWHM **ước lượng để chọn ROI** với FWHM **fit Gaussian** trong bảng định lượng. Chỉ ROI fit đạt mới có thể dùng cho chuẩn hiệu suất/hoạt độ; app không tự nhận diện đồng vị.

## Chuẩn hiệu suất nhập gọn (v0.8.0)

Mặc định chỉ cần chọn ROI Gaussian đạt, nhập **hoạt độ tại đầu phép đo (Bq)** và
**xác suất gamma (%)**. Live time từ CNF; năng lượng để trống lấy từ tâm ROI.
Các ô chứng chỉ, mã nguồn, đồng vị, hình học, khoảng cách, tham chiếu và sai số
đều có thể để trống. Xem [hướng dẫn nhập gọn](../docs/Chuan_hieu_suat_nhap_gon_v0.8.0.md).
Bỏ trống sai số được ghi là chưa cung cấp, không phải sai số thật bằng 0; ± chỉ
phản ánh thành phần có dữ liệu. Không nhập bán rã nghĩa là chưa hiệu chỉnh phân
rã trong phép đo. Nếu chọn **Tự quy đổi A từ ngày chứng nhận**, mới cần thêm các
ngày có múi giờ, bán rã và real time. JSON mới schema 2; đọc được schema 1 cũ.

Phần dưới mô tả quy trình đầy đủ với thông tin bổ sung, không phải danh sách
bắt buộc của chế độ nhập gọn. Điều kiện vật lý về hình học và chất lượng ROI/
đường chuẩn vẫn áp dụng.

## Chuẩn hiệu suất đỉnh toàn phần

Mở phổ nguồn chuẩn, áp dụng đúng đường chuẩn năng lượng, trừ phông nếu cần và fit các ROI. Bấm **Hiệu suất / Hoạt độ…**. App không cài sẵn đường hiệu suất hay tự điền hoạt độ/chu kỳ bán rã/xác suất gamma thực tế; các dữ liệu này phải lấy từ chứng chỉ và tài liệu hạt nhân đã kiểm chứng.

### Điều kiện đo và nguồn chuẩn

1. Nhập **detector, mã hình học/cấu hình mẫu, khoảng cách mm và ghi chú** (bình chứa, thể tích, thành phần nền mẫu…). App so khớp toàn bộ các mục này khi áp dụng hiệu suất. Tên trùng không chứng minh điều kiện vật lý trùng: người dùng vẫn phải kiểm tra thực tế.
2. Xác nhận live/real time. Nhập thời điểm bắt đầu và ngày chứng nhận theo ISO có múi giờ, ví dụ `2026-10-29T10:30:00+07:00`. CNF không ghi múi giờ nên app không tự thêm offset. Live time phải dương và không vượt real time.
3. Ở tab **Nguồn và điểm chuẩn**, nhập mã nguồn duy nhất, đồng vị, tham chiếu chứng chỉ, hoạt độ **Bq**, độ không đảm bảo tương đối **%** và hệ số phủ **k**. App quy đổi độ không đảm bảo trên chứng chỉ thành 1σ bằng cách chia cho k. Nhập bán rã và sai số 1σ theo **ngày**. Giữ nguyên thông tin cùng mã nguồn giữa các phổ; mỗi đồng vị trong nguồn hỗn hợp dùng một mã riêng.
4. Chọn ROI rồi nhập năng lượng vạch **keV**, xác suất phát **%**, sai số xác suất **điểm phần trăm** (1σ), cùng tham chiếu/phiên bản dữ liệu Pγ và bán rã. Ví dụ Pγ=85,1% với sai số 0,2 điểm phần trăm tương ứng 0,851 ± 0,002 theo fraction. Nhập 0 cho sai số nghĩa là coi thành phần đó chính xác, làm ngân sách sai số không đầy đủ nếu thực tế có độ không đảm bảo.
5. Nếu có hiệu chỉnh diện tích nguồn chuẩn, nhập K nhân vào diện tích và sai số tuyệt đối của K, kèm căn cứ. Mặc định K=1, σK=0. App **không tự tính** tự hấp thụ, trùng phùng tổng hoặc suy giảm. Với nguồn gần detector, cần xác định các hiệu chỉnh này trước; đường chuẩn chưa hiệu chỉnh có thể chỉ là hiệu suất biểu kiến và không chuyển được sang đồng vị khác.
6. Bấm **Thêm ROI đã chọn làm điểm chuẩn**. ROI cần fit đạt, diện tích ≥3σ, χ²/ν ≤3 và tâm khớp năng lượng khai báo trong nửa FWHM (ngưỡng tối thiểu 0,2 keV). Việc ghép vạch do người dùng xác nhận; app chưa tự nhận diện đồng vị hoặc đánh giá nhiễu vạch.

Với λ=ln(2)/T, Δt từ ngày chứng nhận đến bắt đầu đo và R=real time:

```text
A_start = A_certificate × exp(−λ Δt)
f_decay = (1 − exp(−λ R)) / (λ R)
epsilon(E) = K_standard × N_peak / (A_start × live_time × f_decay × P_gamma)
```

`f_decay` giả định tỷ lệ live/real phân bố đều trong phép đo. Sai số thời điểm và live/real chưa được nhập, được coi chính xác. Hiệu suất lưu dưới dạng fraction (0,01 = 1%).

### Fit, lưu và kiểm tra đường chuẩn

Ở tab **Đường chuẩn và phần dư**, chọn bậc 1–3 rồi Fit. Mô hình:

```text
ln epsilon = c0 + c1·x + c2·x² + c3·x³, x = ln(E_keV/1000)
```

Fit GLS trong miền log có ma trận covariance: sai số hoạt độ chứng chỉ chung của cùng mã nguồn được giữ tương quan; bán rã và cùng vạch gamma được giữ tương quan khi cùng đồng vị/tham chiếu dữ liệu. Sai số ROI và K từng điểm coi độc lập. Chưa mô hình hóa tương quan do tái sử dụng cùng phông giữa các phép đo hoặc các K có chung nguồn sai số, hay covariance hạt nhân giữa các vạch khác nhau. Sai số 1σ được tuyến tính hóa; không phải độ không đảm bảo mở rộng hoàn chỉnh.

Cần tối thiểu **bậc+2 năng lượng khác nhau**; nên có nhiều vạch phân bố rộng, tránh đa thức bậc cao khi dữ liệu thưa. Không dùng lặp/chồng ROI của cùng phép đo làm điểm độc lập. Dấu nhận dạng phép đo dựa trên số đếm/thời gian, không đổi khi sửa hiệu chuẩn hoặc đổi tên file.

Đồ thị thể hiện điểm ±1σ, đường fit và phần dư ln(ε đo/ε fit)/σ_lnε đầu vào. Đây không phải phần dư studentized. χ²/ν hoặc |phần dư| >3 sẽ cảnh báo và chặn tính hoạt độ từ bộ chuẩn đó. Không cho hiệu suất >1 và **không ngoại suy** ngoài min–max năng lượng chuẩn.

**Lưu bộ chuẩn JSON** lưu chứng chỉ, điểm đo, thông tin ROI/phông/hiệu chỉnh và cấu hình; khi mở, app tính lại đường fit từ dữ liệu gốc, không tin vào hệ số không kiểm chứng. Có thể lưu **bản nháp chưa đủ điểm** để tiếp tục trên phổ khác. Các điểm cũng còn trong phiên khi đóng cửa sổ hiệu suất/mở phổ khác; đóng app khi chưa lưu sẽ được nhắc. Không ghi bộ hiệu suất vào CNF.

**Xuất báo cáo chuẩn TXT** có các điểm, sai số, phần dư, hệ số và covariance cùng tham chiếu nguồn. Điểm chưa fit không thể xuất thành báo cáo đường chuẩn hợp lệ.

## Tính hoạt độ và Bq/kg

Mở **phổ mẫu độc lập** (không dùng lại phổ đã tạo chuẩn), fit ROI rồi mở cửa sổ hiệu suất. Mở JSON chuẩn, kiểm tra detector/hình học/khoảng cách/ghi chú, đánh dấu xác nhận và nhập dữ liệu đồng vị/vạch ở tab **Tính hoạt độ mẫu**. Nhập ngày quy chiếu nếu muốn (trống = đầu phép đo). Nhập khối lượng kg và sai số kg nếu cần Bq/kg.

```text
A_start = N_peak × K_sample / (live_time × epsilon × P_gamma × f_decay)
A_report = A_start × exp[λ × (measurement_start − report_date)]
Bq/kg = A_report / mass_kg
```

K_sample mặc định 1, có sai số và căn cứ khi khác 1; không dùng K để bỏ qua kiểm tra sai hình học. App không tự tính hiệu chỉnh tự hấp thụ, trùng phùng tổng hay nền mẫu. Cần hình học và đặc tính suy giảm mẫu phù hợp với chuẩn, hoặc mô hình chuyển đổi được xác nhận riêng.

Kết quả có Bq ở đầu phép đo, Bq tại ngày quy chiếu và Bq/kg, cùng sai số 1σ. Lan truyền covariance đường chuẩn, diện tích, Pγ, bán rã, K và khối lượng; tính tương quan Pγ/bán rã với bộ chuẩn khi cùng tham chiếu. Thông tin cùng tham chiếu phải nhất quán. Các thành phần chưa cung cấp được coi chính xác. Kết quả **theo từng vạch ROI**, chưa gộp nhiều vạch thành hoạt độ đồng vị chung.

**Xuất báo cáo hoạt độ TXT** tính lại từ các ô hiện tại, ghi điều kiện, ROI, bộ chuẩn, dữ liệu hạt nhân, K, hiệu chỉnh phân rã và ngân sách sai số được dùng. Chưa có xác nhận bằng nguồn chuẩn thực tế/so sánh Genie 2000; kiểm tra với vật liệu chuẩn hoặc phép đo kiểm chứng độc lập trước khi dùng kết quả chính thức.

## Phát triển và kiểm tra

Cài .NET 10 SDK x64, chạy trong thư mục repository:

```console
dotnet run --project windows/GammaCnf.App
dotnet run --project windows/GammaCnf.Checks
```

Có file mẫu riêng:

```console
dotnet run --project windows/GammaCnf.Checks -- "D:\Data\Test_spc.cnf"
```

155 kiểm tra với file mẫu: đọc CNF, ghi/đọc lại hiệu chuẩn, bảo toàn byte/số đếm, backup/ghi nguyên tử, từ chối file bị đổi bên ngoài, kiểm tra căn chỉnh phông, bảo toàn net âm và variance, khôi phục tham số Gaussian có/không nhiễu và nền nghiêng, cảnh báo đỉnh chồng, từ chối ROI lỗi, xuất CSV/TXT, hiệu suất GLS, phân rã, covariance chung, JSON nháp/đầy đủ, hoạt độ/Bq/kg và các kiểm tra từ chối sai điều kiện. Không đưa file mẫu người dùng vào repository. Không có file mẫu thì chạy 144 kiểm tra chung và báo nhóm mẫu NOT RUN.

CI Windows xây dựng app, kiểm tra cú pháp script cài, chạy kiểm tra dữ liệu và smoke test WPF với CNF giả lập: mở phổ, hiệu chuẩn, trừ phông, ROI, vẽ log/keV và xuất CSV/TXT/PNG; tạo đường hiệu suất giả lập bằng giao diện, lưu/đọc JSON, vẽ đường chuẩn/phần dư và tính hoạt độ; mở/chuyển/đóng nhiều phổ, giữ riêng phông/ROI/bản nháp hệ số/log/zoom, tính lại tỷ số thời gian theo từng phổ và kiểm tra các tổng ROI; dò đỉnh bất đồng bộ, tạo/fit ROI, dò lặp, giữ ROI thủ công, chỉnh ROI, hủy tác vụ, cảnh báo dữ liệu đã đổi và xuất nguồn gốc ROI. Kết quả/ảnh nằm trong artifact `WPF-smoke-evidence`. Đây không thay thế kiểm tra thủ công trên PC của bạn hoặc kiểm thử cài/gỡ.

Cài thêm Inno Setup 6 và chạy `windows\build-windows.bat` để tạo portable exe và Setup.exe. GitHub Actions kiểm tra khi push main; tag `v*` sẽ phát hành bản Latest nếu các kiểm tra đạt.

Phạm vi CNF: một khối SPEC, 1–32.768 kênh uint32; thời gian giữ theo file, không tự suy diễn múi giờ. Tham khảo cấu trúc CAM/CNF: [SpecUtils CAMIO](https://github.com/sandialabs/SpecUtils/blob/master/src/CAMIO.cpp).

## Ngưỡng amplitude tối thiểu — v0.9.1

Trong phần dò đỉnh tự động, nhập **Amplitude tối thiểu (counts/kênh trên nền)**
rồi bấm dò lại. Ví dụ `500` chỉ tạo ROI để fit tự động cho đỉnh có chiều cao
Gaussian ước lượng ≥500 trên nền liên tục và đủ SNR. Giá trị `0` bỏ lọc chiều cao.
Đây là chiều cao trên phổ đang dùng (sau trừ phông nếu có), không phải diện tích
hay số đếm toàn phần tại kênh đỉnh. Ngưỡng giữ riêng theo phổ; ROI thủ công giữ
nguyên. Báo cáo TXT ghi ngưỡng và amplitude ước lượng.

## Thông tin tác giả

Lê Hoàng Minh

Bộ môn Vật lý hạt nhân

Khoa Vật lý-Vật lý Kỹ thuật

Trường Đại học khoa học tự nhiên

Đại học Quốc gia Thành phố Hồ Chí Minh

email: hoangminh@hcmus.edu.vn

Trong phần mềm, bấm **Giới thiệu** để xem thông tin tác giả và phiên bản.

## Gợi ý đồng vị — v0.10.0

Sau khi chọn/fit ROI, mở tab **Gợi ý đồng vị**, chọn thư mục thư viện NLB của
Genie hoặc CSV. Xem đồng vị khả dĩ, ΔE và các ROI hỗ trợ; chỉnh dung sai theo
detector. Đây là gợi ý theo năng lượng, chưa xác nhận đồng vị.

[Hướng dẫn nạp thư viện và sử dụng](../docs/Goi_y_dong_vi_v0.10.0.md).
