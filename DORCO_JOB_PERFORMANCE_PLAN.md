# Kế hoạch tổng hợp Dorco: hiệu năng job và in SSCC Zebra

Ngày ghi nhận: 16/10/2026.

Cập nhật tổng hợp: 17/10/2026.

## Tổng quan hai hạng mục

| Hạng mục | Vấn đề | Hướng xử lý chính | Điều kiện nghiệm thu |
| --- | --- | --- | --- |
| A — Hiệu năng job | Nạp lại toàn bộ XML/CSV khi mở, vẽ và tìm kiếm danh sách; chậm khi có job lớn | Tách metadata và dữ liệu chi tiết, cache danh sách, tra hộp bằng Dictionary, tải nền | Mở lại danh sách 100 job không nạp toàn bộ mã; UI phản hồi và dữ liệu khôi phục chính xác |
| B — Tem SSCC Zebra | Nét 1 dot quét chập chờn, tăng 2 dot với cách mã hóa hiện tại bị tràn tem | Thử GS1-128 Code Set C cho SSCC dạng số; kiểm tra kích thước rồi chọn độ rộng nét phù hợp | Tem vừa vùng in, đủ khoảng trắng, giữ nguyên SSCC và quét ổn định trên thiết bị thực tế |

Hai hạng mục độc lập về nguyên nhân. Sửa và kiểm thử riêng từng phần trước khi ghép bản triển khai; không coi tối ưu job là cách sửa chất lượng barcode.

Trạng thái: **Chỉ lập kế hoạch. Chưa sửa mã nguồn, chưa triển khai.**

## 1. Hiện tượng và bằng chứng

- Ứng dụng mở danh sách và thao tác chậm khi giữ các job lớn.
- Cùng một job xuất hiện nhiều lần trong log `LoadProductCodesFromCsv`.
- Khi chỉ giữ job S0B nhỏ, người dùng xác nhận ứng dụng nhanh. Log vẫn ghi 11 lần nạp 168 mã trong khoảng 3,2 giây.
- Người dùng cũng xác nhận sau khi giảm dữ liệu trong `C:\ProgramData\R-Link\Jobs` và `C:\ProgramData\R-Link\ProductCodesDroco`, ứng dụng mượt lại. Do thay đổi cả hai thư mục, chưa đo riêng mức ảnh hưởng của từng thư mục.

Dữ liệu đã kiểm tra trước khi dọn:

| Job | Số hộp | Số dòng mã trong CSV |
| --- | ---: | ---: |
| 260923_155016_LINE_00_ZKD | 4.226 | 42.260 |
| 261002_144139_LINE_00_0YT | 15.122 | 151.220 |
| 261009_082619_LINE_00_ZXM | 15.122 | 151.220 |
| 261016_101301_LINE_00_S0B | 14 | 168 |

Log ngày 16/10 ghi nhận khoảng cách khoảng 26–27 giây giữa một số lần hoàn tất nạp job lớn. Đây là khoảng cách giữa các dòng log, **không phải phép đo riêng thời gian thực thi hàm**.

Nguồn dữ liệu kiểm tra:

- Dữ liệu tại `C:\ProgramData\R-Link`.
- Bản dữ liệu người dùng cung cấp tại `D:\R-Link-16102026\R-Link`.

## 2. Nhận định và giới hạn

Điểm nghẽn có bằng chứng mạnh nhất là đọc đầy đủ job nhiều lần trên luồng giao diện, kết hợp dò tuần tự danh sách hộp cho từng mã CSV.

Luồng gọi hiện tại:

`Sự kiện giao diện -> Shared.GetJob() -> JobModel.LoadFile() -> LoadProductCodesFromCsv()`

- `frmJobDroco.ListBoxJobList_DrawItem`: đọc toàn bộ job chỉ để lấy `PrinterSeries` khi vẽ dòng.
- `TxtSearch_TextChanged`: đọc các job khớp tên khi nội dung tìm kiếm thay đổi.
- `LoadJobNameList`: vòng lặp đọc job nằm trong `Invoke`, nên vẫn chạy trên luồng giao diện.
- `LoadJobListToComboBox` và các danh sách liên quan: tiếp tục đọc job để lọc trạng thái.
- `JobModel.LoadProductCodesFromCsv`: mỗi dòng gọi `BoxList.FirstOrDefault`, chi phí tăng theo số mã nhân số hộp.

