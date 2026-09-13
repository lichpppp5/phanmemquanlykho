# HƯỚNG DẪN ĐẤU NỐI & NẠP CODE MÀN HÌNH PHỤ QR 2.8"
## MẠCH ESP32-C3 SUPER MINI + MÀN HÌNH MÀU 2.8'' TFT 240x320 (ILI9341 SPI)

Tài liệu này hướng dẫn chi tiết cách đấu nối dây và nạp phần mềm cho bộ hiển thị QR thanh toán đặt tại quầy thu ngân.

---

### I. PHÂN TÍCH PHẦN CỨNG THỰC TẾ

1. **Board mạch:** **TENSTAR ROBOT ESP32-C3 Super Mini**
   - Vi điều khiển: ESP32-C3 (32-bit RISC-V 160MHz).
   - Cổng giao tiếp: Type-C tích hợp USB Serial/JTAG (USB CDC) phần cứng.
   - Sơ đồ chân hai bên mạch (nhìn từ mặt sau):
     - **Hàng bên trái:** `5V` | `G (GND)` | `3.3` | `4` | `3` | `2` | `1` | `0`
     - **Hàng bên phải:** `5` | `6` | `7` | `8` | `9` | `10` | `20` | `21`

2. **Màn hình:** **2.8'' TFT 240xRGBx320 V1.1 (Mạch đỏ, SPI)**
   - Độ phân giải: 240 x 320 pixel, 65.000 màu sắc nét.
   - IC Điều khiển (Driver): **ST7789V** (hoặc **ILI9341** tùy lô sản xuất; bản mạch đỏ 2.8'' TFT 240xRGBx320 V1.1 phổ biến chạy ST7789V hiển thị chuẩn 240x320, không bị sọc nhiễu).
   - Hàng chân SPI (9 chân): `VCC`, `GND`, `CS`, `RESET`, `DC`, `SDI(MOSI)`, `SCK`, `LED`, `SDO(MISO)`.

---

### II. BẢNG SƠ ĐỒ ĐẤU NỐI DÂY CHUẨN

> [!IMPORTANT]
> Các chân **GPIO 3, 4, 5, 6, 7** được chọn chuẩn xác để tránh các chân Bootloader strapping (GPIO 8, 9) của chip ESP32-C3. Mạch sẽ khởi động 100% tin cậy, không bao giờ bị kẹt bootloader hoặc chập chờn cổng USB.

| STT | Chân Màn Hình 2.8" TFT | Chân ESP32-C3 Super Mini | Chức Năng | Màu Dây Khuyên Dùng |
| :---: | :--- | :--- | :--- | :--- |
| **1** | **VCC** | **5V** (hoặc 3.3V) | Nguồn dương cấp màn hình | Dây Đỏ |
| **2** | **GND** | **G** (GND) | Nguồn âm / Mass | Dây Đen |
| **3** | **CS** | **GPIO 5** | Chip Select màn hình | Dây Cam |
| **4** | **RESET (RES)** | **GPIO 3** | Reset phần cứng màn hình | Dây Vàng |
| **5** | **DC (RS)** | **GPIO 4** | Data / Command Select | Dây Xanh lá |
| **6** | **SDI (MOSI)** | **GPIO 7** | Hardware SPI MOSI | Dây Xanh dương |
| **7** | **SCK (CLK)** | **GPIO 6** | Hardware SPI Clock | Dây Tím |
| **8** | **LED (BLK)** | **3.3V** | Đèn nền màn hình (Backlight) | Dây Nâu |
| **9** | **SDO (MISO)** | *Không nối (NC)* | Không cần nối cho hiển thị QR | Để trống |

*(Cụm chân bên phải TOUCH và khe cắm thẻ nhớ SD trên màn hình để trống, không cần nối).*

---

### III. HƯỚNG DẪN CÀI ĐẶT TRÊN ARDUINO IDE

