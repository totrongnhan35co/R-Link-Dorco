Quy trình dữ liệu của camera Keyence IV4_G500CA và IV4-G600CA
Giao thức: TCP Socket (Port 8500) — Framed Mode (CR)

Dữ liệu thô từ camera
Nội dung: RT,00075,OK,01,OK,0000085<CR>
Mô tả cấu trúc gói tin: RT,00075,OK,01,OK,0000085<CR>
Trong đó:

Thành phần			Ví dụ			Ý nghĩa
RT					RT				Header báo đây là gói kết quả
aaaaa				00075			Result number, số thứ tự kết quả
bb					OK				Kết quả tổng của lần kiểm tra
cc					01				Số tool kiểm tra
dd					OK				Kết quả riêng của tool đó
eeeeeee				0000085			Giá trị chi tiết của tool


1. Việc cần làm:
- Khi có trigger từ sensor thì nó sẽ nhận gói tin
- IV G600CA + G500 CA kết hợp với gói tin của VS-C hoặc CV-X để hiển thị giá trị (bb của gói tin IV G600CA + G500 CA) của cột ngoại quan trong bảng dgvCheckedResult


- lưu chuỗi này bảng vào tb_CameraLogs
- Hiển thị dữ liệu: gói tin vào cột gói tin có sẵn cách nhau bởi ;
- Hiển thị dữ liệu: Đường dẫn ảnh lỗi ngoại quan nếu có vào cột đường dẫn ảnh có sẵn cách nhau bởi ;

2. So sánh dữ liệu giữa QR code và OCR PHÂN LOẠI LỖI

qr_quantity	(auto true)			ocr_quantity		ngoại quan		So sánh dữ liệu					 Kết quả 								PLC
					
TRUE							FALSE				OK				Không so sánh						F									Camera
TRUE							true				NG				Không so sánh						F									Camera
true							TRUE				OK				Không so sánh						A									"Nếu lỗi liên tục n sản phẩm - thiết đặt trên Rlink Master Rlink (dừng dàn máy)"

3. Đếm lỗi liên vẫn giữ nguyển thay đổi phần 2 đã bao gồm phần 3

4. Cách xử lý lưu ảnh lỗi:
- Chỉ gửi ảnh về khi lỗi: gửi qua FTP
- Cách lấy sẽ dựa vào thứ tự gói tin và thứ tự hình ảnh được kh



5. Lệnh đổi chương trình
- PR<CR> : Đọc chương trình hiện tại --> Response: PR,000<CR>
- PNR<CR>: đọc tên chương trình hiện tại --> Response: PNR,ProgramName<CR>
- PW,001<CR>: Đổi chương trình --> Response: PW<CR> (nếu thành công)
6. Một số lệnh cần thiết:
- Lệnh kiểm tra xem camera đang ở chế độ nào: RM<CR> --> Response: RM,1<CR> -> fđang ở chế độ RUN có thể gửi lệnh trigger hoặc sersor trigger
- Lấy kết quả thủ công:	RT<CR>
- Bật tự động gửi kết quả: OE,1<CR>
- Tắt tự động gửi kết quả: <CR>