Chưa xác định vì sao đúng ngày này mới chậm rõ: cần đối chiếu phiên bản EXE/DLL, lượng dữ liệu và điều kiện máy với ngày trước. Chưa có kết quả profiling ứng dụng đang chạy.

Log tổng lớn (khoảng 1,5 GB trong bản sao; `log.txt` gần 790 MB) nhưng mã logger hiện tại ghi nối tiếp. Chưa có căn cứ coi kích thước log là nguyên nhân chính.

Đường dẫn dữ liệu phải được xác minh trên bản chạy: `CommonVariable/CommVariables.cs` hiện có hỗ trợ `datapath.cfg`, và đường dẫn mặc định trong source khác đường dẫn xuất hiện trong log. Không mặc định mọi bản build đều dùng `C:\ProgramData\R-Link`.

## 3. Kế hoạch sửa theo ưu tiên

### Đợt 1 — Đo và loại bỏ điểm nghẽn trực tiếp (P0)

- [ ] Thêm phép đo thời gian cho đọc XML, đọc/gắn CSV và tổng thời gian mở danh sách/mở job.
- [ ] Log lý do nạp và mã yêu cầu để phân biệt mở danh sách, chọn job, vẽ, tìm kiếm; không log toàn bộ mã sản phẩm.
- [ ] Thay dò `FirstOrDefault` từng dòng bằng bảng tra `Dictionary` tạo một lần theo mã hộp.
- [ ] Quy định rõ cách xử lý mã hộp trùng: giữ tương thích hành vi hiện tại hoặc báo dữ liệu bất thường, không để `ToDictionary` gây lỗi không kiểm soát.
- [ ] Đọc CSV tuần tự bằng `File.ReadLines`, giữ nguyên ký tự điều khiển GS1 và quy tắc tách ở dấu phẩy đầu tiên.
- [ ] Loại bỏ đọc file khỏi `DrawItem` và tìm kiếm; dùng thông tin job đã nạp trong bộ nhớ.
- [ ] Kiểm tra job cũ có mã trong XML và job mới dùng CSV riêng để không nạp trùng hoặc làm mất dữ liệu.

### Đợt 2 — Tách tải danh sách và tải dữ liệu sản xuất (P0/P1)

- [ ] Tạo mô hình tóm tắt chứa tên, trạng thái, loại job và `PrinterSeries` cùng các trường thực sự cần cho danh sách.
- [ ] Tách API đọc thông tin tóm tắt khỏi API nạp đầy đủ job. Đọc tóm tắt không nạp CSV và tránh dựng toàn bộ danh sách mã lồng trong XML cũ.
- [ ] Dùng chung cache thông tin tóm tắt cho danh sách, tìm kiếm và combobox.
- [ ] Chỉ nạp đầy đủ mã sản phẩm khi nghiệp vụ mở/chạy/tra cứu job yêu cầu.
- [ ] Đưa I/O và xử lý dữ liệu ra nền; chỉ bind và cập nhật điều khiển trên luồng giao diện.
- [ ] Gộp yêu cầu đang nạp cùng job; tránh đăng ký sự kiện trùng và kích hoạt tải lặp khi bind danh sách.
- [ ] Bỏ kết quả lỗi thời khi người dùng chuyển job hoặc đóng form trong lúc tải.
- [ ] Làm mới cache khi tạo/sửa/xóa job; có thao tác tải lại để nhận thay đổi từ bên ngoài hoặc chế độ client.
- [ ] Không cache đầy đủ mã của tất cả job trong RAM. Giới hạn vòng đời dữ liệu chi tiết theo job đang dùng.
- [ ] Kiểm tra tất cả nơi gọi API dùng chung để bảo đảm không thay đổi hành vi các project khác.

### Đợt 3 — Đánh giá hiệu năng trong sản xuất (P2)

- [ ] Đo thời gian và tần suất `SaveFile`, `SaveProductCodesToCsv` và các hàm cập nhật `AllValues`.
- [ ] Xem xét giảm số lần ghi toàn bộ file, cập nhật theo lô hoặc lưu thay đổi tăng dần dựa trên kết quả đo.
- [ ] Thiết kế tính nhất quán giữa XML, CSV và AllValues; kiểm tra khôi phục sau lỗi ghi hoặc mất điện trước khi thay đổi cách lưu.
- [ ] Đánh giá chính sách xoay vòng log riêng sau khi giải quyết việc tải job; không dùng dọn log làm biện pháp sửa chính.

## 4. Vị trí mã nguồn cần kiểm tra