#### Bước 1: Cài đặt 3 thư viện cần thiết
1. Mở **Arduino IDE** trên máy tính.
2. Vào menu **Sketch** -> **Include Library** -> **Manage Libraries...** (phím tắt `Ctrl + Shift + I`).
3. Gõ tên và bấm **Install** lần lượt 3 thư viện sau:
   - **`Adafruit GFX Library`** (bởi Adafruit)
   - **`Adafruit ST7735 and ST7789 Library`** (bởi Adafruit - hỗ trợ ST7789V)
   - **`QRCode`** (bởi Richard Moore - ricmoo)

#### Bước 2: Thiết lập thông số Board ESP32-C3
Cắm cáp Type-C từ máy tính vào bo mạch ESP32-C3 Super Mini, sau đó vào menu **Tools** thiết lập:
- **Board:** Chọn `ESP32C3 Dev Module` (hoặc `AirM2M_CORE_ESP32C3`).
- **Port:** Chọn cổng COM vừa nhận (VD: COM3, COM4...).
- **USB CDC On Boot:** **`Enabled`**  *(⚠️ BẮT BUỘC: Phải chọn Enabled thì Serial mới truyền dữ liệu qua cổng Type-C!)*.
- **Flash Size:** `4MB (32Mb)`.
- **Upload Speed:** `921600` (hoặc `460800`).

#### Bước 3: Mở file code và nạp
1. Mở file mã nguồn: `scripts\esp32_c3_tft28_customer_display.ino`.
2. Bấm nút **Upload** (mũi tên trỏ sang phải `->`) trên Arduino IDE.
3. Chờ màn hình nạp 100%. Khi nạp xong, màn hình 2.8" TFT sẽ sáng lên với giao diện chào mừng màu xanh navy **"XIN CHÀO!"** và báo **"KET NOI: USB SERIAL 115200 BAUD"**.

---

### IV. KẾT NỐI VỚI PHẦN MỀM THU NGÂN TRÊN MÁY TÍNH

1. **Cắm cố định cáp Type-C:** Cắm 1 sợi cáp Type-C từ máy tính thu ngân sang mạch ESP32-C3 Super Mini. Cáp này vừa cấp nguồn cho màn hình sáng liên tục, vừa truyền tín hiệu thanh toán siêu tốc (0ms).
2. **Mở phần mềm:** Dùng trình duyệt **Google Chrome**, **Microsoft Edge** hoặc **Cốc Cốc**.
3. **Kết nối một lần duy nhất:**
   - Vào menu **Cài Đặt** -> tìm mục **📺 Màn Hình Phụ Khách Hàng (ESP32-C3 Super Mini + TFT 2.8" SPI 240x320)**.
   - Bấm nút **🔌 Kết Nối Cổng USB (Cổng COM)**.
   - Trình duyệt sẽ hiện danh sách thiết bị, chọn cổng COM của ESP32-C3 và bấm **Connect**.
   - Biểu tượng trạng thái sẽ chuyển sang: `🟢 Đang chạy qua cáp USB Serial`. Trình duyệt sẽ tự động ghi nhớ cho những lần mở sau.

---

### V. TRẢI NGHIỆM THỰC TẾ KHI BÁN HÀNG

- **Khi không có đơn (Màn hình chờ):**
  Màn hình hiển thị tên cửa hàng của bạn ở thanh trên cùng, icon chào đón thân thiện và lời cảm ơn quý khách.
- **Khi thu ngân bấm Thanh Toán (F4):**
  Màn hình lập tức đổi sang giao diện thanh toán:
  - Header: Mã đơn hàng (VD: `DON: HD000123`).
  - Trung tâm: **Mã VietQR chuẩn EMVCo phóng to 190x190 pixel**, cực kỳ sắc nét trên nền trắng. Khách hàng đứng xa hơn 1 mét đưa điện thoại lên là camera bắt nét và chuyển khoản tức thì!
  - Footer: **SỐ TIỀN CẦN THANH TOÁN** hiển thị màu vàng to rõ (VD: `250.000 VND`), khách nhìn là biết ngay số tiền cần trả.
- **Khi khách chuyển khoản thành công:**
  Màn hình chuyển ngay sang nền xanh lục báo **"ĐÃ NHẬN TIỀN!"** kèm thông tin hoá đơn và số tiền, giúp thu ngân và khách hàng đều yên tâm tuyệt đối.
