using System.Text;
using System.Text.Json.Serialization;

namespace PMTapHoa.Core.Services;

public enum Esp32DisplayState
{
    Idle = 0,
    WaitingPayment = 1,
    PaymentSuccess = 2
}

public class Esp32ScreenInfo
{
    [JsonPropertyName("state")]
    public string State { get; set; } = "IDLE"; // IDLE, QR, SUCCESS

    [JsonPropertyName("storeName")]
    public string StoreName { get; set; } = "TẠP HOÁ VIỆT";

    [JsonPropertyName("orderCode")]
    public string OrderCode { get; set; } = "";

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; } = 0;

    [JsonPropertyName("amountFormatted")]
    public string AmountFormatted { get; set; } = "0 đ";

    [JsonPropertyName("customerName")]
    public string CustomerName { get; set; } = "";

    [JsonPropertyName("qrContent")]
    public string QrContent { get; set; } = "";

    [JsonPropertyName("bankName")]
    public string BankName { get; set; } = "";

    [JsonPropertyName("message")]
    public string Message { get; set; } = "Xin chào quý khách!";

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
}

public class Esp32DeviceStatus
{
    public bool IsOnline => (DateTime.UtcNow - LastPingUtc).TotalSeconds < 15;
    public string? IpAddress { get; set; }
    public int? Rssi { get; set; } // WiFi signal strength in dBm
    public DateTime LastPingUtc { get; set; } = DateTime.MinValue;
    public string FirmwareVersion { get; set; } = "1.0.0";
    public string ScreenResolution { get; set; } = "240x320 ILI9341 2.8\"";
}

public class Esp32CustomerDisplayService
{
    private readonly AppConfigService _config;
    private Esp32DisplayState _currentState = Esp32DisplayState.Idle;
    private readonly object _lock = new();

    private string _orderCode = "";
    private decimal _amount = 0;
    private string _customerName = "";
    private string _qrContent = "";
    private string _paidBank = "";
    private DateTime _stateChangedAt = DateTime.UtcNow;

    public Esp32DeviceStatus DeviceStatus { get; } = new();

    public Esp32CustomerDisplayService(AppConfigService config)
    {
        _config = config;
    }

