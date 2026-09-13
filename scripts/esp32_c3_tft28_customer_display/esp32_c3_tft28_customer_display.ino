/*
 * =====================================================================
 * PHẦN MỀM BÁN HÀNG - MÀN HÌNH PHỤ KHÁCH HÀNG (CÁP USB CẮM TRỰC TIẾP)
 * KẾT NỐI: CỔNG TYPE-C USB MÁY TÍNH THU NGÂN (CẤP NGUỒN + TRUYỀN DỮ LIỆU)
 * KHÔNG CẦN WIFI - KHÔNG SỢ RỚT MẠNG - ĐỘ TRỄ 0 GIÂY - CẮM LÀ CHẠY NGAY
 * PHẦN CỨNG: TENSTAR ROBOT ESP32-C3 SUPER MINI + 2.8" TFT SPI 240x320 (DRIVER ST7789V)
 * =====================================================================
 *
 * SƠ ĐỒ ĐẤU NỐI DÂY (ESP32-C3 SUPER MINI -> MÀN HÌNH TFT 2.8" SPI):
 * ---------------------------------------------------------------------
 *  TFT 2.8" PIN  | ESP32-C3 SUPER MINI | GHI CHÚ
 *  --------------+---------------------+--------------------------------
 *  VCC           | 5V (hoặc 3.3V)      | Nguồn cấp (màn hình có IC hạ áp U2)
 *  GND           | G (GND)             | Nguồn âm / Mass
 *  CS            | GPIO 5              | TFT Chip Select
 *  RESET (RES)   | GPIO 3              | TFT Reset phần cứng
 *  DC (RS)       | GPIO 4              | Data / Command
 *  SDI (MOSI)    | GPIO 7              | Hardware SPI MOSI của ESP32-C3
 *  SCK (CLK)     | GPIO 6              | Hardware SPI Clock của ESP32-C3
 *  LED (BLK)     | 3.3V                | Đèn nền màn hình (sáng liên tục)
 *  SDO (MISO)    | Không nối (NC)      | Không bắt buộc với hiển thị QR
 * ---------------------------------------------------------------------
 */

#include <Adafruit_GFX.h>
#include <Adafruit_ST7789.h>
#include <SPI.h>
#include <qrcode.h>

// ========== CẤU HÌNH CHÂN CHO ESP32-C3 SUPER MINI -> TFT 2.8" ST7789 ==========
#define TFT_CS    5
#define TFT_DC    4
#define TFT_RST   3
#define TFT_MOSI  7
#define TFT_SCK   6

Adafruit_ST7789 tft = Adafruit_ST7789(TFT_CS, TFT_DC, TFT_RST);

// Hướng hiển thị chuẩn: Portrait (Dọc 240x320)
// Rotation 2: Dọc với hàng chân cắm ở phía trên/dưới theo thực tế
// Hỗ trợ lệnh ROTATE|0 hoặc ROTATE|2 để lật 180 độ linh hoạt
int currentRotation = 2;

// Khai báo trước các hàm (Forward Declarations)
void showScreenIdle(String store, String msg);
void showScreenQR(String qrData, String orderCode, String amount);
void showScreenSuccess(String orderCode, String amount, String bank);
void processUsbCommand(String cmdLine);
String cleanAscii(String s);
void printCenter(String text, int y, int size = 1, uint16_t color = ST77XX_WHITE, int maxWidth = 230, int startXOffset = 0, int boxWidth = 240);

// Buffer tích lũy lệnh USB từng ký tự
String _usbInputBuffer = "";

void setup() {
  Serial.begin(115200);
  delay(300); // Chờ USB CDC ổn định

  // Reset cứng màn hình trước khi khởi tạo
  pinMode(TFT_RST, OUTPUT);
  digitalWrite(TFT_RST, HIGH); delay(10);
  digitalWrite(TFT_RST, LOW);  delay(20);
  digitalWrite(TFT_RST, HIGH); delay(150);

  // Khởi tạo phần cứng SPI cho ESP32-C3 (SCK=6, MOSI=7)
  SPI.begin(TFT_SCK, -1, TFT_MOSI, TFT_CS);

  // Khởi động ST7789 chuẩn 240x320 - quét sạch toàn bộ 320 hàng, triệt tiêu 80 pixel nhiễu
  tft.init(240, 320);
  tft.setRotation(currentRotation);
  tft.fillScreen(ST77XX_BLACK);
  delay(150);

  // Hiển thị màn hình chờ ban đầu
  showScreenIdle("TAP HOA", "Xin chao quy khach!");
  Serial.println("READY|ESP32_C3_CUSTOMER_DISPLAY_USB_OK");
}

