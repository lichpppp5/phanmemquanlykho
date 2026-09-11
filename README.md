# PM Tạp Hóa - Hệ Thống Quản Lý Kho & Bán Hàng POS Đa Nền Tảng

Ứng dụng quản lý kho, bán hàng (POS), in ấn hoá đơn chuyên nghiệp cho cửa hàng tạp hóa, siêu thị mini và đại lý bán buôn. Hỗ trợ cả ứng dụng **Desktop WinForms** và **Web App Local** chạy mượt mà trên macOS, Windows, Linux.

## 🚀 Cài Đặt & Chạy Nhanh Cho Máy Tính Mới

- Xem toàn bộ danh sách module & driver yêu cầu tại: 👉 **[REQUIREMENTS.md](file:///d:/code%20tap%20hoa/phanmemquanlykho/REQUIREMENTS.md)**
- **Cài đặt tự động toàn bộ module (1 Click):** Chạy file `scripts\install-dependencies.bat`
- **Khởi động bán hàng:** Chạy file `scripts\run-web.bat` (tự động mở trình duyệt tại `http://taphoa.local:8888`)
- **Tạo tên miền nội bộ (1 lần duy nhất):** Chạy file `scripts\setup-local-domain.bat`
- **Hoặc dùng lệnh:**
```bash
dotnet run --project PMTapHoa.Web/PMTapHoa.Web.csproj
```


---

## 🖨️ Tính Năng In Ấn & Thiết Bị Phần Cứng Chuyên Nghiệp

### 1. Cấu Hình Máy In Hoá Đơn Nhiệt (K80 / K58)
- **Preset cho các dòng máy in nhiệt phổ biến nhất Việt Nam**:
  - Xprinter XP-N160M / Q200 / C300H (Khổ 80mm)
  - Epson TM-T82 / TM-T88 (K80)
  - Bixolon SRP-330 / SRP-350 (K80)
  - POS-58 Series / Xprinter XP-58 (Khổ 58mm mini)
  - Sunmi V2 / V2 Pro / Máy POS Android cầm tay (K58)
- **Kiểu kết nối**: Cổng USB, Cắm dây mạng LAN / Wi-Fi, Không dây Bluetooth.
- **Tuỳ chọn số liên in**:
  - 1 liên: Giao cho khách hàng.
  - 2 liên: Tự động in Liên 1 (Khách hàng) + Đường cắt giấy + Liên 2 (Lưu quầy / Kế toán).
- **Cỡ chữ hoá đơn**: Tiêu chuẩn / To rõ nét cho người già / Nhỏ gọn tiết kiệm giấy.
- **Tích hợp mã VietQR ngân hàng**: Tự động sinh mã VietQR động ở chân hoá đơn để khách quét thanh toán ngay.
- **Nút "In Thử Bill Nhiệt"**: Thử nghiệm và căn lề máy in nhiệt ngay tại màn hình Cài Đặt.

### 2. Cấu Hình Tay Bấm QR & Máy Quét Mã Vạch
- **Hỗ trợ đầy đủ các dòng thiết bị**:
  - Tay bấm cầm tay có dây USB (Honeywell, Zebra, Sunlux, Netum...).
  - Tay bấm không dây Wireless 2.4GHz / Bluetooth (Deli, Syble, Netum...).
  - Máy quét để bàn đa tia siêu thị (Honeywell Orbit, Datalogic...).
  - Quét qua Camera / Webcam máy tính.
- **Ký tự hậu tố (Suffix)**: Phím `Enter`, phím `Tab`, hoặc khoảng lặng `None`.
- **Tự động thêm vào giỏ**: Tự động nhận diện mã vạch và thêm ngay vào đơn hàng khi quét.
- **Âm thanh bíp xác nhận**: Phát âm thanh "Bíp" chuẩn máy POS từ loa máy tính khi quét thành công.
- **🎯 Hộp quét thử nghiệm tay bấm tương tác**:
  - Đèn LED trạng thái thời gian thực (Xanh lá: Sẵn sàng, Xanh ngọc: Đang nhận tín hiệu, Xanh ngọc lục bảo: Thành công).
  - Đo lường độ trễ truyền dữ liệu (phân biệt tay bấm phần cứng siêu tốc <200ms và gõ phím thông thường).
  - Tự động tra cứu tức thì mã vừa quét với kho hàng hiện tại.

### 3. In Hoá Đơn Tổng Khổ A4 / A5 (Bán Buôn & Đại Lý)
- **Dành cho máy in Laser/In phun văn phòng** (Canon LBP 2900, HP LaserJet, Brother, Epson...).
- **Tiêu đề tuỳ biến**: `HÓA ĐƠN BÁN HÀNG`, `PHIẾU XUẤT KHO KIÊM BÁN HÀNG`, v.v.
- **Mã số thuế cửa hàng (MST)**: In chuẩn mực trên đầu hoá đơn.
- **Khổ giấy & Hướng in**: Khổ A4 hoặc A5; Dọc (Portrait) hoặc Ngang (Landscape).
- **Đọc số tiền thành chữ tiếng Việt tự động**: Tự động chuyển đổi số tiền thành chữ chuẩn ngữ pháp tiếng Việt (VD: *"Một trăm năm mươi nghìn đồng chẵn."*).
- **Góc mã VietQR chuyển khoản**: Tự động kèm mã VietQR ngân hàng của chủ quầy.
- **Bảng 3 chữ ký chứng từ**: Người mua hàng - Người giao hàng - Người lập phiếu.
- **In linh hoạt**: Có thể in từ màn hình Thanh toán (POS), Lịch sử hoá đơn, hoặc xem trước trong mục Cài Đặt.

### 4. Lưu Trữ Hoá Đơn Máy Tính Theo Ngày (`HoaDon/Ban` & `HoaDon/Nhap`)
- **Bắt buộc cấu hình trước khi bán hàng**: Cửa hàng phải cấu hình thư mục lưu trữ (mặc định `~/HoaDon`).
- **Phân loại tự động**:
  - `HoaDon/Ban/yyyy-MM-dd/`: Lưu từng hoá đơn bán lẻ dạng file `.html`, `.txt`, và biểu mẫu `.a4.html`.
  - `HoaDon/Nhap/yyyy-MM-dd/`: Lưu từng phiếu nhập kho.
- **Nút mở nhanh thư mục**: Truy cập trực tiếp thư mục `Ban` hoặc `Nhap` của ngày hôm nay bằng 1 cú click.

---

## 👥 Quản Lý Người Dùng & Phân Quyền (RBAC)

- **Admin (Quản trị viên)**: Toàn quyền cấu hình máy in, sửa xoá sản phẩm, xem báo cáo doanh thu & lợi nhuận, quản lý tài khoản nhân viên.
- **Staff (Nhân viên thu ngân)**: Bán hàng tại quầy POS, quét mã vạch, xem tồn kho. Tự động ẩn các mục nhạy cảm (Cài đặt, Báo cáo tài chính, Quản lý user).
- **Chuyển đổi tài khoản nhanh**: Có nút chuyển đổi nhanh trực tiếp giữa Admin và Thu ngân tại thanh điều hướng.

---

## 📦 Công Nghệ Sử Dụng

- Backend: ASP.NET Core (.NET 10), SQLite nhúng file nhẹ và an toàn, Dapper ORM.
- Frontend: HTML5, CSS3 hiện đại, Vanilla JavaScript không phụ thuộc thư viện nặng.
- Thiết kế: Giao diện chuẩn UI/UX cao cấp, hỗ trợ Chế độ Sáng / Tối (Light / Dark mode), Responsive đa màn hình.