- `BarcodeVerificationSystem/View/OtherProjects/DrocoUI/frmJobDroco.cs`: vẽ danh sách, tìm kiếm, tải danh sách/combobox, chọn job.
- `BarcodeVerificationSystem/Controller/Shared.cs`: `GetJob`, `GetJobNameList`.
- `BarcodeVerificationSystem/Model/JobModel/JobModel.cs`: `LoadFile`, `LoadProductCodesFromCsv`, `SaveFile`, `SaveProductCodesToCsv`.
- `BarcodeVerificationSystem/View/OtherProjects/DrocoUI/frmMainDroco.cs`: tải job, `LoadProgressFromAllValues`, luồng cập nhật sản xuất.
- `BarcodeVerificationSystem/Modules/ReliableDataSender/Services/Droco/DrocoAllValueProcess.cs`: cập nhật và đọc toàn bộ AllValues.
- `CommonVariable/CommVariables.cs`: xác định đường dẫn dữ liệu thực tế.

Workspace đang có các thay đổi chưa commit, bao gồm `JobModel.cs` và `DrocoPackagingManager.cs`. Khi triển khai phải kiểm tra diff và giữ nguyên các thay đổi có sẵn không thuộc phạm vi sửa.

## 5. Kiểm thử và tiêu chí chấp nhận

Thực hiện trên bản sao dữ liệu, cùng máy và cùng bộ dữ liệu để so sánh trước/sau. Không dùng trực tiếp dữ liệu sản xuất để thử thay đổi cách lưu.

- [ ] Đo riêng lần mở đầu và các lần mở tiếp theo với S0B, ZKD, ZXM, 0YT.
- [ ] Cuộn, vẽ lại, chọn dòng và gõ tìm kiếm không phát sinh đọc CSV mã sản phẩm ngoài yêu cầu mở job thực sự.
- [ ] Mở danh sách không nạp đầy đủ mã sản phẩm của tất cả job.
- [ ] Một yêu cầu mở job không tạo nhiều lượt nạp CSV trùng nhau.
- [ ] Giao diện phản hồi khi nạp job lớn; chuyển job nhanh không hiển thị nhầm kết quả tải cũ.
- [ ] Tạo/sửa/xóa job và tải lại danh sách phản ánh đúng trạng thái.
- [ ] Đối chiếu số hộp, mã và quan hệ hộp/thùng/pallet trước/sau; không trùng, không mất mã, giữ nguyên GS1.
- [ ] Kiểm tra CSV thiếu, CSV lỗi, mã hộp trùng và job XML cũ; không âm thầm coi dữ liệu thiếu là khôi phục thành công.
- [ ] Kiểm tra mở lại ứng dụng, tiếp tục job và nghiệp vụ in lại trên môi trường kiểm thử.
- [ ] Build cấu hình Dorco phù hợp và kiểm tra ảnh hưởng tới các project dùng chung `JobModel`.

Mục tiêu thời gian cụ thể sẽ chốt sau khi có baseline đo đúng từng công đoạn; chưa cam kết số giây khi chưa benchmark.

## 6. Biện pháp tạm thời và bảo toàn dữ liệu

- Chỉ giữ các job cần dùng trong thư mục Jobs; lưu trữ job hoàn thành bên ngoài thư mục đang quét.
- Đóng ứng dụng trước khi di chuyển hoặc khôi phục dữ liệu.
- `ProductCodesDroco` là dữ liệu liên kết mã sản phẩm với hộp, **không phải cache được phép xóa tùy ý**.
- Nếu đã xóa CSV, kiểm tra và khôi phục từ bản sao tương ứng trước khi tiếp tục các job cần dữ liệu đó. Không ghép tùy ý XML và CSV ở các thời điểm khác nhau.
- Giữ bản sao nguyên vẹn để kiểm thử và rollback. Mượt hơn sau khi thiếu CSV không chứng minh dữ liệu job còn đầy đủ.

## 7. Bước tiếp theo khi bắt đầu triển khai

1. Xác minh bản EXE/DLL, đường dẫn dữ liệu và diff hiện có.
2. Chuẩn bị bộ dữ liệu thử đầy đủ và đo baseline.
3. Thực hiện đợt 1, sau đó đợt 2; kiểm chứng từng đợt.
4. Chỉ xử lý đợt 3 khi có số đo chứng minh điểm nghẽn còn lại.
5. Bàn giao diff, kết quả đo trước/sau, kiểm thử dữ liệu và hướng dẫn triển khai/rollback.

