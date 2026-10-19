Quy trình dữ liệu của camera Keyence CV-X470
Giao thức: TCP Socket (Port 8500) — Framed Mode (CR)


Dữ liệu thô từ camera
http://ndatrace.vn/02/8935217402737/02/hp9vm3mzoio,1,NSX:17O826,15:38,HSD:13O427,A99,0,0000000275
tương tự vs-c
8 fields sau khi split 
Index			Field				Ví dụ							Ý nghĩa
0				QR					https://ndatrace.vn/...			Nội dung QR code
1				QR_Result			true							Kết quả đọc QR: true/false
2				NSX					NSX:28/05/2026					Ngày sản xuất (từ camera, có prefix NSX:)
3				Time				11:48							Thời gian sản xuất
4				HSD					HSD:28/06/2027					Hạn sử dụng (từ camera, có prefix HSD:)
5				Batch				A16								Lô 
6				OCR_Result			true							Kết quả OCR: true/false
7				Image_ID			0000000001						ID ảnh để download từ FTP


1. Mô tả:
CVX và VS_C sẽ thay thế nhau dùng rrieeng lẻ hoặc dùng chung với IV4
- VS-C
- CV-X
- VS-C + IV4
- CV-X + IV4



2. So sánh dữ liệu giữa QR code và OCR PHÂN LOẠI LỖI


3. Cách xử lý lưu ảnh lỗi:
- Chỉ gửi ảnh về khi lỗi: gửi qua FTP
- Lấy tương tự vs-c



5. Lệnh đổi chương trình
- PR<CR> : Đọc chương trình hiện tại --> Response: PR,1,000<CR>
- 
- PNR<CR>: đọc tên chương trình hiện tại --> Response: PNR,ProgramName<CR>
- PW,001<CR>: Đổi chương trình --> Response: PW<CR> (nếu thành công)
6. Một số lệnh cần thiết:
- Lệnh kiểm tra xem camera đang ở chế độ nào: RM<CR> --> Response: RM,1<CR> -> fđang ở chế độ RUN có thể gửi lệnh trigger hoặc sersor trigger
- Lấy kết quả thủ công:	RT<CR>
- Bật tự động gửi kết quả: OE,1<CR>
- Tắt tự động gửi kết quả: <CR>


