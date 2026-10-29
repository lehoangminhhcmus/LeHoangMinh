# Gamma CNF Reader 0.4.0 — C# / WPF

App Windows 10/11 x64 đọc và phân tích phổ gamma Canberra/Genie 2000 `.CNF`. Bản self-contained không cần Python, Genie 2000 hay cài .NET riêng. Mở file, kéo thả hoặc truyền đường dẫn qua dòng lệnh.

## Cài đặt

Tải bộ cài tại [Releases](https://github.com/lehoangminhhcmus/LeHoangMinh/releases). Chạy `GammaCNFReader-CSharp-Setup.exe`, hoặc giải nén ZIP và chạy `GammaCNFReader.exe`. `Install.cmd` cài theo người dùng và tạo shortcut/mục gỡ cài đặt; không cần Administrator. Đóng app trước khi cập nhật/gỡ.

Bản thử nghiệm chưa ký số. Bộ cài cập nhật vào thư mục của bản C# trước đó. Không tự đổi ứng dụng mặc định cho đuôi `.cnf`.

## Nhập và lưu đường chuẩn năng lượng

Tab **Hiệu chuẩn**: nhập a0–a3 cho `E(ch) = a0 + a1·ch + a2·ch² + a3·ch³` (keV), kênh bắt đầu từ 0. Nhập dấu `.` hoặc `,` thập phân, không dùng dấu phân nhóm hàng nghìn. Bấm **Áp dụng hệ số** để cập nhật đồ thị và tất cả ROI. App từ chối hệ số không hữu hạn hoặc đường chuẩn không tăng trên miền kênh.

Bấm **Lưu hiệu chuẩn vào CNF…** rồi chọn lại file gốc để ghi chồng, hoặc chọn tên mới. Khi ghi đè, app xác nhận và tạo bản sao đầy đủ `.bak` với tên riêng, ghi file tạm, rồi thay file nguyên tử. App kiểm tra file gốc chưa bị chỉnh bên ngoài. Hệ số được lượng tử hóa theo số thực CAM 32-bit của CNF và đọc lại sau lưu.

Chỉ ghi trường hệ số/flags/số hệ số trong **ACQP chuẩn POLY/keV có layout đã kiểm chứng** (như file Test_spc.cnf đã cung cấp). Các biến thể khác có thể xem nhưng app sẽ từ chối ghi. **Số đếm và những byte khác giữ nguyên; số đếm sau trừ phông và ROI không được ghi vào CNF. Kết quả phân tích cũ đã có bên trong CNF không được tính lại và có thể không còn phù hợp sau thay hiệu chuẩn.** Cần phân tích lại khi mở trong Genie 2000.

## Trừ phông

Tab **Trừ phông**: chọn một file phông CNF độc lập, giữ chọn tỷ số thời gian sống hoặc nhập α thủ công, rồi bấm **Áp dụng trừ phông**.

- `α = live_mẫu / live_phông` theo mặc định; thiếu live time hợp lệ thì phải nhập α thủ công.
- `net[i] = mẫu[i] − α × phông[i]`.
- `variance[i] = mẫu[i] + α² × phông[i]`, giả định hai phép đo độc lập và α chính xác.
- Giữ nguyên số đếm âm, không làm tròn về uint32 hoặc ép về 0. Trên thang log, chỉ ẩn điểm ≤ 0 khi vẽ.
- Hai phổ phải cùng số kênh và lệch hiệu chuẩn không quá 0,05 kênh trên toàn phổ. Chưa tự rebin hay sửa trôi đỉnh. Nếu thiếu hiệu chuẩn, phải xác nhận cùng cấu hình MCA để trừ theo kênh.
- **Bỏ trừ phông** khôi phục phổ mẫu và tính lại ROI. Khi thay đường chuẩn, app kiểm tra lại tương thích với phông; nếu lệch, bỏ phông trước khi áp dụng.

CSV chứa kênh, keV, số đếm gốc, phông đã chuẩn hóa, số đếm net và sai số. Xử lý tại máy; không gửi file phổ lên dịch vụ ngoài.

## ROI và fit đỉnh

Giữ **Shift + kéo** trên phổ để thêm ROI, hoặc vào tab **ROI / Gaussian** nhập kênh đầu/cuối (bao gồm cả hai đầu). Kéo không Shift để zoom; con lăn zoom quanh con trỏ; Home/Toàn phổ để đặt lại. Mỗi ROI từ 9 đến 4096 kênh, tối đa 100 ROI.

App fit **một Gaussian tích phân theo từng kênh + nền tuyến tính** bằng bình phương tối thiểu có trọng số; trọng số lấy từ variance ở trên, có sàn 1 khi variance bằng 0. Có thể dùng trên phổ mẫu hoặc phổ sau trừ phông. Các thông số:

- Diện tích toàn Gaussian và sai số 1σ xấp xỉ từ ma trận hiệp phương sai của fit.
- Diện tích phần Gaussian nằm trong ROI (trong TXT), khác với tổng net của các kênh còn chứa nền liên tục.
- Vị trí tâm Gaussian theo kênh và keV; sai số tâm theo kênh trong TXT.
- `FWHM = 2√(2 ln 2) × σ` theo kênh; độ rộng keV bằng hiệu năng lượng tại hai điểm nửa chiều cao, phù hợp cả đường chuẩn phi tuyến.
- χ²/ν và trạng thái. χ²/ν > 3 được cảnh báo. Fit không hội tụ, ma trận suy biến, diện tích < 3σ hoặc đỉnh chạm giới hạn ROI được báo không đủ điều kiện, không hiển thị số liệu fit như kết quả hợp lệ.

Vùng ROI được tô nền và mô hình fit có đường đỏ. Thay hiệu chuẩn/phông sẽ tính lại tất cả ROI. **Xuất tất cả ROI ra TXT…** xuất báo cáo UTF-8 phân tách bằng tab, có cả ROI thất bại, đường dẫn mẫu/phông, thời gian sống, α, chế độ chuẩn hóa, hệ số hiệu chuẩn và mô tả phương pháp. ROI chỉ tồn tại trong phiên; xuất TXT trước khi đổi phổ/đóng app.

**Giới hạn:** mô hình đơn Gaussian không tách multiplet/đỉnh chồng, không mô tả tailing phức tạp; fit WLS và sai số chỉ là xấp xỉ, đặc biệt với số đếm thấp. Không tự nhận diện đồng vị hoặc tính hoạt độ. Cần kiểm chứng với phần mềm phân tích chuẩn trước khi dùng kết quả chính thức. Chưa đối chiếu với Genie 2000.

## Chuẩn hiệu suất đỉnh toàn phần (mới ở 0.4.0)

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

105 kiểm tra với file mẫu: đọc CNF, ghi/đọc lại hiệu chuẩn, bảo toàn byte/số đếm, backup/ghi nguyên tử, từ chối file bị đổi bên ngoài, kiểm tra căn chỉnh phông, bảo toàn net âm và variance, khôi phục tham số Gaussian có/không nhiễu và nền nghiêng, cảnh báo đỉnh chồng, từ chối ROI lỗi, xuất CSV/TXT, hiệu suất GLS, phân rã, covariance chung, JSON nháp/đầy đủ, hoạt độ/Bq/kg và các kiểm tra từ chối sai điều kiện. Không đưa file mẫu người dùng vào repository. Không có file mẫu thì chạy 96 kiểm tra chung và báo nhóm mẫu NOT RUN.

CI Windows xây dựng app, kiểm tra cú pháp script cài, chạy kiểm tra dữ liệu và smoke test WPF với CNF giả lập: mở phổ, hiệu chuẩn, trừ phông, ROI, vẽ log/keV và xuất CSV/TXT/PNG; tạo đường hiệu suất giả lập bằng giao diện, lưu/đọc JSON, vẽ đường chuẩn/phần dư và tính hoạt độ. Kết quả/ảnh nằm trong artifact `WPF-smoke-evidence`. Đây không thay thế kiểm tra thủ công trên PC của bạn hoặc kiểm thử cài/gỡ.

Cài thêm Inno Setup 6 và chạy `windows\build-windows.bat` để tạo portable exe và Setup.exe. GitHub Actions kiểm tra khi push main; tag `v*` sẽ tạo prerelease nếu các kiểm tra đạt.

Phạm vi CNF: một khối SPEC, 1–32.768 kênh uint32; thời gian giữ theo file, không tự suy diễn múi giờ. Tham khảo cấu trúc CAM/CNF: [SpecUtils CAMIO](https://github.com/sandialabs/SpecUtils/blob/master/src/CAMIO.cpp).