## 8. Kiểm tra bổ sung toàn bộ thư mục dữ liệu

Đã đọc lại `D:\R-Link-16102026\R-Link` và đối chiếu đường đọc dữ liệu trong source ngày 16/10/2026.

| Thư mục | Số file | Dung lượng MiB xấp xỉ | Liên hệ với hiệu năng |
| --- | ---: | ---: | --- |
| Jobs | 59 | 61,14 | Danh sách hiện gọi deserialize đầy đủ từng job nhiều lần. |
| ProductCodesDroco | 13 | 26,94 | Nạp theo tên job và dò hộp từng dòng; nhỏ về dung lượng nhưng tốn CPU. |
| PathAllValues | 57 | 262,32 | Đọc khi mở/khôi phục job hoặc tra cứu; cần đo riêng với job lớn. |
| Database | 116 | 84,48 | Nạp dữ liệu của job đang mở; không thấy bước quét đọc toàn bộ thư mục trong đường khởi tạo đã kiểm tra. |
| CheckedResult | 57 | 79,11 | Nạp kết quả của job đang mở. |
| PrintedResponse | 3.288 | 148,95 | Đường khởi tạo trạng thái in dùng file được job tham chiếu; chưa có bằng chứng đọc cả 3.288 file khi mở danh sách. |
| Logs | 128 | 1.504,71 | Logger ghi nối tiếp và liệt kê log ngày để dọn theo hạn; chưa có bằng chứng đọc toàn bộ nội dung log khi mở danh sách. |

Phát hiện bổ sung:

1. Có cả hai kiểu lưu dữ liệu. Ví dụ CRF có 151.270 mã nằm trong XML job (~12,13 MiB), VRR có 151.220 mã trong XML; ZXM và 0YT có 151.220 dòng CSV riêng mỗi job. **API tóm tắt phải tránh deserialize các danh sách mã/hộp lồng trong XML**, không chỉ bỏ bước đọc CSV.
2. Constructor màn hình job đăng ký `VisibleChanged` để gọi `LoadJobListToMappedComboBox()` mỗi lần hiện form. Hàm này gọi `Shared.GetJob()` cho từng job trên luồng UI, kể cả trước bước lọc AllValues. Chuyển tab cũng gọi tải lại các danh sách. Bổ sung các đường gọi này vào phạm vi sửa tải lặp, không chỉ `DrawItem`.
3. Cả 59 file job có các trường `DirectoryDatabase` và `DirectoryDatabaseCodeSSCC` trỏ về `C:\ProgramData\R-Link\Database`. Chuyển thư mục gốc sang ổ D không tự đổi các đường dẫn tuyệt đối trong job. Cần ghi nhận đường dẫn đã resolve khi đo và xử lý tương thích đường dẫn khi import/restore; không tự sửa dữ liệu gốc lúc khảo sát.
4. Chưa thấy cơ chế đọc toàn bộ nội dung mọi thư mục ở đường khởi động Dorco đã kiểm tra. Không kết luận chậm chỉ từ tổng dung lượng thư mục. Các thư mục liên quan job được đọc theo những đường gọi cụ thể ở trên.

### Mục tiêu 100 job khi mở lại ứng dụng

Người dùng làm rõ mục tiêu là giữ khoảng 100 job rồi mở lại ứng dụng, không phải chạy 100 job đồng thời.

- Kiểm thử 100 job, bao gồm job có khoảng 1 triệu mã, nhưng khởi động chỉ nạp metadata nhỏ.
- Phân biệt hai chỉ số: thời gian hiển thị danh sách 100 job và thời gian mở chi tiết một job lớn.
- Nếu chưa có metadata/index, lần tạo chỉ mục ban đầu có thể cần đọc job cũ; phải chạy nền, có tiến độ và lưu kết quả để các lần mở sau không quét toàn bộ payload lần nữa.
- Có thể đạt mục tiêu mở danh sách nhanh bằng tách metadata/cache mà chưa cần chuyển toàn bộ lưu trữ sản xuất sang SQLite. Việc mở/chạy job 1 triệu mã cần benchmark riêng trước khi quyết định thay kiến trúc lưu trữ.
- Không cam kết hiệu năng cho 100 job lớn chỉ từ kiểm tra source và bộ dữ liệu hiện có; cần kết quả đo trên cấu hình máy mục tiêu.

## 9. Luồng in SSCC xuống Zebra — kiểm tra 17/10/2026