void loop() {
  while (Serial.available() > 0) {
    char c = (char)Serial.read();
    if (c == '\n') {
      _usbInputBuffer.trim();
      if (_usbInputBuffer.length() > 0) {
        processUsbCommand(_usbInputBuffer);
        _usbInputBuffer = "";
      }
    } else if (c != '\r') {
      _usbInputBuffer += c;
      if (_usbInputBuffer.length() > 512) {
        _usbInputBuffer = "";
      }
    }
  }
}

// ================= XỬ LÝ LỆNH TỪ MÁY TÍNH QUA USB =================
void processUsbCommand(String cmdLine) {
  cmdLine.trim();
  if (cmdLine.length() == 0) return;

  if (cmdLine == "PING") {
    Serial.println("PONG|ESP32_C3_TFT28|USB_ONLINE");
    return;
  }

  int firstPipe = cmdLine.indexOf('|');
  if (firstPipe == -1) return;

  String action = cmdLine.substring(0, firstPipe);
  String rest = cmdLine.substring(firstPipe + 1);

  if (action == "QR") {
    // Cú pháp: QR|HD000099|250.000 VND|chuoi_vietqr_emvco
    String orderCode = "";
    String amount = "";
    String qrData = rest;

    int secondPipe = rest.indexOf('|');
    if (secondPipe != -1) {
      orderCode = rest.substring(0, secondPipe);
      String rest2 = rest.substring(secondPipe + 1);
      int thirdPipe = rest2.indexOf('|');
      if (thirdPipe != -1) {
        amount = rest2.substring(0, thirdPipe);
        qrData = rest2.substring(thirdPipe + 1);
      } else {
        qrData = rest2;
      }
    }

    showScreenQR(qrData, orderCode, amount);
    Serial.println("ACK|QR_SHOWN");
  }
  else if (action == "SUCCESS") {
    // Cú pháp: SUCCESS|HD000099|250.000 VND|MBBank
    int secondPipe = rest.indexOf('|');
    if (secondPipe != -1) {
      String orderCode = rest.substring(0, secondPipe);
      String rest2 = rest.substring(secondPipe + 1);
      int thirdPipe = rest2.indexOf('|');
      String amount = (thirdPipe != -1) ? rest2.substring(0, thirdPipe) : rest2;
      String bank = (thirdPipe != -1) ? rest2.substring(thirdPipe + 1) : "";
      showScreenSuccess(orderCode, amount, bank);
      Serial.println("ACK|SUCCESS_SHOWN|" + orderCode);
    }
  }
  else if (action == "IDLE") {
    // Cú pháp: IDLE|TenCuaHang|LoiChao
    int secondPipe = rest.indexOf('|');
    String store = (secondPipe != -1) ? rest.substring(0, secondPipe) : "TAP HOA VIET";
    String msg = (secondPipe != -1) ? rest.substring(secondPipe + 1) : "Xin chao quy khach!";
    showScreenIdle(store, msg);
    Serial.println("ACK|IDLE_SHOWN");
  }
  else if (action == "ROTATE") {
    // Cú pháp: ROTATE|0 hoặc ROTATE|2 (đổi chiều dọc 180 độ)
    int r = rest.toInt();
    if (r >= 0 && r <= 3) {
      currentRotation = r;
      tft.setRotation(currentRotation);
      tft.fillScreen(ST77XX_BLACK);
      showScreenIdle("TAP HOA", "Xin chao quy khach!");
      Serial.println("ACK|ROTATION_SET|" + String(currentRotation));
    }
  }
}

// ===== HÀM LÀM SẠCH VĂN BẢN ASCII =====
String cleanAscii(String s) {
  String out = "";
  int len = s.length();
  for (int i = 0; i < len; i++) {
    char c = s[i];
    if (c >= 32 && c <= 126) {
      out += c;
    } else if (c == '\n' || c == '\r' || c == '\t') {
      out += ' ';
    }
  }
  return out;
}