    /// <summary>
    /// Ghi nhận ping từ board ESP32
    /// </summary>
    public void RecordPing(string? ip, int? rssi, string? version)
    {
        DeviceStatus.LastPingUtc = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(ip)) DeviceStatus.IpAddress = ip;
        if (rssi.HasValue) DeviceStatus.Rssi = rssi.Value;
        if (!string.IsNullOrWhiteSpace(version)) DeviceStatus.FirmwareVersion = version;
    }

    /// <summary>
    /// Đưa màn hình về trạng thái Chờ (Idle / Chào mừng)
    /// </summary>
    public void SetIdle(string? customMessage = null)
    {
        lock (_lock)
        {
            _currentState = Esp32DisplayState.Idle;
            _orderCode = "";
            _amount = 0;
            _customerName = "";
            _qrContent = "";
            _paidBank = "";
            _stateChangedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Kích hoạt màn hình hiển thị mã VietQR thanh toán
    /// </summary>
    public void SetWaitingPayment(string orderCode, decimal amount, string? customerName = null)
    {
        lock (_lock)
        {
            _currentState = Esp32DisplayState.WaitingPayment;
            _orderCode = orderCode;
            _amount = amount;
            _customerName = customerName ?? "Khách lẻ";
            _paidBank = "";
            _stateChangedAt = DateTime.UtcNow;

            // Sinh chuỗi VietQR chuẩn EMVCo để ESP32 có thể tự vẽ bằng thư viện qrcode
            _qrContent = GenerateVietQrPayload(orderCode, amount);
        }
    }

    /// <summary>
    /// Kích hoạt màn hình Báo Thanh Toán Thành Công
    /// </summary>
    public void SetPaymentSuccess(string orderCode, decimal amount, string? bankName = null)
    {
        lock (_lock)
        {
            _currentState = Esp32DisplayState.PaymentSuccess;
            _orderCode = orderCode;
            _amount = amount;
            _paidBank = string.IsNullOrWhiteSpace(bankName) ? "Ngân hàng" : bankName;
            _stateChangedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Lấy dữ liệu màn hình hiện tại dạng JSON cho ESP32 lấy qua HTTP GET
    /// </summary>
    public Esp32ScreenInfo GetCurrentScreenInfo()
    {
        lock (_lock)
        {
            // Tự động chuyển từ Success về Idle sau 8 giây
            if (_currentState == Esp32DisplayState.PaymentSuccess &&
                (DateTime.UtcNow - _stateChangedAt).TotalSeconds > 8)
            {
                _currentState = Esp32DisplayState.Idle;
            }

            var storeRaw = string.IsNullOrWhiteSpace(_config.StoreName) ? "TAP HOA VIET" : _config.StoreName;
            var store = RemoveDiacritics(storeRaw).ToUpper();

            switch (_currentState)
            {
                case Esp32DisplayState.WaitingPayment:
                    return new Esp32ScreenInfo
                    {
                        State = "QR",
                        StoreName = store,
                        OrderCode = _orderCode,
                        Amount = _amount,
                        AmountFormatted = $"{_amount:N0} VND",
                        CustomerName = RemoveDiacritics(_customerName),
                        QrContent = _qrContent,
                        BankName = _config.QrBankBin ?? "",
                        Message = "Quet VietQR thanh toan",
                        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                    };

                case Esp32DisplayState.PaymentSuccess:
                    return new Esp32ScreenInfo
                    {
                        State = "SUCCESS",
                        StoreName = store,
                        OrderCode = _orderCode,
                        Amount = _amount,
                        AmountFormatted = $"{_amount:N0} VND",
                        CustomerName = RemoveDiacritics(_customerName),
                        BankName = RemoveDiacritics(_paidBank),
                        Message = "Da nhan tien thanh cong!",
                        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                    };

                case Esp32DisplayState.Idle:
                default:
                    return new Esp32ScreenInfo
                    {
                        State = "IDLE",
                        StoreName = store,
                        Message = "Xin chao quy khach!",
                        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                    };
            }
        }
    }

    /// <summary>
    /// Sinh chuỗi chuẩn VietQR (EMVCo QR Code)
    /// </summary>
    public string GenerateVietQrPayload(string orderCode, decimal amount)
    {
        var bin = string.IsNullOrWhiteSpace(_config.QrBankBin) ? "970436" : _config.QrBankBin.Trim();
        var acc = string.IsNullOrWhiteSpace(_config.QrAccountNo) ? "123456789" : _config.QrAccountNo.Trim();
        var rawName = string.IsNullOrWhiteSpace(_config.QrAccountName) ? "STORE" : _config.QrAccountName.Trim().ToUpper();
        var name = RemoveDiacritics(rawName).ToUpper();
        var intAmount = (long)Math.Max(0, amount);

        // Sub-tags của Tag 38: Consumer Account Information
        // 00: GUID VietQR (A000000727)
        // 01: Sub-tag 00 (BNB ID) + 01 (Account No)
        // 02: Dịch vụ chuyển khoản nhanh (QRIBFTTA)
        var sub01 = FormatEmvTag("00", bin) + FormatEmvTag("01", acc);
        var tag38Value = FormatEmvTag("00", "A000000727") +
                         FormatEmvTag("01", sub01) +
                         FormatEmvTag("02", "QRIBFTTA");

        var sb = new StringBuilder();
        sb.Append(FormatEmvTag("00", "01")); // Format indicator
        sb.Append(FormatEmvTag("01", "12")); // 12 = Dynamic QR
        sb.Append(FormatEmvTag("38", tag38Value)); // Tag 38
        sb.Append(FormatEmvTag("53", "704")); // Currency: VND
        if (intAmount > 0)
        {
            sb.Append(FormatEmvTag("54", intAmount.ToString())); // Transaction amount
        }
        sb.Append(FormatEmvTag("58", "VN")); // Country code
        sb.Append(FormatEmvTag("59", name)); // Account Name

        // Tag 62: Additional Data (08 = Reference label / Order Code)
        var tag62Value = FormatEmvTag("08", orderCode);
        sb.Append(FormatEmvTag("62", tag62Value));

        // Tag 63: CRC16
        sb.Append("6304");
        var crc = CalculateCrc16Ccitt(sb.ToString());
        sb.Append(crc.ToString("X4"));

        return sb.ToString();
    }

    private static string RemoveDiacritics(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder(capacity: normalizedString.Length);

        for (int i = 0; i < normalizedString.Length; i++)
        {
            char c = normalizedString[i];
            var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                if (c == 'đ' || c == 'Đ')
                {
                    stringBuilder.Append('D');
                }
                else
                {
                    stringBuilder.Append(c);
                }
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static string FormatEmvTag(string id, string value)
    {
        return $"{id}{value.Length:D2}{value}";
    }

    private static ushort CalculateCrc16Ccitt(string data)
    {
        var bytes = Encoding.ASCII.GetBytes(data);
        ushort crc = 0xFFFF;
        foreach (var b in bytes)
        {
            crc ^= (ushort)(b << 8);
            for (int i = 0; i < 8; i++)
            {
                if ((crc & 0x8000) != 0)
                    crc = (ushort)((crc << 1) ^ 0x1021);
                else
                    crc <<= 1;
            }
        }
        return crc;
    }

    /// <summary>
    /// Tạo mã nguồn Arduino .ino hoàn chỉnh dành cho ESP32-C3 Super Mini + 2.8" TFT 240x320 ILI9341 SPI
    /// Chế độ WiFi: ESP32-C3 kết nối WiFi không dây và poll dữ liệu từ máy chủ POS
    /// </summary>
    public string GenerateArduinoCode(string serverIp, string wifiSsid, string wifiPass)
    {
        var storeName = string.IsNullOrWhiteSpace(_config.StoreName) ? "TAP HOA VIET" : _config.StoreName;

        return $@"/*
 * =====================================================================
 * PHẦN MỀM BÁN HÀNG - MÀN HÌNH PHỤ KHÁCH HÀNG (WIFI KHÔNG DÂY)
 * PHẦN CỨNG: TENSTAR ROBOT ESP32-C3 SUPER MINI + 2.8"" TFT 240x320 ILI9341
 * =====================================================================
 *
 * SƠ ĐỒ ĐẤU NỐI DÂY (ESP32-C3 SUPER MINI -> MÀN HÌNH TFT 2.8"" ILI9341 SPI):
 * ---------------------------------------------------------------------
 *  TFT 2.8"" PIN  | ESP32-C3 SUPER MINI | GHI CHÚ
 *  --------------+---------------------+--------------------------------
 *  VCC           | 5V (hoặc 3.3V)      | Nguồn cấp (màn hình có IC hạ áp U2)
 *  GND           | G (GND)             | Nguồn âm / Mass
 *  CS            | GPIO 5              | TFT Chip Select
 *  RESET (RES)   | GPIO 3              | TFT Reset phần cứng
 *  DC (RS)       | GPIO 4              | Data / Command
 *  SDI (MOSI)    | GPIO 7              | Hardware SPI MOSI (ESP32-C3)
 *  SCK (CLK)     | GPIO 6              | Hardware SPI Clock (ESP32-C3)
 *  LED (BLK)     | 3.3V                | Đèn nền màn hình (sáng liên tục)
 *  SDO (MISO)    | Không nối (NC)      | Không bắt buộc với hiển thị QR
 * ---------------------------------------------------------------------
 *
 * CÀI ĐẶT TRÊN ARDUINO IDE:
 * 1. Vào Sketch -> Include Library -> Manage Libraries (Ctrl+Shift+I):
 *    - Adafruit GFX Library (bởi Adafruit)
 *    - Adafruit ILI9341 (bởi Adafruit)
 *    - ArduinoJson (Benoit Blanchon - bản 6.x hoặc 7.x)
 *    - QRCode (bởi Richard Moore) -> gõ ""qrcode"" và bấm Install
 * 2. Menu Tools trên Arduino IDE:
 *    - Board: ""ESP32C3 Dev Module"" (hoặc ""AirM2M_CORE_ESP32C3"")
 *    - Flash Size: ""4MB (32Mb)""
 *    - USB CDC On Boot: ""Enabled""
 *    - Upload Speed: ""921600"" hoặc ""460800""
 * =====================================================================
 */

#include <WiFi.h>
#include <HTTPClient.h>
#include <ArduinoJson.h>
#include <Adafruit_GFX.h>
#include <Adafruit_ILI9341.h>
#include <SPI.h>
#include <qrcode.h>

// ========== CẤU HÌNH WIFI & MÁY CHỦ BÁN HÀNG ==========
const char* WIFI_SSID     = ""{wifiSsid}"";
const char* WIFI_PASSWORD = ""{wifiPass}"";
const char* SERVER_URL    = ""http://{serverIp}:8888/api/esp32/display"";
const char* PING_URL      = ""http://{serverIp}:8888/api/esp32/ping"";

// ========== CẤU HÌNH CHÂN CHO ESP32-C3 SUPER MINI -> TFT 2.8"" ILI9341 ==========
#define TFT_CS    5
#define TFT_DC    4
#define TFT_RST   3
#define TFT_MOSI  7
#define TFT_SCK   6

Adafruit_ILI9341 tft = Adafruit_ILI9341(TFT_CS, TFT_DC, TFT_RST);

// Khai báo trước các hàm (Forward Declarations)
void showScreenIdle(String store, String msg);
void showScreenQR(String qrData, String orderCode, String amount);
void showScreenSuccess(String orderCode, String amount, String bank);
void showConnectingWiFi();
void showWiFiError();
void sendPing();
void fetchScreenData();
String cleanAscii(String s);
void printCenter(String text, int y, int size = 1, uint16_t color = ILI9341_WHITE, int maxWidth = 230, int screenW = 240);

// Trạng thái hiển thị để chống chớp màn hình (Flicker Free)
String lastState = """";
String lastOrderCode = """";
unsigned long lastPingTime = 0;

void setup() {{
  Serial.begin(115200);
  delay(150);
  Serial.println(""\n--- KHOI DONG MAN HINH KHACH HANG ESP32-C3 TFT 2.8\"" WiFi ---"");

  // Reset cứng màn hình
  pinMode(TFT_RST, OUTPUT);
  digitalWrite(TFT_RST, HIGH); delay(10);
  digitalWrite(TFT_RST, LOW);  delay(20);
  digitalWrite(TFT_RST, HIGH); delay(150);

  // Khởi tạo phần cứng SPI cho ESP32-C3 (SCK=6, MOSI=7)
  SPI.begin(TFT_SCK, -1, TFT_MOSI, TFT_CS);

  // Khởi động ILI9341 với xung nhịp SPI cao 40MHz
  tft.begin(40000000);
  tft.setRotation(0); // 0 = Dọc 240x320 (Portrait), đặt bàn quét mã cực đẹp
  tft.fillScreen(ILI9341_BLACK);

  showConnectingWiFi();

  // Kết nối WiFi
  WiFi.mode(WIFI_STA);
  WiFi.begin(WIFI_SSID, WIFI_PASSWORD);
  int retry = 0;
  while (WiFi.status() != WL_CONNECTED && retry < 30) {{
    delay(500);
    Serial.print(""."");
    retry++;
  }}

  if (WiFi.status() == WL_CONNECTED) {{
    Serial.println(""\nWiFi OK! IP: "" + WiFi.localIP().toString());
    showScreenIdle(""{storeName}"", ""Xin chao quy khach!"");
  }} else {{
    Serial.println(""\nWiFi That Bai!"");
    showWiFiError();
  }}
}}

void loop() {{
  if (WiFi.status() != WL_CONNECTED) {{
    WiFi.reconnect();
    delay(2000);
    return;
  }}

  // Gửi Heartbeat Ping mỗi 10 giây
  if (millis() - lastPingTime > 10000) {{
    sendPing();
    lastPingTime = millis();
  }}

  // Lấy dữ liệu hiển thị từ Máy chủ Bán hàng
  fetchScreenData();
  delay(1200); // Lắng nghe cập nhật mỗi 1.2 giây
}}

// ================= GỬI PING BÁO TRẠNG THÁI =================
void sendPing() {{
  HTTPClient http;
  http.begin(PING_URL);
  http.addHeader(""Content-Type"", ""application/json"");
  String payload = ""{{\\""ip\\"":\"""" + WiFi.localIP().toString() +
                   ""\"",\""rssi\"":"" + String(WiFi.RSSI()) +
                   "",\""version\"":\""1.0.0\""}}"";
  http.POST(payload);
  http.end();
}}

// ================= LẤY DỮ LIỆU HIỂN THỊ =================
void fetchScreenData() {{
  HTTPClient http;
  http.begin(SERVER_URL);
  int httpCode = http.GET();

  if (httpCode == 200) {{
    String json = http.getString();
    StaticJsonDocument<1024> doc;
    DeserializationError error = deserializeJson(doc, json);

    if (!error) {{
      String state = doc[""state""].as<String>();
      String orderCode = doc[""orderCode""].as<String>();
      String amount = doc[""amountFormatted""].as<String>();
      String qrContent = doc[""qrContent""].as<String>();
      String store = doc[""storeName""].as<String>();
      String msg = doc[""message""].as<String>();
      String bank = doc[""bankName""].as<String>();

      // Chỉ vẽ lại màn hình khi có sự thay đổi trạng thái hoặc mã đơn
      if (state != lastState || orderCode != lastOrderCode) {{
        lastState = state;
        lastOrderCode = orderCode;

        if (state == ""QR"") {{
          showScreenQR(qrContent, orderCode, amount);
        }} else if (state == ""SUCCESS"") {{
          showScreenSuccess(orderCode, amount, bank);
        }} else {{
          showScreenIdle(store, msg);
        }}
      }}
    }}
  }}
  http.end();
}}

// ===== HÀM CHỐNG LỖI FONT: CHUYỂN UTF-8 TIẾNG VIỆT SANG ASCII TIÊU CHUẨN =====
String cleanAscii(String s) {{
  String out = """";
  int len = s.length();
  for (int i = 0; i < len; i++) {{
    uint8_t c = (uint8_t)s[i];
    if (c < 128) {{
      out += (char)c;
    }} else if (c == 0xC3) {{
      i++;
      if (i < len) {{
        uint8_t c2 = (uint8_t)s[i];
        switch (c2) {{
          case 0xA0: case 0xA1: case 0xA2: case 0xA3: case 0xA4: case 0xA5: out += 'a'; break;
          case 0x80: case 0x81: case 0x82: case 0x83: case 0x84: case 0x85: out += 'A'; break;
          case 0xA8: case 0xA9: case 0xAA: case 0xAB: out += 'e'; break;
          case 0x88: case 0x89: case 0x8A: case 0x8B: out += 'E'; break;
          case 0xAC: case 0xAD: case 0xAE: case 0xAF: out += 'i'; break;
          case 0x8C: case 0x8D: case 0x8E: case 0x8F: out += 'I'; break;
          case 0xB2: case 0xB3: case 0xB4: case 0xB5: case 0xB6: out += 'o'; break;
          case 0x92: case 0x93: case 0x94: case 0x95: case 0x96: out += 'O'; break;
          case 0xB9: case 0xBA: case 0xBB: case 0xBC: out += 'u'; break;
          case 0x99: case 0x9A: case 0x9B: case 0x9C: out += 'U'; break;
          case 0xBD: out += 'y'; break;
          case 0x9D: out += 'Y'; break;
          default: break;
        }}
      }}
    }} else if (c == 0xC4) {{
      i++;
      if (i < len) {{
        uint8_t c2 = (uint8_t)s[i];
        if (c2 == 0x90) out += 'D';
        else if (c2 == 0x91) out += 'd';
        else if (c2 == 0x82 || c2 == 0x83) out += 'a';
        else out += 'a';
      }}
    }} else if (c == 0xC6) {{
      i++;
      if (i < len) {{
        uint8_t c2 = (uint8_t)s[i];
        if (c2 == 0xA0 || c2 == 0xA1) out += 'o';
        else if (c2 == 0xAF || c2 == 0xB0) out += 'u';
        else out += '?';
      }}
    }} else if (c == 0xE1) {{
      i++;
      if (i + 1 < len) {{
        uint8_t b2 = (uint8_t)s[i];
        uint8_t b3 = (uint8_t)s[i + 1];
        i++;
        if (b2 == 0xBA) {{
          switch (b3) {{
            case 0xA0: case 0xA2: case 0xA4: case 0xA6: case 0xA8: case 0xAA: case 0xAC: case 0xAE: out += 'A'; break;
            case 0xA1: case 0xA3: case 0xA5: case 0xA7: case 0xA9: case 0xAB: case 0xAD: case 0xAF: out += 'a'; break;
            case 0xB0: case 0xB2: case 0xB4: case 0xB6: out += 'A'; break;
            case 0xB1: case 0xB3: case 0xB5: case 0xB7: out += 'a'; break;
            case 0xB8: case 0xBA: case 0xBC: case 0xBE: out += 'E'; break;
            case 0xB9: case 0xBB: case 0xBD: case 0xBF: out += 'e'; break;
            default: out += '?'; break;
          }}
        }} else if (b2 == 0xBB) {{
          switch (b3) {{
            case 0x80: case 0x82: case 0x84: case 0x86: out += 'E'; break;
            case 0x81: case 0x83: case 0x85: case 0x87: out += 'e'; break;
            case 0x88: case 0x8A: out += 'I'; break;
            case 0x89: case 0x8B: out += 'i'; break;
            case 0x8C: case 0x8E: case 0x90: case 0x92: case 0x94: case 0x96: case 0x98: case 0x9A: out += 'O'; break;
            case 0x8D: case 0x8F: case 0x91: case 0x93: case 0x95: case 0x97: case 0x99: case 0x9B: out += 'o'; break;
            case 0x9C: case 0x9E: out += 'O'; break;
            case 0x9D: case 0x9F: out += 'o'; break;
            case 0xA0: case 0xA2: case 0xA4: case 0xA6: out += 'O'; break;
            case 0xA1: case 0xA3: case 0xA5: case 0xA7: out += 'o'; break;
            case 0xA8: case 0xAA: case 0xAC: case 0xAE: out += 'U'; break;
            case 0xA9: case 0xAB: case 0xAD: case 0xAF: out += 'u'; break;
            case 0xB0: case 0xB2: case 0xB4: out += 'U'; break;
            case 0xB1: case 0xB3: case 0xB5: out += 'u'; break;
            case 0xB6: case 0xB8: case 0xBA: case 0xBC: out += 'Y'; break;
            case 0xB7: case 0xB9: case 0xBB: case 0xBD: out += 'y'; break;
            default: out += '?'; break;
          }}
        }}
      }}
    }}
  }}
  return out;
}}

// ===== HÀM CĂN GIỮA VÀ TỰ ĐỘNG CHỐNG TRÀN CHỮ TRÊN MÀN HÌNH 240x320 =====
void printCenter(String text, int y, int size = 1, uint16_t color = ILI9341_WHITE, int maxWidth = 230, int screenW = 240) {{
  text = cleanAscii(text);
  text.trim();
  int charW = 6 * size;
  int maxChars = maxWidth / charW;
  if (maxChars < 1) maxChars = 1;
  if ((int)text.length() > maxChars) {{
    if (size > 1) {{
      size--;
      charW = 6 * size;
      maxChars = maxWidth / charW;
    }}
    if ((int)text.length() > maxChars && maxChars > 2) {{
      text = text.substring(0, maxChars - 2) + "".."";
    }}
  }}
  int textWidth = text.length() * charW;
  int x = (screenW - textWidth) / 2;
  if (x < 2) x = 2;

  tft.setTextSize(size);
  tft.setTextColor(color);
  tft.setCursor(x, y);
  tft.println(text);
}}

// ================= GIAO DIỆN 1: MÀN HÌNH CHỜ (IDLE) =================
void showScreenIdle(String store, String msg) {{
  tft.fillScreen(ILI9341_BLACK);

  // Thanh tiêu đề phía trên (y: 0..46)
  tft.fillRect(0, 0, 240, 46, 0x0277);
  tft.drawFastHLine(0, 46, 240, 0x041F);
  printCenter(store, 14, 2, ILI9341_WHITE, 230, 240);

  // Khung biểu tượng chào mừng ở giữa
  tft.fillRoundRect(16, 68, 208, 165, 8, 0x10A2);
  tft.drawRoundRect(16, 68, 208, 165, 8, 0x21E8);

  printCenter(""XIN CHAO!"", 92, 3, ILI9341_CYAN, 196, 240);
  printCenter(""Cam on quy khach da ghe tham!"", 132, 1, ILI9341_WHITE, 196, 240);
  printCenter(""San sang thanh toan VietQR"", 152, 1, 0xCE79, 196, 240);
  printCenter(""Hen gap lai quy khach!"", 182, 1, ILI9341_GREENYELLOW, 196, 240);

  // Dưới cùng: Báo kết nối WiFi
  tft.drawFastHLine(16, 260, 208, 0x39E7);
  printCenter(""WiFi: "" + WiFi.localIP().toString(), 275, 1, ILI9341_GREEN, 230, 240);
  printCenter(""ESP32-C3 + 2.8\"" TFT 240x320"", 295, 1, 0x7BEF, 230, 240);
}}

// ================= GIAO DIỆN 2: MÃ VIETQR SẮC NÉT KÈM SỐ TIỀN =================
// Tính toán version QR nhỏ nhất tương ứng với độ dài chuỗi (tránh tràn buffer gây crash ESP32)
int getMinQrVersion(int len) {{
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
}}

#define QR_BUF_SIZE 1024
static uint8_t qrcodeData[QR_BUF_SIZE];
static QRCode qrcode;

void showScreenQR(String qrData, String orderCode, String amount) {{
  qrData.trim();
  if (qrData.length() == 0) return;

  int minVer = getMinQrVersion(qrData.length());
  if (minVer < 0 || minVer > 10) {{
    tft.fillScreen(ILI9341_BLACK);
    printCenter(""LOI: QR DATA QUA DAI!"", 130, 2, ILI9341_RED, 230, 240);
    printCenter(""Do dai: "" + String(qrData.length()), 160, 1, ILI9341_WHITE, 230, 240);
    return;
  }}

  int8_t err = -1;
  int version = minVer;
  while (err != 0 && version <= 10) {{
    uint16_t needed = qrcode_getBufferSize(version);
    if (needed > QR_BUF_SIZE) break;
    err = qrcode_initText(&qrcode, qrcodeData, version, ECC_LOW, qrData.c_str());
    if (err != 0) version++;
  }}

  if (err != 0) {{
    tft.fillScreen(ILI9341_BLACK);
    printCenter(""LOI SINH MA QR!"", 130, 2, ILI9341_RED, 230, 240);
    printCenter(""Thu lai hoac kiem tra"", 160, 1, ILI9341_WHITE, 230, 240);
    printCenter(""cai dat ngan hang."", 175, 1, ILI9341_WHITE, 230, 240);
    return;
  }}

  tft.fillScreen(ILI9341_BLACK);

  // 1. Thanh tiêu đề phía trên: Mã đơn hàng (y: 0..38)
  tft.fillRect(0, 0, 240, 38, 0x0277);
  tft.drawFastHLine(0, 38, 240, 0x041F);
  String headerText = ""QUET VIETQR"";
  if (orderCode.length() > 0) {{
    headerText = ""DON: "" + orderCode;
  }}
  printCenter(headerText, 11, 2, ILI9341_WHITE, 230, 240);

  // 2. Vẽ mã QR to ở trung tâm
  int qrSize = qrcode.size;
  int scale = 196 / qrSize;
  if (scale < 1) scale = 1;
  if (scale > 4) scale = 4;
  int qrPixelSize = qrSize * scale;
  int startX = (240 - qrPixelSize) / 2;
  int startY = 44 + (196 - qrPixelSize) / 2;

  // Khung nền trắng tương phản cao để quét siêu nhạy
  int pad = 5;
  tft.fillRoundRect(startX - pad, startY - pad, qrPixelSize + pad * 2, qrPixelSize + pad * 2, 4, ILI9341_WHITE);

  // Vẽ từng module QR
  for (uint8_t y = 0; y < qrcode.size; y++) {{
    for (uint8_t x = 0; x < qrcode.size; x++) {{
      if (qrcode_getModule(&qrcode, x, y)) {{
        tft.fillRect(startX + (x * scale),
                     startY + (y * scale),
                     scale, scale, ILI9341_BLACK);
      }}
    }}
  }}

  // 3. Khung số tiền thanh toán phía dưới (y: 248..320)
  tft.fillRect(0, 248, 240, 72, 0x0A20);
  tft.drawFastHLine(0, 248, 240, 0x21E8);

  if (amount.length() > 0 && amount != ""0 VND"") {{
    printCenter(""SO TIEN THANH TOAN:"", 253, 1, 0xCE79, 230, 240);
    printCenter(amount, 268, 2, ILI9341_YELLOW, 230, 240);
  }} else {{
    printCenter(""QUET MA DE THANH TOAN"", 264, 2, ILI9341_YELLOW, 230, 240);
  }}
  printCenter(""App Ngan hang / Vi dien tu"", 298, 1, ILI9341_WHITE, 230, 240);
}}

// ================= GIAO DIỆN 3: BÁO THÀNH CÔNG (SUCCESS) =================
void showScreenSuccess(String orderCode, String amount, String bank) {{
  tft.fillScreen(0x04A0); // Nền xanh lá đậm dịu mắt

  // Thanh tiêu đề thông báo
  tft.fillRect(0, 0, 240, 44, 0x0360);
  tft.drawFastHLine(0, 44, 240, 0x05E5);
  printCenter(""DA NHAN TIEN!"", 12, 2, ILI9341_YELLOW, 230, 240);

  // Biểu tượng tích [V]
  tft.fillCircle(120, 80, 24, ILI9341_WHITE);
  tft.drawCircle(120, 80, 24, ILI9341_YELLOW);
  tft.setTextSize(3);
  tft.setTextColor(0x04A0);
  tft.setCursor(111, 70);
  tft.println(""V"");

  // Hộp chi tiết hoá đơn
  tft.fillRoundRect(14, 115, 212, 115, 8, ILI9341_BLACK);
  tft.drawRoundRect(14, 115, 212, 115, 8, ILI9341_WHITE);

  if (orderCode.length() > 0) {{
    printCenter(""Ma don: "" + orderCode, 128, 2, ILI9341_CYAN, 200, 240);
  }}
  printCenter(amount, 158, 3, ILI9341_YELLOW, 200, 240);
  if (bank.length() > 0) {{
    printCenter(""Nhan qua: "" + bank, 198, 1, 0xCE79, 200, 240);
  }}

  printCenter(""Cam on quy khach da mua hang!"", 252, 1, ILI9341_WHITE, 230, 240);
  printCenter(""HEN GAP LAI QUY KHACH!"", 278, 2, ILI9341_GREENYELLOW, 230, 240);
}}

// ================= MÀN HÌNH KẾT NỐI WIFI =================
void showConnectingWiFi() {{
  tft.fillScreen(ILI9341_BLACK);
  printCenter(""DANG KET NOI WIFI..."", 120, 2, ILI9341_WHITE, 230, 240);
  printCenter(WIFI_SSID, 155, 2, ILI9341_YELLOW, 230, 240);
}}

// ================= MÀN HÌNH LỖI WIFI =================
void showWiFiError() {{
  tft.fillScreen(ILI9341_RED);
  printCenter(""LOI KET NOI WIFI!"", 120, 2, ILI9341_WHITE, 230, 240);
  printCenter(""Kiem tra lai mat khau / song"", 155, 1, ILI9341_YELLOW, 230, 240);
}}
";
    }

    /// <summary>
    /// Tạo mã nguồn Arduino .ino kết nối TRỰC TIẾP QUA CỔNG USB TYPE-C
    /// Không dùng WiFi - cực kỳ ổn định, máy tính tự cấp nguồn qua cổng USB
    /// Hỗ trợ hoàn hảo ESP32-C3 Super Mini + Màn hình 2.8" TFT 240x320 ILI9341
    /// </summary>
    public string GenerateArduinoUsbCode()
    {
        var storeName = string.IsNullOrWhiteSpace(_config.StoreName) ? "TAP HOA VIET" : _config.StoreName;

        return $@"/*
 * =====================================================================
 * PHẦN MỀM BÁN HÀNG - MÀN HÌNH PHỤ KHÁCH HÀNG (CÁP USB CẮM TRỰC TIẾP)
 * KẾT NỐI: CỔNG TYPE-C USB MÁY TÍNH THU NGÂN (CẤP NGUỒN + TRUYỀN DỮ LIỆU)
 * KHÔNG CẦN WIFI - KHÔNG SỢ RỚT MẠNG - ĐỘ TRỄ 0 GIÂY - CẮM LÀ CHẠY NGAY
 * PHẦN CỨNG: TENSTAR ROBOT ESP32-C3 SUPER MINI + 2.8"" TFT SPI 240x320 ILI9341
 * =====================================================================
 *
 * SƠ ĐỒ ĐẤU NỐI DÂY (ESP32-C3 SUPER MINI -> MÀN HÌNH TFT 2.8"" ILI9341 SPI):
 * ---------------------------------------------------------------------
 *  TFT 2.8"" PIN  | ESP32-C3 SUPER MINI | GHI CHÚ
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
 *
 * CÀI ĐẶT TRÊN ARDUINO IDE (RẤT QUAN TRỌNG VỚI ESP32-C3 SUPER MINI):
 * 1. Vào Sketch -> Include Library -> Manage Libraries (Ctrl+Shift+I):
 *    - Adafruit GFX Library (bởi Adafruit)
 *    - Adafruit ILI9341 (bởi Adafruit)
 *    - QRCode (bởi Richard Moore) -> gõ ""qrcode"" và bấm Install
 * 2. Menu Tools trên Arduino IDE:
 *    - Board: ""ESP32C3 Dev Module"" (hoặc ""AirM2M_CORE_ESP32C3"")
 *    - USB CDC On Boot: ""Enabled""  <--- BẮT BUỘC BẬT DÒNG NÀY ĐỂ GIAO TIẾP QUA CỔNG TYPE-C!
 *    - Flash Size: ""4MB (32Mb)""
 *    - Upload Speed: ""921600"" hoặc ""460800""
 * =====================================================================
 */

#include <Adafruit_GFX.h>
#include <Adafruit_ILI9341.h>
#include <SPI.h>
#include <qrcode.h>

// ========== CẤU HÌNH CHÂN CHO ESP32-C3 SUPER MINI -> TFT 2.8"" ILI9341 ==========
#define TFT_CS    5
#define TFT_DC    4
#define TFT_RST   3
#define TFT_MOSI  7
#define TFT_SCK   6

Adafruit_ILI9341 tft = Adafruit_ILI9341(TFT_CS, TFT_DC, TFT_RST);

// Khai báo trước các hàm (Forward Declarations)
void showScreenIdle(String store, String msg);
void showScreenQR(String qrData, String orderCode, String amount);
void showScreenSuccess(String orderCode, String amount, String bank);
void processUsbCommand(String cmdLine);
String cleanAscii(String s);
void printCenter(String text, int y, int size = 1, uint16_t color = ILI9341_WHITE, int maxWidth = 230, int screenW = 240);

void setup() {{
  // Khởi động cổng Serial USB tốc độ cao 115200 baud
  Serial.begin(115200);
  Serial.setTimeout(50);
  delay(200); // Chờ USB CDC ổn định

  // Reset cứng màn hình trước khi khởi tạo
  pinMode(TFT_RST, OUTPUT);
  digitalWrite(TFT_RST, HIGH); delay(10);
  digitalWrite(TFT_RST, LOW);  delay(20);
  digitalWrite(TFT_RST, HIGH); delay(150);

  // Khởi tạo phần cứng SPI cho ESP32-C3 (SCK=6, MOSI=7)
  SPI.begin(TFT_SCK, -1, TFT_MOSI, TFT_CS);

  // Khởi động ILI9341 với xung nhịp SPI cao 40MHz
  tft.begin(40000000);
  tft.setRotation(0); // 0 = Dọc 240x320 (Portrait)
  tft.fillScreen(ILI9341_BLACK);
  delay(150);

  // Hiển thị màn hình chờ ban đầu
  showScreenIdle(""{storeName}"", ""Xin chao quy khach!"");
  Serial.println(""READY|ESP32_C3_CUSTOMER_DISPLAY_USB_OK"");
}}

// Buffer tích lũy lệnh USB từng ký tự — tránh bị cắt cụt khi dữ liệu dài qua USB CDC
String _usbInputBuffer = """";

void loop() {{
  while (Serial.available() > 0) {{
    char c = (char)Serial.read();
    if (c == '\n') {{
      _usbInputBuffer.trim();
      if (_usbInputBuffer.length() > 0) {{
        processUsbCommand(_usbInputBuffer);
        _usbInputBuffer = """";
      }}
    }} else if (c != '\r') {{
      _usbInputBuffer += c;
      if (_usbInputBuffer.length() > 512) {{
        _usbInputBuffer = """";
      }}
    }}
  }}
}}

// ================= XỬ LÝ LỆNH TỪ MÁY TÍNH QUA USB =================
void processUsbCommand(String cmdLine) {{
  cmdLine.trim();
  if (cmdLine.length() == 0) return;

  if (cmdLine == ""PING"") {{
    Serial.println(""PONG|ESP32_C3_TFT28|USB_ONLINE"");
    return;
  }}

  int firstPipe = cmdLine.indexOf('|');
  if (firstPipe == -1) return;

  String action = cmdLine.substring(0, firstPipe);
  String rest = cmdLine.substring(firstPipe + 1);

  if (action == ""QR"") {{
    // Cú pháp hỗ trợ linh hoạt:
    // 1) QR|chuoi_vietqr_emvco
    // 2) QR|HD000099|250.000 VND|chuoi_vietqr_emvco
    String orderCode = """";
    String amount = """";
    String qrData = rest;

    int secondPipe = rest.indexOf('|');
    if (secondPipe != -1) {{
      orderCode = rest.substring(0, secondPipe);
      String rest2 = rest.substring(secondPipe + 1);
      int thirdPipe = rest2.indexOf('|');
      if (thirdPipe != -1) {{
        amount = rest2.substring(0, thirdPipe);
        qrData = rest2.substring(thirdPipe + 1);
      }} else {{
        qrData = rest2;
      }}
    }}

    showScreenQR(qrData, orderCode, amount);
    Serial.println(""ACK|QR_SHOWN"");
  }}
  else if (action == ""SUCCESS"") {{
    // Cú pháp: SUCCESS|HD000099|250.000 VND|MBBank
    int secondPipe = rest.indexOf('|');
    if (secondPipe != -1) {{
      String orderCode = rest.substring(0, secondPipe);
      String rest2 = rest.substring(secondPipe + 1);
      int thirdPipe = rest2.indexOf('|');
      String amount = (thirdPipe != -1) ? rest2.substring(0, thirdPipe) : rest2;
      String bank = (thirdPipe != -1) ? rest2.substring(thirdPipe + 1) : """";
      showScreenSuccess(orderCode, amount, bank);
      Serial.println(""ACK|SUCCESS_SHOWN|"" + orderCode);
    }}
  }}
  else if (action == ""IDLE"") {{
    // Cú pháp: IDLE|TenCuaHang|LoiChao
    int secondPipe = rest.indexOf('|');
    String store = (secondPipe != -1) ? rest.substring(0, secondPipe) : ""{storeName}"";
    String msg = (secondPipe != -1) ? rest.substring(secondPipe + 1) : ""Xin chao quy khach!"";
    showScreenIdle(store, msg);
    Serial.println(""ACK|IDLE_SHOWN"");
  }}
}}

// ===== HÀM CHỐNG LỖI FONT: CHUYỂN UTF-8 TIẾNG VIỆT SANG ASCII TIÊU CHUẨN =====
// Hỗ trợ đầy đủ 2-byte (U+00C0..U+017F) và 3-byte (U+1E00..U+1EFF) tiếng Việt
String cleanAscii(String s) {{
  String out = "";
  int len = s.length();
  for (int i = 0; i < len; i++) {{
    uint8_t c = (uint8_t)s[i];
    if (c < 128) {{
      out += (char)c;
    }} else if (c == 0xC3) {{
      i++;
      if (i < len) {{
        uint8_t c2 = (uint8_t)s[i];
        switch (c2) {{
          case 0xA0: case 0xA1: case 0xA2: case 0xA3: case 0xA4: case 0xA5: out += 'a'; break;
          case 0x80: case 0x81: case 0x82: case 0x83: case 0x84: case 0x85: out += 'A'; break;
          case 0xA8: case 0xA9: case 0xAA: case 0xAB: out += 'e'; break;
          case 0x88: case 0x89: case 0x8A: case 0x8B: out += 'E'; break;
          case 0xAC: case 0xAD: case 0xAE: case 0xAF: out += 'i'; break;
          case 0x8C: case 0x8D: case 0x8E: case 0x8F: out += 'I'; break;
          case 0xB2: case 0xB3: case 0xB4: case 0xB5: case 0xB6: out += 'o'; break;
          case 0x92: case 0x93: case 0x94: case 0x95: case 0x96: out += 'O'; break;
          case 0xB9: case 0xBA: case 0xBB: case 0xBC: out += 'u'; break;
          case 0x99: case 0x9A: case 0x9B: case 0x9C: out += 'U'; break;
          case 0xBD: out += 'y'; break;
          case 0x9D: out += 'Y'; break;
          default: break;
        }}
      }}
    }} else if (c == 0xC4) {{
      i++;
      if (i < len) {{
        uint8_t c2 = (uint8_t)s[i];
        if (c2 == 0x90) out += 'D';
        else if (c2 == 0x91) out += 'd';
        else if (c2 == 0x82 || c2 == 0x83) out += 'a';  // Ă/ă
        else out += 'a';
      }}
    }} else if (c == 0xC6) {{
      i++;
      if (i < len) {{
        uint8_t c2 = (uint8_t)s[i];
        if (c2 == 0xA0 || c2 == 0xA1) out += 'o';  // Ơ/ơ
        else if (c2 == 0xAF || c2 == 0xB0) out += 'u';  // Ư/ư
        else out += '?';
      }}
    }} else if (c == 0xE1) {{
      i++;
      if (i + 1 < len) {{
        uint8_t b2 = (uint8_t)s[i];
        uint8_t b3 = (uint8_t)s[i + 1];
        i++;
        if (b2 == 0xBA) {{
          switch (b3) {{
            case 0xA0: case 0xA2: case 0xA4: case 0xA6: case 0xA8: case 0xAA: case 0xAC: case 0xAE: out += 'A'; break;
            case 0xA1: case 0xA3: case 0xA5: case 0xA7: case 0xA9: case 0xAB: case 0xAD: case 0xAF: out += 'a'; break;
            case 0xB0: case 0xB2: case 0xB4: case 0xB6: out += 'A'; break;
            case 0xB1: case 0xB3: case 0xB5: case 0xB7: out += 'a'; break;
            case 0xB8: case 0xBA: case 0xBC: case 0xBE: out += 'E'; break;
            case 0xB9: case 0xBB: case 0xBD: case 0xBF: out += 'e'; break;
            default: break;
          }}
        }} else if (b2 == 0xBB) {{
          switch (b3) {{
            case 0x80: case 0x82: case 0x84: case 0x86: out += 'E'; break;
            case 0x81: case 0x83: case 0x85: case 0x87: out += 'e'; break;
            case 0x88: case 0x8A: out += 'I'; break;
            case 0x89: case 0x8B: out += 'i'; break;
            case 0x8C: case 0x8E: case 0x90: case 0x92: case 0x94: case 0x96: case 0x98: case 0x9A: case 0x9C: case 0x9E: out += 'O'; break;
            case 0x8D: case 0x8F: case 0x91: case 0x93: case 0x95: case 0x97: case 0x99: case 0x9B: case 0x9D: case 0x9F: out += 'o'; break;
            case 0xA0: case 0xA2: case 0xA4: case 0xA6: out += 'O'; break;
            case 0xA1: case 0xA3: case 0xA5: case 0xA7: out += 'o'; break;
            case 0xA8: case 0xAA: case 0xAC: case 0xAE: out += 'U'; break;
            case 0xA9: case 0xAB: case 0xAD: case 0xAF: out += 'u'; break;
            case 0xB0: case 0xB2: case 0xB4: out += 'U'; break;
            case 0xB1: case 0xB3: case 0xB5: out += 'u'; break;
            case 0xB6: case 0xB8: case 0xBA: case 0xBC: out += 'Y'; break;
            case 0xB7: case 0xB9: case 0xBB: case 0xBD: out += 'y'; break;
            default: break;
          }}
        }}
      }}
    }}
  }}
  return out;
}}

// ===== HÀM CĂN GIỮA VÀ TỰ ĐỘNG CHỐNG TRÀN CHỮ TRÊN MÀN HÌNH 128x160 =====
// screenW: chiều ngang màn hình (128 khi dọc, 160 khi ngang)
void printCenter(String text, int y, int size = 1, uint16_t color = ST77XX_WHITE, int maxWidth = 124, int screenW = 128) {{
  text = cleanAscii(text);
  text.trim();
  int charW = 6 * size;
  int maxChars = maxWidth / charW;
  if ((int)text.length() > maxChars) {{
    if (size > 1) {{
      size = 1;
      charW = 6;
      maxChars = maxWidth / charW;
    }}
    if ((int)text.length() > maxChars) {{
      text = text.substring(0, maxChars - 2) + "".."";
    }}
  }}
  int textWidth = text.length() * charW;
  int x = (screenW - textWidth) / 2;
  if (x < 2) x = 2;

  tft.setTextSize(size);
  tft.setTextColor(color);
  tft.setCursor(x, y);
  tft.println(text);
}}

// ================= GIAO DIỆN 1: MÀN HÌNH CHỜ (IDLE) =================
void showScreenIdle(String store, String msg) {{
  tft.fillScreen(ILI9341_BLACK);

  // Thanh tiêu đề phía trên (y: 0..46)
  tft.fillRect(0, 0, 240, 46, 0x0277);
  tft.drawFastHLine(0, 46, 240, 0x041F);
  printCenter(store, 14, 2, ILI9341_WHITE, 230, 240);

  // Khung biểu tượng chào mừng ở giữa
  tft.fillRoundRect(16, 68, 208, 165, 8, 0x10A2);
  tft.drawRoundRect(16, 68, 208, 165, 8, 0x21E8);

  printCenter(""XIN CHAO!"", 92, 3, ILI9341_CYAN, 196, 240);
  printCenter(""Cam on quy khach da ghe tham!"", 132, 1, ILI9341_WHITE, 196, 240);
  printCenter(""San sang thanh toan VietQR"", 152, 1, 0xCE79, 196, 240);
  printCenter(""Hen gap lai quy khach!"", 182, 1, ILI9341_GREENYELLOW, 196, 240);

  // Dưới cùng: Báo kết nối USB ổn định
  tft.drawFastHLine(16, 260, 208, 0x39E7);
  printCenter(""KET NOI: USB SERIAL 115200 BAUD"", 275, 1, ILI9341_GREEN, 230, 240);
  printCenter(""ESP32-C3 + 2.8\"" TFT 240x320"", 295, 1, 0x7BEF, 230, 240);
}}

// ================= GIAO DIỆN 2: MÃ VIETQR SẮC NÉT KÈM SỐ TIỀN =================
// Tính toán version QR nhỏ nhất tương ứng với độ dài chuỗi (tránh tràn buffer gây crash ESP32)
int getMinQrVersion(int len) {{
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
}}

#define QR_BUF_SIZE 1024
static uint8_t qrcodeData[QR_BUF_SIZE];
static QRCode qrcode;

void showScreenQR(String qrData, String orderCode, String amount) {{
  qrData.trim();
  if (qrData.length() == 0) return;

  int minVer = getMinQrVersion(qrData.length());
  if (minVer < 0 || minVer > 10) {{
    tft.fillScreen(ILI9341_BLACK);
    printCenter(""LOI: QR DATA QUA DAI!"", 130, 2, ILI9341_RED, 230, 240);
    printCenter(""Do dai: "" + String(qrData.length()), 160, 1, ILI9341_WHITE, 230, 240);
    return;
  }}

  int8_t err = -1;
  int version = minVer;
  while (err != 0 && version <= 10) {{
    uint16_t needed = qrcode_getBufferSize(version);
    if (needed > QR_BUF_SIZE) break;
    err = qrcode_initText(&qrcode, qrcodeData, version, ECC_LOW, qrData.c_str());
    if (err != 0) version++;
  }}

  if (err != 0) {{
    tft.fillScreen(ILI9341_BLACK);
    printCenter(""LOI SINH MA QR!"", 130, 2, ILI9341_RED, 230, 240);
    printCenter(""Thu lai hoac kiem tra"", 160, 1, ILI9341_WHITE, 230, 240);
    printCenter(""cai dat ngan hang."", 175, 1, ILI9341_WHITE, 230, 240);
    return;
  }}

  tft.fillScreen(ILI9341_BLACK);

  // 1. Thanh tiêu đề phía trên: Mã đơn hàng (y: 0..38)
  tft.fillRect(0, 0, 240, 38, 0x0277);
  tft.drawFastHLine(0, 38, 240, 0x041F);
  String headerText = ""QUET VIETQR"";
  if (orderCode.length() > 0) {{
    headerText = ""DON: "" + orderCode;
  }}
  printCenter(headerText, 11, 2, ILI9341_WHITE, 230, 240);

  // 2. Vẽ mã QR to ở trung tâm
  int qrSize = qrcode.size;
  int scale = 196 / qrSize;
  if (scale < 1) scale = 1;
  if (scale > 4) scale = 4;
  int qrPixelSize = qrSize * scale;
  int startX = (240 - qrPixelSize) / 2;
  int startY = 44 + (196 - qrPixelSize) / 2;

  // Khung nền trắng tương phản cao để quét siêu nhạy
  int pad = 5;
  tft.fillRoundRect(startX - pad, startY - pad, qrPixelSize + pad * 2, qrPixelSize + pad * 2, 4, ILI9341_WHITE);

  // Vẽ từng module QR
  for (uint8_t y = 0; y < qrcode.size; y++) {{
    for (uint8_t x = 0; x < qrcode.size; x++) {{
      if (qrcode_getModule(&qrcode, x, y)) {{
        tft.fillRect(startX + (x * scale),
                     startY + (y * scale),
                     scale, scale, ILI9341_BLACK);
      }}
    }}
  }}

  // 3. Khung số tiền thanh toán phía dưới (y: 248..320)
  tft.fillRect(0, 248, 240, 72, 0x0A20);
  tft.drawFastHLine(0, 248, 240, 0x21E8);

  if (amount.length() > 0 && amount != ""0 VND"") {{
    printCenter(""SO TIEN THANH TOAN:"", 253, 1, 0xCE79, 230, 240);
    printCenter(amount, 268, 2, ILI9341_YELLOW, 230, 240);
  }} else {{
    printCenter(""QUET MA DE THANH TOAN"", 264, 2, ILI9341_YELLOW, 230, 240);
  }}
  printCenter(""App Ngan hang / Vi dien tu"", 298, 1, ILI9341_WHITE, 230, 240);
}}

// ================= GIAO DIỆN 3: BÁO THÀNH CÔNG (SUCCESS) =================
void showScreenSuccess(String orderCode, String amount, String bank) {{
  tft.fillScreen(0x04A0); // Nền xanh lá đậm dịu mắt

  // Thanh tiêu đề thông báo
  tft.fillRect(0, 0, 240, 44, 0x0360);
  tft.drawFastHLine(0, 44, 240, 0x05E5);
  printCenter(""DA NHAN TIEN!"", 12, 2, ILI9341_YELLOW, 230, 240);

  // Biểu tượng tích [V]
  tft.fillCircle(120, 80, 24, ILI9341_WHITE);
  tft.drawCircle(120, 80, 24, ILI9341_YELLOW);
  tft.setTextSize(3);
  tft.setTextColor(0x04A0);
  tft.setCursor(111, 70);
  tft.println(""V"");

  // Hộp chi tiết hoá đơn
  tft.fillRoundRect(14, 115, 212, 115, 8, ILI9341_BLACK);
  tft.drawRoundRect(14, 115, 212, 115, 8, ILI9341_WHITE);

  if (orderCode.length() > 0) {{
    printCenter(""Ma don: "" + orderCode, 128, 2, ILI9341_CYAN, 200, 240);
  }}
  printCenter(amount, 158, 3, ILI9341_YELLOW, 200, 240);
  if (bank.length() > 0) {{
    printCenter(""Nhan qua: "" + bank, 198, 1, 0xCE79, 200, 240);
  }}

  printCenter(""Cam on quy khach da mua hang!"", 252, 1, ILI9341_WHITE, 230, 240);
  printCenter(""HEN GAP LAI QUY KHACH!"", 278, 2, ILI9341_GREENYELLOW, 230, 240);
}}
";
    }
}