Kết quả từ source và Settings.xml của bộ dữ liệu: hộp/thùng được cấu hình in **GS1-128**, không phải nhánh Code 128 thường. Chưa kiểm tra tem in vật lý hoặc bản EXE đang triển khai.

- `frmMainDroco` nạp `DirectoryDatabaseCodeSSCC` bằng `ExcelCodeCache.LoadNoHeader`. Hộp và thùng cùng lấy cột 1 tuần tự. Khi resume, code bỏ qua số mã bằng tổng số hộp và thùng đã lưu.
- `DrocoPackagingManager.GenerateQrCode` lấy mã qua `GetNextCode`; `PrintZebraLabel` chuyển cấp đóng gói sang `Shared.PrintZebraDroco`.
- `Shared.PrintZebraDroco` ép kiểu hộp/thùng thành `GS1-128`; pallet thành `QR Code`. Tên biến `qrData`/`QrCode` không có nghĩa mọi cấp đều in QR.
- `GenerateZPLBarcode` tạo lệnh `^BCN` cho cả Code 128 và GS1-128; riêng nhánh GS1-128 thêm `>8` trước dữ liệu. Chuỗi ZPL được gửi qua `PODController.Send` khi LAN hoặc `RawPrinterHelper.SendStringToPrinter` khi USB.
- Cấu hình Zebra trong bản dữ liệu ổ D là LAN, cổng 9100, EnableBarcode=true, BarcodeType=GS1-128. File cấu hình tại C cũng có BarcodeType=GS1-128 ở mục ZebraPrinter.
- Dữ liệu SSCC mẫu có 20 chữ số bắt đầu bằng `00`; cần xác nhận đầy đủ AI, độ dài và check digit trong kiểm thử tem, chưa kết luận toàn bộ file hợp lệ từ vài dòng mẫu.

Các kiểm thử cần bảo toàn khi sửa hiệu năng:

- [ ] Mở lại job không cấp lại SSCC đã dùng; xác minh vị trí resume với dữ liệu thực tế.
- [ ] Mã gửi Zebra khớp mã lưu trong hộp/thùng và dữ liệu máy quét đọc lại.
- [ ] Phân biệt GS1-128 của hộp/thùng với QR pallet; kiểm tra cả nhánh in lại.
- [ ] Kiểm tra hết mã/mất file SSCC: hiện có nhánh fallback sinh mã tự động; cần quyết định nghiệp vụ chặn in hay cho phép, không mặc định mã fallback là SSCC hợp lệ.
- [ ] Kết quả Send thành công hiện dẫn tới log PRINTED; chưa đủ chứng minh máy đã in tem vật lý. Kiểm thử mất kết nối, hết giấy và in lại để tránh trùng/thiếu tem.

## 10. Hạng mục B — Cải thiện tem SSCC trên nhãn nhỏ

### Hiện tượng đã được người dùng xác nhận

- `00046071962202507526` in mẫu quét được; `00046071962203210838` có lúc quét được nhưng đa số không, kể cả in mẫu.
- Nhãn nhỏ: độ rộng nét 1 dot khó đọc, tăng lên 2 dot theo cách in hiện tại bị tràn tem.
- Hai chuỗi đều dài 20 chữ số, gồm AI `00` và SSCC 18 số. Đã tính lại check digit: tương ứng 6 và 8, đều đúng. Đây không phải xác nhận toàn bộ dữ liệu SSCC đều hợp lệ.
- Mã đuôi 38 có sẵn ở dòng đầu CSV SSCC của job S0B trong bộ dữ liệu đã kiểm tra; chưa có bằng chứng mã này bị tạo sai khi tạo job.
- Chưa có mẫu tem vật lý, model/DPI máy in, chiều rộng vùng in hoặc số đo chất lượng barcode. Nét mảnh, khoảng trắng, mất nét/bết mực là các giả thuyết cần thử, chưa kết luận duy nhất nguyên nhân.

### Phân tích và phương án

Cả `Shared.GenerateZPLBarcode` (in khi chạy job) và `ucZebraPrinterSetting.GenerateZPLBarcode` (in mẫu) hiện thêm `>8` cho GS1-128 nhưng không chỉ định Start C. Theo tài liệu Zebra, khi không chỉ định start code, mặc định là bộ B; `>;` chọn bộ C, `>8` là FNC1.

Nguồn đối chiếu: https://docs.zebra.com/us/en/printers/software/zpl-pg/c-zpl-zpl-commands/r-zpl-bc.html

Với dữ liệu SSCC hợp lệ là chuỗi 20 chữ số, phương án thử:

```text
Hiện tại: ^FD>800046071962203210838^FS
Đề xuất:  ^FD>;>800046071962203210838^FS
```

Giữ nguyên chuỗi SSCC, AI và check digit. Không đổi GS1-128 thành Code 128 thường. Chỉ áp dụng cách mã hóa theo cặp cho dữ liệu số có độ dài phù hợp; không áp dụng mù quáng cho dữ liệu khác hoặc mã fallback.

Ước lượng phần vạch cho 20 chữ số, một FNC1, không gồm khoảng trắng hai bên:

| Mã hóa | Độ rộng nét nhỏ nhất | Bề rộng phần vạch |
| --- | ---: | ---: |
| Bộ B | 1 dot | 266 dot |
| Bộ B | 2 dot | 532 dot |
| Bộ C | 1 dot | 156 dot |
| Bộ C | 2 dot | 312 dot |

Bộ C ở 2 dot chỉ rộng hơn khoảng 17% so với bộ B ở 1 dot. Đây là tính toán kích thước lý thuyết, không bảo đảm vừa tem hoặc đạt chất lượng quét. Phải cộng khoảng trắng, vị trí X và kiểm tra vùng in thực tế. Đổi bộ C nhưng giữ 1 dot giúp thu ngắn barcode, không tự làm nét nhỏ nhất dày hơn.

### Các bước thực hiện khi được triển khai

- [ ] Xác định model, DPI, kích thước tem, vùng in, vị trí X, tốc độ và độ đậm đang dùng.
- [ ] Ghi lại ZPL thực tế của hai mã ở cả luồng in mẫu và in job; không suy ra bản EXE hiện hành hoàn toàn khớp source.
- [ ] Dùng chung hoặc đồng bộ logic mã hóa GS1-128 giữa hai luồng; giữ nguyên hành vi QR pallet và các loại barcode khác.
- [ ] Thử bộ C ở 1 dot làm mẫu đối chứng; thử 2 dot chỉ khi tính đủ vùng in và khoảng trắng. Không tự tăng 2 dot trên cấu hình sản xuất.
- [ ] Kiểm tra vị trí HRI, nội dung chữ dưới mã, không cắt mép hoặc chồng lên barcode.
- [ ] Nếu vẫn đọc chập chờn, kiểm tra đầu in, vật tư, tốc độ, độ đậm và máy quét bằng thử nghiệm có kiểm soát; không mặc định tăng độ đậm.
- [ ] Nếu vùng in không đủ cho cấu hình đọc ổn định, ghi rõ giới hạn và đề xuất đổi bố trí/nhãn; không cam kết giải quyết chỉ bằng phần mềm.

### Tiêu chí kiểm thử

- [ ] In hai mã nêu trên và tập SSCC đại diện, nhiều tem cho mỗi cấu hình; ghi lại số lần đọc thành công/tổng lần thử với khoảng cách và góc quét xác định.
- [ ] Đối chiếu dữ liệu máy quét trả về, phân biệt không giải mã được với ứng dụng từ chối mã sau khi giải mã.
- [ ] Yêu cầu không có lần đọc thất bại trong bộ thử nghiệm đã thống nhất; không coi vài lần quét thành công là bảo đảm mọi tem sản xuất.
- [ ] Mã đọc được khớp SSCC gốc; kiểm tra GS1/FNC1 theo khả năng thiết bị và chế độ trả dữ liệu của máy quét.
- [ ] In mẫu, in job và in lại cho kết quả nhất quán; không làm thay đổi việc cấp mã hay quan hệ hộp/thùng.
- [ ] Giữ cấu hình và bản chạy cũ để rollback; chỉ áp dụng sau khi tem thực tế được kiểm chứng.

## 11. Thứ tự bàn giao tổng hợp

1. Chốt baseline hiệu năng (A) và thông số tem/máy in (B).
2. Sửa A theo đợt 1–2; thử 100 job với metadata và một job lớn khi mở chi tiết.
3. Sửa B thành thay đổi riêng, thử tem vật lý trước khi chọn độ rộng nét.
4. Kiểm thử kết hợp: mở lại job, tiếp tục cấp SSCC, in/quét xác nhận, in lại và đối chiếu dữ liệu lưu.
5. Bàn giao số đo, kết quả quét, giới hạn còn lại và hướng dẫn rollback. Chưa sửa source hoặc dữ liệu sản xuất trong giai đoạn lập kế hoạch này.