// ===== HÀM CĂN GIỮA VĂN BẢN TRÊN MÀN HÌNH DỌC 240x320 =====
void printCenter(String text, int y, int size, uint16_t color, int maxWidth, int startXOffset, int boxWidth) {
  text = cleanAscii(text);
  text.trim();
  int charW = 6 * size;
  int maxChars = maxWidth / charW;
  if (maxChars < 1) maxChars = 1;
  if ((int)text.length() > maxChars) {
    if (size > 1) {
      size--;
      charW = 6 * size;
      maxChars = maxWidth / charW;
    }
    if ((int)text.length() > maxChars && maxChars > 2) {
      text = text.substring(0, maxChars - 2) + "..";
    }
  }
  int textWidth = text.length() * charW;
  int x = startXOffset + (boxWidth - textWidth) / 2;
  if (x < startXOffset + 2) x = startXOffset + 2;

  tft.setTextSize(size);
  tft.setTextColor(color);
  tft.setCursor(x, y);
  tft.println(text);
}

// ================= GIAO DIỆN 1: MÀN HÌNH CHỜ (IDLE 240x320 DỌC) =================
void showScreenIdle(String store, String msg) {
  tft.fillScreen(ST77XX_BLACK);

  // Khung chính căn giữa màn hình dọc 240x320
  tft.fillRoundRect(12, 40, 216, 240, 10, 0x10A2);
  tft.drawRoundRect(12, 40, 216, 240, 10, 0x21E8);

  // Tên cửa hàng
  printCenter(store, 60, 2, ST77XX_CYAN, 200, 12, 216);

  // Vạch kẻ phân cách
  tft.drawFastHLine(30, 95, 180, 0x39E7);

  // Lời chào XIN CHAO to ở giữa
  printCenter("XIN CHAO!", 120, 3, ST77XX_YELLOW, 200, 12, 216);
  printCenter(msg, 165, 2, ST77XX_WHITE, 200, 12, 216);

  // Chữ thông báo sẵn sàng quét
  printCenter("San sang thanh toan", 215, 1, 0xCE79, 200, 12, 216);
  printCenter("VietQR", 235, 2, ST77XX_GREEN, 200, 12, 216);
}

// ================= GIAO DIỆN 2: MÃ VIETQR KÈM SỐ TIỀN (240x320 DỌC) =================
int getMinQrVersion(int len) {
  if (len <= 17) return 1;
  if (len <= 32) return 2;
  if (len <= 53) return 3;
  if (len <= 78) return 4;
  if (len <= 106) return 5;
  if (len <= 134) return 6;
  if (len <= 154) return 7;
  if (len <= 192) return 8;
  if (len <= 230) return 9;
  if (len <= 271) return 10;
  return -1;
}

#define QR_BUF_SIZE 1024
static uint8_t qrcodeData[QR_BUF_SIZE];
static QRCode qrcode;

