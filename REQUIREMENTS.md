# HƯỚNG DẪN CÀI ĐẶT & YÊU CẦU HỆ THỐNG (REQUIREMENTS)
## PHẦN MỀM QUẢN LÝ BÁN HÀNG & KHO TẠP HOÁ (PM TAP HOA)
> Dành cho triển khai thương mại trên máy tính khách hàng mới (Windows 10, 11, macOS, Linux)

---

## 1. YÊU CẦU HỆ THỐNG (SYSTEM REQUIREMENTS)

### Phần cứng tối thiểu (Hardware Requirements):
- **Hệ điều hành:** Windows 10 (64-bit) / Windows 11 / macOS 11+ / Ubuntu 20.04+
- **CPU:** Intel Core i3 / AMD Ryzen 3 hoặc tương đương (tối thiểu 2 nhân, khuyến nghị 4 nhân).
- **RAM:** Tối thiểu 4 GB (Khuyến nghị 8 GB để chạy đa nhiệm mượt mà).
- **Ổ cứng:** Còn trống tối thiểu 500 MB (SSD để tốc độ đọc ghi hoá đơn và cơ sở dữ liệu nhanh nhất).
- **Cổng kết nối:**
  - Tối thiểu 1 cổng USB cho Máy quét mã vạch / Máy in bill.
  - Thêm 1 cổng USB nếu dùng Màn hình phụ hiển thị QR khách hàng (ESP32 TFT).

### Môi trường thực thi (Runtime Requirements):
- **.NET Runtime:** .NET 10.0 ASP.NET Core Runtime (hoặc .NET 8.0 nếu chạy bản Desktop WinForms).
  - *Lưu ý:* Nếu đóng gói dạng **Self-Contained (Đóng gói trọn gói)** thì máy khách hàng **KHÔNG CẦN CÀI .NET**, cắm là chạy ngay!
- **Trình duyệt web:** Google Chrome (v89+), Microsoft Edge (v89+), hoặc Cốc Cốc (hỗ trợ Web Serial API để kết nối màn hình phụ USB).

---

## 2. DANH SÁCH DRIVER & PHẦN CỨNG BẮT BUỘC (DRIVERS & HARDWARE)

Khi triển khai trên máy tính mới của khách hàng, cần đảm bảo cài các Driver tương ứng:

### 1. Driver Màn hình hiển thị QR rời (ESP32):
Hầu hết các mạch vi điều khiển ESP32 giao tiếp qua chip USB-to-UART. Cần cài 1 trong 2 driver sau tùy theo mạch:
1. **Chip CH340 / CH341 (Phổ biến 90% mạch tại Việt Nam):**
   - Tải về: [CH341SER.EXE (Trang chủ WCH)](https://www.wch.cn/downloads/CH341SER_EXE.html)
   - Hoặc chạy file `scripts/install-ch340.bat` (nếu có).
2. **Chip CP2102 / CP210x (Mạch chính hãng / NodeMCU):**
   - Tải về: [CP210x Windows VCP Driver (Silicon Labs)](https://www.silabs.com/developers/usb-to-uart-bridge-vcp-drivers)

### 2. Driver Máy in bill nhiệt (K80 / K58):
- **Xprinter (XP-N160M, XP-Q200, XP-C300H, XP-58):** Cài bộ `XPrinter Driver Setup` cho khổ giấy 80mm hoặc 58mm.
- **Epson / Bixolon / POS-58:** Cài driver máy in theo hãng hoặc chọn chuẩn máy in hóa đơn mặc định của Windows (`Generic / Text Only`).

### 3. Máy quét mã vạch (Barcode Scanner / QR Scanner):
- Hoạt động theo cơ chế **HID Keyboard (Bàn phím ảo)**: Cắm cổng USB là Windows tự nhận ngay trong 3 giây, **không cần cài driver rời**.
- Khuyến nghị cấu hình máy quét có hậu tố phím `Enter` (CR/LF).

---

## 3. DANH SÁCH THƯ VIỆN & MODULE CODE (PACKAGES / DEPENDENCIES)

Dự án được xây dựng theo kiến trúc Clean Architecture nhẹ nhàng, tốc độ cao, không phụ thuộc cồng kềnh:

| Dự án | Package / Thư viện | Phiên bản | Mục đích sử dụng |
|---|---|---|---|
| **PMTapHoa.Core** | `Microsoft.Data.Sqlite` | Mới nhất | Hệ cơ sở dữ liệu SQLite cục bộ, an toàn, tự động tạo file database |
| **PMTapHoa.Core** | `Dapper` | Mới nhất | Micro-ORM truy vấn dữ liệu hiệu năng cao (< 2ms) |
| **PMTapHoa.Desktop**| `ClosedXML` | Mới nhất | Xuất nhập báo cáo, danh mục hàng hóa ra file Excel (.xlsx) |
| **PMTapHoa.Desktop**| `QRCoder` | Mới nhất | Sinh mã VietQR tĩnh/động cho máy in |
| **ESP32 Firmware** | `Adafruit GFX Library` | 1.11+ | Thư viện đồ hoạ hiển thị LCD/TFT (Adafruit) |
| **ESP32 Firmware** | `Adafruit ST7735 and ST7789` | 1.10+ | Driver điều khiển màn hình TFT SPI 1.8 inch (Adafruit) |
| **ESP32 Firmware** | `QRCode` | 0.0.1+ | Thư viện sinh mã QR nhẹ (bởi Richard Moore / ricmoo) |

---

## 4. QUY TRÌNH CÀI ĐẶT NHANH CHO MÁY MỚI (DEPLOYMENT GUIDE)

### Cách 1: Đóng gói thành file cài đặt chạy 1 Click (Khuyên dùng cho thương mại)
Trên máy phát triển, chạy file script:
```bat
scripts\build-installer.bat
```
- Script sẽ tự động gom toàn bộ .NET Runtime + Web + SQLite thành 1 file duy nhất (`.exe` hoặc `.zip` Portable).
- Sang máy khách: Chỉ việc giải nén hoặc chạy file cài đặt, bấm **Next -> Finish** là phần mềm sẵn sàng bán hàng ngay!

### Cách 2: Chạy trực tiếp từ mã nguồn (.NET SDK)
1. Cài đặt .NET SDK (chạy script tự động):
   ```powershell
   .\dotnet-install.ps1 -Channel 10.0
   ```
2. Chạy ứng dụng:
   ```bat
   scripts\run-web.bat
   ```
3. Cấu hình tên miền nội bộ (Chỉ cần chạy 1 lần):
   - Nhấp đúp chạy: `scripts\setup-local-domain.bat` (tự động gắn `127.0.0.1 taphoa.local` vào file hosts)
4. Mở trình duyệt Chrome/Edge và truy cập: 👉 **`http://taphoa.local:8888`**
   *(Hoặc dự phòng: `http://localhost:8888`)*