void showScreenQR(String qrData, String orderCode, String amount) {
  qrData.trim();
  if (qrData.length() == 0) return;

  int minVer = getMinQrVersion(qrData.length());
  if (minVer < 0 || minVer > 10) {
    tft.fillScreen(ST77XX_BLACK);
    printCenter("LOI: QR DATA QUA DAI!", 130, 2, ST77XX_RED, 230, 0, 240);
    printCenter("Do dai: " + String(qrData.length()), 160, 1, ST77XX_WHITE, 230, 0, 240);
    Serial.println("ERR|QR_DATA_TOO_LONG|" + String(qrData.length()));
    return;
  }

  int8_t err = -1;
  int version = minVer;
  while (err != 0 && version <= 10) {
    uint16_t needed = qrcode_getBufferSize(version);
    if (needed > QR_BUF_SIZE) {
      Serial.println("ERR|BUF_TOO_SMALL|needed=" + String(needed));
      break;
    }
    err = qrcode_initText(&qrcode, qrcodeData, version, ECC_LOW, qrData.c_str());
    if (err != 0) version++;
  }

  if (err != 0) {
    tft.fillScreen(ST77XX_BLACK);
    printCenter("LOI SINH MA QR!", 130, 2, ST77XX_RED, 230, 0, 240);
    printCenter("Kiem tra lai thong tin thanh toan", 160, 1, ST77XX_WHITE, 230, 0, 240);
    Serial.println("ERR|QR_FAIL|len=" + String(qrData.length()));
    return;
  }

  Serial.println("DBG|QR_OK|ver=" + String(version) + "|size=" + String(qrcode.size));

  // Quét phủ đen toàn bộ màn hình dọc 240x320 sạch sẽ, triệt tiêu 80 pixel nhiễu
  tft.fillScreen(ST77XX_BLACK);

  // 1. THANH TIÊU ĐỀ PHÍA TRÊN: Mã đơn hàng (y: 0..38)
  tft.fillRect(0, 0, 240, 38, 0x0277);
  tft.drawFastHLine(0, 38, 240, 0x041F);
  String headerText = "QUET VIETQR";
  if (orderCode.length() > 0) {
    headerText = "DON: " + orderCode;
  }
  printCenter(headerText, 11, 2, ST77XX_WHITE, 230, 0, 240);

  // 2. VẼ MÃ QR TO Ở TRUNG TÂM (y: 42..242)
  int qrSize = qrcode.size;
  int scale = 196 / qrSize;
  if (scale < 1) scale = 1;
  if (scale > 4) scale = 4;
  int qrPixelSize = qrSize * scale;
  int startX = (240 - qrPixelSize) / 2;
  int startY = 44 + (196 - qrPixelSize) / 2;

  // Viền trắng tương phản cao để quét siêu nhạy
  int pad = 5;
  tft.fillRoundRect(startX - pad, startY - pad, qrPixelSize + pad * 2, qrPixelSize + pad * 2, 4, ST77XX_WHITE);

  // Vẽ từng module QR
  for (uint8_t y = 0; y < qrcode.size; y++) {
    for (uint8_t x = 0; x < qrcode.size; x++) {
      if (qrcode_getModule(&qrcode, x, y)) {
        tft.fillRect(startX + (x * scale),
                     startY + (y * scale),
                     scale, scale, ST77XX_BLACK);
      }
    }
  }

  // 3. KHUNG SỐ TIỀN THANH TOÁN PHÍA DƯỚI (y: 248..320)
  tft.fillRect(0, 248, 240, 72, 0x0A20);
  tft.drawFastHLine(0, 248, 240, 0x21E8);

  if (amount.length() > 0 && amount != "0 VND") {
    printCenter("SO TIEN THANH TOAN:", 254, 1, 0xCE79, 230, 0, 240);
    printCenter(amount, 269, 2, ST77XX_YELLOW, 230, 0, 240);
  } else {
    printCenter("QUET MA DE THANH TOAN", 264, 2, ST77XX_YELLOW, 230, 0, 240);
  }
  printCenter("App Ngan hang / Vi dien tu", 298, 1, ST77XX_WHITE, 230, 0, 240);

  Serial.println("ACK|QR_DRAWN");
}

// ================= GIAO DIỆN 3: BÁO THÀNH CÔNG (SUCCESS 240x320 DỌC) =================
void showScreenSuccess(String orderCode, String amount, String bank) {
  tft.fillScreen(0x04A0); // Nền xanh lá tươi dịu mắt

  // Thanh tiêu đề thông báo
  tft.fillRect(0, 0, 240, 44, 0x0360);
  tft.drawFastHLine(0, 44, 240, 0x05E5);
  printCenter("DA NHAN TIEN!", 12, 2, ST77XX_YELLOW, 230, 0, 240);

  // Biểu tượng tích [V]
  tft.fillCircle(120, 80, 24, ST77XX_WHITE);
  tft.drawCircle(120, 80, 24, ST77XX_YELLOW);
  tft.setTextSize(3);
  tft.setTextColor(0x04A0);
  tft.setCursor(111, 70);
  tft.println("V");

  // Hộp chi tiết hoá đơn
  tft.fillRoundRect(14, 115, 212, 115, 8, ST77XX_BLACK);
  tft.drawRoundRect(14, 115, 212, 115, 8, ST77XX_WHITE);

  if (orderCode.length() > 0) {
    printCenter("Ma don: " + orderCode, 128, 2, ST77XX_CYAN, 200, 14, 212);
  }
  printCenter(amount, 158, 3, ST77XX_YELLOW, 200, 14, 212);
  if (bank.length() > 0) {
    printCenter("Nhan qua: " + bank, 198, 1, 0xCE79, 200, 14, 212);
  }

  printCenter("Cam on quy khach da mua hang!", 252, 1, ST77XX_WHITE, 230, 0, 240);
  printCenter("HEN GAP LAI QUY KHACH!", 278, 2, ST77XX_GREEN, 230, 0, 240);
}
