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
    public string ScreenResolution { get; set; } = "128x160 ST7735";
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
    /// Tạo mã nguồn Arduino .ino hoàn chỉnh dành cho ESP32 + ST7735 1.8" SPI 128x160
    /// Chế độ WiFi: ESP32 tự kết nối WiFi và poll dữ liệu từ server POS
    /// </summary>
    public string GenerateArduinoCode(string serverIp, string wifiSsid, string wifiPass)
    {
        var storeName = string.IsNullOrWhiteSpace(_config.StoreName) ? "TAP HOA VIET" : _config.StoreName;

        return $@"/*
 *
 * SƠ ĐỒ ĐẤU DÂY (ESP32 -> TFT ST7735 1.8"" 128x160 SPI):
 * --------------------------------------------------------
 *  TFT PIN   | ESP32 GPIO  | GHI CHÚ
 *  ----------+-------------+-----------------------------
 *  VCC       | 3.3V (hoặc 5V nếu board có LDO 3.3V)
 *  GND       | GND
 *  CS        | GPIO 5      | Chip Select
 *  RESET     | GPIO 4      | Reset màn hình
 *  A0 (DC)   | GPIO 2      | Data / Command
 *  SDA (MOSI)| GPIO 23     | Hardware SPI MOSI
 *  SCK (SCLK)| GPIO 18     | Hardware SPI CLK
 *  LED (BLK) | 3.3V        | Đèn nền (Backlight)
 * --------------------------------------------------------
 *
 * THƯ VIỆN CẦN CÀI TRÊN ARDUINO IDE:
 * 1. Adafruit GFX Library (Adafruit)
 * 2. Adafruit ST7735 and ST7789 Library (Adafruit)
 * 3. ArduinoJson (Benoit Blanchon - bản 6.x hoặc 7.x)
 * (Mã QR dùng thư viện có sẵn trong nhân ESP32, KHÔNG CẦN CÀI THÊM THƯ VIỆN QR)
 * =====================================================================
 */

#include <WiFi.h>
#include <HTTPClient.h>
#include <ArduinoJson.h>
#include <Adafruit_GFX.h>
#include <Adafruit_ST7735.h>
#include <SPI.h>
#include ""esp_qrcode.h""

// ========== CẤU HÌNH WIFI & MÁY CHỦ BÁN HÀNG ==========
const char* WIFI_SSID     = ""{wifiSsid}"";
const char* WIFI_PASSWORD = ""{wifiPass}"";
const char* SERVER_URL    = ""http://{serverIp}:5050/api/esp32/display"";
const char* PING_URL      = ""http://{serverIp}:5050/api/esp32/ping"";

// ========== CẤU HÌNH CHÂN SPI TFT ST7735 ==========
#define TFT_CS    5
#define TFT_RST   4
#define TFT_DC    2
// MOSI = 23, SCK = 18 (SPI mặc định của ESP32)

Adafruit_ST7735 tft = Adafruit_ST7735(TFT_CS, TFT_DC, TFT_RST);

// Trạng thái hiển thị để tránh chớp màn hình (Flicker)
String lastState = """";
String lastOrderCode = """";
unsigned long lastPingTime = 0;

void setup() {{
  Serial.begin(115200);
  Serial.println(""\n--- KHOI DONG MAN HINH KHACH HANG ESP32 WiFi ---"");

  // Khởi tạo SPI phần cứng ESP32 (MOSI=23, SCK=18)
  SPI.begin(18, -1, 23, TFT_CS);

  // Khởi tạo màn hình ST7735 1.8 inch (128x160)
  // LƯU Ý: Hầu hết màn hình TFT 1.8"" ST7735 bán ở VN dùng INITR_BLACKTAB
  tft.initR(INITR_BLACKTAB); // Thử BLACKTAB - hầu hết module China dùng loại này
  tft.setRotation(0);        // 0: 128x160 Dọc, 1: 160x128 Ngang
  tft.fillScreen(ST77XX_BLACK);
  delay(150); // Chờ màn hình ổn định

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
  delay(1200); // Lắng nghe cập nhật mỗi 1.2s
}}

// ================= GỬI PING BÁO TRẠNG THÁI =================
void sendPing() {{
  HTTPClient http;
  http.begin(PING_URL);
  http.addHeader(""Content-Type"", ""application/json"");
  String payload = ""{{\\""ip\\"":\"" "" + WiFi.localIP().toString() +
                   ""\\"",\\""rssi\\"":"" + String(WiFi.RSSI()) +
                   "",\\""version\\"":""\\""1.0.0\\""}}"";"";
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

      // Chỉ vẽ lại màn hình khi có sự thay đổi trạng thái hoặc mã đơn
      if (state != lastState || orderCode != lastOrderCode) {{
        lastState = state;
        lastOrderCode = orderCode;

        if (state == ""QR"") {{
          showScreenQR(qrContent); // Full màn hình mã QR
        }} else if (state == ""SUCCESS"") {{
          showScreenSuccess(orderCode, amount);
        }} else {{
          showScreenIdle(store, msg);
        }}
      }}
    }}
  }}
  http.end();
}}

// ===== HÀM CHỐNG LỖI FONT: CHUYỂN UTF-8 TIẾNG VIỆT SANG ASCII TIÊU CHUẨN =====
// Hỗ trợ đầy đủ 2-byte (U+00C0..U+017F) và 3-byte (U+1E00..U+1EFF) tiếng Việt
String cleanAscii(String s) {{
  String out = "";
  int len = s.length();
  for (int i = 0; i < len; i++) {{
    uint8_t c = (uint8_t)s[i];
    if (c < 128) {{
      // ASCII thuần – giữ nguyên
      out += (char)c;
    }} else if (c == 0xC3) {{
      // 2-byte UTF-8 U+00C0..U+00FF (Latin Extended)
      i++;
      if (i < len) {{
        uint8_t c2 = (uint8_t)s[i];
        switch (c2) {{
          // a, à, á, â, ã, ä, å
          case 0xA0: case 0xA1: case 0xA2: case 0xA3: case 0xA4: case 0xA5: out += 'a'; break;
          // A, À, Á, Â, Ã, Ä, Å
          case 0x80: case 0x81: case 0x82: case 0x83: case 0x84: case 0x85: out += 'A'; break;
          // e, è, é, ê, ë
          case 0xA8: case 0xA9: case 0xAA: case 0xAB: out += 'e'; break;
          // E, È, É, Ê, Ë
          case 0x88: case 0x89: case 0x8A: case 0x8B: out += 'E'; break;
          // i, ì, í, î, ï
          case 0xAC: case 0xAD: case 0xAE: case 0xAF: out += 'i'; break;
          // I, Ì, Í, Î, Ï
          case 0x8C: case 0x8D: case 0x8E: case 0x8F: out += 'I'; break;
          // o, ò, ó, ô, õ, ö
          case 0xB2: case 0xB3: case 0xB4: case 0xB5: case 0xB6: out += 'o'; break;
          // O, Ò, Ó, Ô, Õ, Ö
          case 0x92: case 0x93: case 0x94: case 0x95: case 0x96: out += 'O'; break;
          // u, ù, ú, û, ü
          case 0xB9: case 0xBA: case 0xBB: case 0xBC: out += 'u'; break;
          // U, Ù, Ú, Û, Ü
          case 0x99: case 0x9A: case 0x9B: case 0x9C: out += 'U'; break;
          // y, ý
          case 0xBD: out += 'y'; break;
          // Y, Ý
          case 0x9D: out += 'Y'; break;
          default: break;
        }}
      }}
    }} else if (c == 0xC4) {{
      // 2-byte UTF-8 U+0100..U+017F: Đ/đ và ă/Ă, ơ/Ơ, ư/Ư...
      i++;
      if (i < len) {{
        uint8_t c2 = (uint8_t)s[i];
        if (c2 == 0x90) out += 'D';       // Đ
        else if (c2 == 0x91) out += 'd';  // đ
        else if (c2 == 0x82 || c2 == 0x83) out += 'a';  // Ă/ă
        else out += 'a';
      }}
    }} else if (c == 0xC6) {{
      // 2-byte UTF-8 Ơ/ơ (0xC6 0xA0/0xA1), Ư/ư (0xC6 0xAF/0xB0)
      i++;
      if (i < len) {{
        uint8_t c2 = (uint8_t)s[i];
        if (c2 == 0xA0 || c2 == 0xA1) out += 'o';  // Ơ/ơ
        else if (c2 == 0xAF || c2 == 0xB0) out += 'u';  // Ư/ư
        else out += '?';
      }}
    }} else if (c == 0xE1) {{
      // 3-byte UTF-8 U+1E00..U+1EFF (Latin Extended Additional - tiếng Việt có dấu)
      // Byte pattern: 0xE1 0xB8..0xBF hoặc 0xE1 0xBB..0xBF
      i++;
      if (i + 1 < len) {{
        uint8_t b2 = (uint8_t)s[i];
        uint8_t b3 = (uint8_t)s[i + 1];
        i++;
        // U+1EA0..U+1EF9 - Bảng chữ có dấu tiếng Việt
        if (b2 == 0xBA) {{
          switch (b3) {{
            // Ạ=A0, ạ=A1, Ả=A2, ả=A3, Ấ=A4, ấ=A5, Ầ=A6, ầ=A7
            case 0xA0: case 0xA2: case 0xA4: case 0xA6: case 0xA8: case 0xAA: case 0xAC: case 0xAE: out += 'A'; break;
            case 0xA1: case 0xA3: case 0xA5: case 0xA7: case 0xA9: case 0xAB: case 0xAD: case 0xAF: out += 'a'; break;
            // Ặ=B0..Ắ=B4..Ặ=B6
            case 0xB0: case 0xB2: case 0xB4: case 0xB6: out += 'A'; break;
            case 0xB1: case 0xB3: case 0xB5: case 0xB7: out += 'a'; break;
            // Ẹ=B8, ẹ=B9, Ẻ=BA, ẻ=BB, Ẽ=BC, ẽ=BD, Ế=BE, ế=BF
            case 0xB8: case 0xBA: case 0xBC: case 0xBE: out += 'E'; break;
            case 0xB9: case 0xBB: case 0xBD: case 0xBF: out += 'e'; break;
            default: out += '?'; break;
          }}
        }} else if (b2 == 0xBB) {{
          switch (b3) {{
            // Ề=80, ề=81, Ể=82, ể=83, Ễ=84, ễ=85, Ệ=86, ệ=87
            case 0x80: case 0x82: case 0x84: case 0x86: out += 'E'; break;
            case 0x81: case 0x83: case 0x85: case 0x87: out += 'e'; break;
            // Ỉ=88, ỉ=89, Ị=8A, ị=8B
            case 0x88: case 0x8A: out += 'I'; break;
            case 0x89: case 0x8B: out += 'i'; break;
            // Ọ=8C, ọ=8D, Ỏ=8E, ỏ=8F, Ố=90, ố=91, Ồ=92, ồ=93
            case 0x8C: case 0x8E: case 0x90: case 0x92: case 0x94: case 0x96: case 0x98: case 0x9A: out += 'O'; break;
            case 0x8D: case 0x8F: case 0x91: case 0x93: case 0x95: case 0x97: case 0x99: case 0x9B: out += 'o'; break;
            // Ộ=9C, ộ=9D, Ớ=9E, ớ=9F
            case 0x9C: case 0x9E: out += 'O'; break;
            case 0x9D: case 0x9F: out += 'o'; break;
            // Ờ=A0..Ợ=A6
            case 0xA0: case 0xA2: case 0xA4: case 0xA6: out += 'O'; break;
            case 0xA1: case 0xA3: case 0xA5: case 0xA7: out += 'o'; break;
            // Ụ=A8, ụ=A9, Ủ=AA, ủ=AB, Ứ=AC, ứ=AD, Ừ=AE, ừ=AF
            case 0xA8: case 0xAA: case 0xAC: case 0xAE: out += 'U'; break;
            case 0xA9: case 0xAB: case 0xAD: case 0xAF: out += 'u'; break;
            // Ử=B0, ử=B1, Ữ=B2, ữ=B3, Ự=B4, ự=B5
            case 0xB0: case 0xB2: case 0xB4: out += 'U'; break;
            case 0xB1: case 0xB3: case 0xB5: out += 'u'; break;
            // Ỳ=B6, ỳ=B7, Ỵ=B8, ỵ=B9, Ỷ=BA, ỷ=BB, Ỹ=BC, ỹ=BD
            case 0xB6: case 0xB8: case 0xBA: case 0xBC: out += 'Y'; break;
            case 0xB7: case 0xB9: case 0xBB: case 0xBD: out += 'y'; break;
            default: out += '?'; break;
          }}
        }} else {{
          // Các byte khác trong range 0xE1 - bỏ qua
        }}
      }}
    }} else {{
      // Byte không xác định - bỏ qua
    }}
  }}
  return out;
}}

// ===== HÀM CĂN GIỮA VÀ TỰ ĐỘNG CHỐNG TRÀN CHỮ TRÊN MÀN HÌNH 128x160 =====
void printCenter(String text, int y, int size = 1, uint16_t color = ST77XX_WHITE, int maxWidth = 124) {{
  text = cleanAscii(text);
  text.trim();
  int charW = 6 * size;
  int maxChars = maxWidth / charW;
  if (text.length() > maxChars) {{
    if (size > 1) {{
      size = 1;
      charW = 6;
      maxChars = maxWidth / charW;
    }}
    if (text.length() > maxChars) {{
      text = text.substring(0, maxChars - 2) + "..";
    }}
  }}
  int textWidth = text.length() * charW;
  int x = (128 - textWidth) / 2;
  if (x < 2) x = 2;

  tft.setTextSize(size);
  tft.setTextColor(color);
  tft.setCursor(x, y);
  tft.println(text);
}}

// ================= GIAO DIỆN 1: MÀN HÌNH CHỜ (IDLE) =================
void showScreenIdle(String store, String msg) {{
  tft.fillScreen(ST77XX_BLACK);

  // Thanh tiêu đề màu xanh navy (0x0277)
  tft.fillRect(0, 0, 128, 28, 0x0277);
  tft.drawFastHLine(0, 28, 128, 0x041F);
  printCenter(store, 10, 1, ST77XX_WHITE, 120);

  // Lời chào trung tâm to rõ
  printCenter("XIN CHAO!", 54, 2, ST77XX_CYAN, 120);
  printCenter("Cam on quy khach!", 86, 1, ST77XX_WHITE, 120);
  printCenter("Hen gap lai!", 102, 1, 0xCE79, 120);

  // Dưới cùng: Tín hiệu kết nối
  tft.drawFastHLine(10, 136, 108, 0x39E7);
  printCenter("WiFi: ONLINE", 144, 1, ST77XX_GREEN, 120);
}}

// ================= GIAO DIỆN 2: MÃ VIETQR FULL MÀN HÌNH =================
// Bỏ hết chữ, logo, thông tin tài khoản - chỉ hiển thị mã QR to nhất có thể
void showScreenQR(String qrData) {{
  drawQRCodeFullScreen(qrData);
}}

// ================= GIAO DIỆN 3: BÁO THÀNH CÔNG (SUCCESS) =================
void showScreenSuccess(String orderCode, String amount) {{
  tft.fillScreen(0x04A0); // Nền xanh lá đậm dịu mắt

  // Thanh tiêu đề thông báo
  tft.fillRect(0, 0, 128, 32, 0x0360);
  printCenter("DA NHAN TIEN!", 10, 1, ST77XX_YELLOW, 120);

  // Hộp chi tiết hoá đơn
  tft.fillRoundRect(8, 42, 112, 58, 4, ST77XX_BLACK);
  tft.drawRoundRect(8, 42, 112, 58, 4, ST77XX_WHITE);

  printCenter(orderCode, 52, 1, ST77XX_CYAN, 106);
  printCenter(amount, 74, 1, ST77XX_YELLOW, 106);

  printCenter("Cam on quy khach!", 116, 1, ST77XX_WHITE, 120);
  printCenter("HEN GAP LAI", 134, 1, 0x07E0, 120);
}}

// ================= MÀN HÌNH KẾT NỐI WIFI =================
void showConnectingWiFi() {{
  tft.fillScreen(ST77XX_BLACK);
  tft.setTextColor(ST77XX_WHITE);
  tft.setTextSize(1);
  tft.setCursor(10, 50);
  tft.println(""DANG KET NOI WIFI..."");
  tft.setCursor(10, 70);
  tft.setTextColor(ST77XX_YELLOW);
  tft.println(WIFI_SSID);
}}

// ================= MÀN HÌNH LỖI WIFI =================
void showWiFiError() {{
  tft.fillScreen(ST77XX_RED);
  tft.setTextColor(ST77XX_WHITE);
  tft.setTextSize(1);
  tft.setCursor(15, 60);
  tft.println(""LOI KET NOI WIFI"");
  tft.setCursor(10, 80);
  tft.println(""Kiem tra lai Pass!"");
}}

// Callback hiển thị mã QR lên màn hình TFT 1.8 ST7735 bằng thư viện ESP32 gốc (Native)
void drawQrCodeCallback(esp_qrcode_handle_t qrcode) {{
  int size = esp_qrcode_get_size(qrcode);
  if (size <= 0) return;

  // Tính scale tối đa: dùng tới 124px chiều ngang để lề trắng 2px mỗi bên
  int scale = 124 / size;
  if (scale < 1) scale = 1;

  int qrSize = size * scale;
  // Căn chính giữa màn hình 128x160
  int startX = (128 - qrSize) / 2;
  int startY = (160 - qrSize) / 2;

  // Nền trắng toàn màn hình
  tft.fillScreen(ST77XX_WHITE);

  // Vẽ từng module QR
  for (int y = 0; y < size; y++) {{
    for (int x = 0; x < size; x++) {{
      if (esp_qrcode_get_module(qrcode, x, y)) {{
        tft.fillRect(startX + (x * scale),
                     startY + (y * scale),
                     scale, scale, ST77XX_BLACK);
      }}
    }}
  }}
}}

// ===== HÀM VẼ MÃ QR TOÀN MÀN HÌNH TFT 1.8 (128x160) - KHÔNG CÓ VIỀN / CHỮ =====
void drawQRCodeFullScreen(String text) {{
  text.trim();
  if (text.length() == 0) return;

  esp_qrcode_config_t cfg = {{
    .display_func = drawQrCodeCallback,
    .max_qrcode_version = 10,
    .qrcode_ecc_level = ESP_QRCODE_ECC_LOW
  }};

  esp_err_t res = esp_qrcode_generate(&cfg, text.c_str());
  if (res != ESP_OK) {{
    text.toUpperCase();
    res = esp_qrcode_generate(&cfg, text.c_str());
  }}

  if (res != ESP_OK) {{
    tft.fillScreen(ST77XX_BLACK);
    tft.setTextColor(ST77XX_RED);
    tft.setTextSize(1);
    tft.setCursor(10, 70);
    tft.println(""LOI DU LIEU QR!"");
  }}
}}
";
    }

    /// <summary>
    /// Tạo mã nguồn Arduino .ino kết nối TRỰC TIẾP QUA CỔNG USB SERIAL
    /// Không dùng WiFi - cực kỳ ổn định, cấp nguồn qua USB
    /// Màn hình QR hiển thị full screen không có chữ
    /// </summary>
    public string GenerateArduinoUsbCode()
    {
        var storeName = string.IsNullOrWhiteSpace(_config.StoreName) ? "TAP HOA VIET" : _config.StoreName;

        return $@"/*
 * =====================================================================
 * PHẦN MỀM TẠP HOÁ VIỆT - MÀN HÌNH PHỤ KHÁCH HÀNG (USB CẮM TRỰC TIẾP)
 * KẾT NỐI: CỔNG USB MÁY TÍNH (CẤP NGUỒN + TRUYỀN TÍN HIỆU SERIAL 115200)
 * KHÔNG DÙNG WIFI -> KHÔNG LO RỚT MẠNG, ĐỘ TRỄ 0 GIÂY, CỰC KỲ ỔN ĐỊNH
 * =====================================================================
 *
 * SƠ ĐỒ ĐẤU DÂY (ESP32 30 PIN -> TFT 1.8"" 128x160 SPI ST7735):
 * ---------------------------------------------------------------------
 *  TFT PIN   | ESP32 GPIO  | GHI CHÚ
 *  ----------+-------------+------------------------------------------
 *  VCC       | 3.3V (hoặc 5V / VIN nếu board có IC hạ áp 3.3V)
 *  GND       | GND
 *  CS        | GPIO 5      | Chip Select
 *  RESET     | GPIO 4      | Reset màn hình
 *  A0 (DC)   | GPIO 2      | Data / Command
 *  SDA (MOSI)| GPIO 23     | Hardware SPI MOSI
 *  SCK (SCLK)| GPIO 18     | Hardware SPI Clock
 *  LED (BLK) | 3.3V        | Đèn nền màn hình (Backlight)
 * ---------------------------------------------------------------------
 *
 * THƯ VIỆN CẦN CÀI TRÊN ARDUINO IDE (Chỉ cần 2 thư viện màn hình):
 * 1. Adafruit GFX Library (Adafruit)
 * 2. Adafruit ST7735 and ST7789 Library (Adafruit)
 * (Mã QR dùng thư viện có sẵn trong nhân ESP32, KHÔNG CẦN CÀI THÊM THƯ VIỆN QR)
 * =====================================================================
 */

#include <Adafruit_GFX.h>
#include <Adafruit_ST7735.h>
#include <SPI.h>
#include ""esp_qrcode.h""

// ========== CẤU HÌNH CHÂN SPI TFT ST7735 ==========
#define TFT_CS    5
#define TFT_RST   4
#define TFT_DC    2
// MOSI = GPIO 23, SCK = GPIO 18 (Hardware SPI mặc định của ESP32)

Adafruit_ST7735 tft = Adafruit_ST7735(TFT_CS, TFT_DC, TFT_RST);

void setup() {{
  // Khởi động cổng Serial USB tốc độ cao 115200 baud
  Serial.begin(115200);
  Serial.setTimeout(50); // Timeout đọc ngắn để phản hồi tức thì

  // Reset cứng màn hình trước khi init (sửa lỗi màn hình trắng)
  pinMode(TFT_RST, OUTPUT);
  digitalWrite(TFT_RST, HIGH); delay(10);
  digitalWrite(TFT_RST, LOW);  delay(20);
  digitalWrite(TFT_RST, HIGH); delay(150);

  // Khởi tạo SPI phần cứng ESP32 (MOSI=23, SCK=18)
  SPI.begin(18, -1, 23, TFT_CS);

  // Khởi tạo màn hình ST7735 1.8"" inch 128x160
  // Nếu màn hình có viền xanh lá -> dùng INITR_GREENTAB
  // Nếu màn hình có viền đen -> dùng INITR_BLACKTAB
  tft.initR(INITR_GREENTAB);  // << Viền xanh lá dùng dòng này
  // tft.initR(INITR_BLACKTAB); // << Nếu sai thì bỏ comment dòng này, comment dòng trên
  tft.setRotation(1);         // 1 = 160x128 Ngang
  tft.fillScreen(ST77XX_BLACK);
  delay(200);

  // Hiển thị màn hình chờ ban đầu
  showScreenIdle(""{storeName}"", ""Xin chao quy khach!"");
  Serial.println(""READY|ESP32_CUSTOMER_DISPLAY_USB_OK"");
}}

void loop() {{
  // Đọc lệnh gửi từ máy tính thu ngân qua cáp USB
  if (Serial.available() > 0) {{
    String line = Serial.readStringUntil('\n');
    line.trim();
    if (line.length() > 0) {{
      processUsbCommand(line);
    }}
  }}
}}

// ================= XỬ LÝ LỆNH TỪ MÁY TÍNH QUA USB =================
void processUsbCommand(String cmdLine) {{
  cmdLine.trim();
  if (cmdLine.length() == 0) return;

  if (cmdLine == ""PING"") {{
    Serial.println(""PONG|ESP32_TFT18|USB_ONLINE"");
    return;
  }}

  int firstPipe = cmdLine.indexOf('|');
  if (firstPipe == -1) return;

  String action = cmdLine.substring(0, firstPipe);
  String rest = cmdLine.substring(firstPipe + 1);

  if (action == ""QR"") {{
    // Cú pháp hỗ trợ linh hoạt:
    // 1) QR|chuoi_vietqr_emvco
    // 2) QR|HD000099|250.000 đ|chuoi_vietqr_emvco
    String qrData = rest;
    int lastPipe = rest.lastIndexOf('|');
    if (lastPipe != -1 && (rest.length() - lastPipe) > 20) {{
      qrData = rest.substring(lastPipe + 1);
    }}
    showScreenQR(qrData); // Full màn hình, không có chữ
    Serial.println(""ACK|QR_SHOWN"");
  }}
  else if (action == ""SUCCESS"") {{
    // Cú pháp: SUCCESS|HD000099|250.000 đ|MBBank
    int secondPipe = rest.indexOf('|');
    if (secondPipe != -1) {{
      String orderCode = rest.substring(0, secondPipe);
      String rest2 = rest.substring(secondPipe + 1);
      int thirdPipe = rest2.indexOf('|');
      String amount = (thirdPipe != -1) ? rest2.substring(0, thirdPipe) : rest2;
      showScreenSuccess(orderCode, amount);
      Serial.println(""ACK|SUCCESS_SHOWN|"" + orderCode);
    }}
  }}
  else if (action == ""IDLE"") {{
    // Cú pháp: IDLE|TenCuaHang|LoiChao
    int secondPipe = rest.indexOf('|');
    String store = (secondPipe != -1) ? rest.substring(0, secondPipe) : ""{storeName}"";
    String msg = (secondPipe != -1) ? rest.substring(secondPipe + 1) : ""Xin chao quy khach!"";
    showScreenIdle(store, msg);
    Serial.println("ACK|IDLE_SHOWN");
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
      text = text.substring(0, maxChars - 2) + "..";
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
  tft.fillScreen(ST77XX_BLACK);

  // Thanh tiêu đề màu xanh navy (0x0277)
  tft.fillRect(0, 0, 128, 28, 0x0277);
  tft.drawFastHLine(0, 28, 128, 0x041F);
  printCenter(store, 10, 1, ST77XX_WHITE, 150, 160);

  // Lời chào trung tâm to rõ (rotation=1: 160x128 ngang)
  printCenter("XIN CHAO!", 44, 2, ST77XX_CYAN, 150, 160);
  printCenter("Cam on quy khach!", 76, 1, ST77XX_WHITE, 150, 160);
  printCenter("Hen gap lai!", 90, 1, 0xCE79, 150, 160);

  // Dưới cùng: Báo kết nối USB ổn định
  tft.drawFastHLine(10, 110, 140, 0x39E7);
  printCenter("USB: 115200 BAUD", 116, 1, ST77XX_GREEN, 150, 160);
}}

// ===== GIAO DIỆN 2: MÃ VIETQR FULL MÀN HÌNH - KHÔNG CÓ CHỮ / LOGO =====
// Toàn bộ màn hình dành cho mã QR để khách dễ quét
void showScreenQR(String qrData) {{
  drawQRCodeFullScreen(qrData);
}}

// ================= GIAO DIỆN 3: BÁO THÀNH CÔNG (SUCCESS) =================
void showScreenSuccess(String orderCode, String amount) {{
  tft.fillScreen(0x04A0); // Nền xanh lá đậm dịu mắt

  // Thanh tiêu đề thông báo
  tft.fillRect(0, 0, 128, 32, 0x0360);
  printCenter("DA NHAN TIEN!", 10, 1, ST77XX_YELLOW, 120);

  // Hộp chi tiết hoá đơn
  tft.fillRoundRect(8, 42, 112, 58, 4, ST77XX_BLACK);
  tft.drawRoundRect(8, 42, 112, 58, 4, ST77XX_WHITE);

  printCenter(orderCode, 52, 1, ST77XX_CYAN, 106);
  printCenter(amount, 74, 1, ST77XX_YELLOW, 106);

  printCenter("Cam on quy khach!", 116, 1, ST77XX_WHITE, 120);
  printCenter("HEN GAP LAI", 134, 1, 0x07E0, 120);
}}

// Callback hiển thị mã QR lên màn hình TFT 1.8 ST7735 bằng thư viện ESP32 gốc (Native)
void drawQrCodeCallback(esp_qrcode_handle_t qrcode) {{
  int size = esp_qrcode_get_size(qrcode);
  if (size <= 0) return;

  // Tính scale tối đa: dùng tới 124px chiều ngang để lề trắng 2px mỗi bên
  int scale = 124 / size;
  if (scale < 1) scale = 1;

  int qrSize = size * scale;
  // Căn chính giữa màn hình 128x160
  int startX = (128 - qrSize) / 2;
  int startY = (160 - qrSize) / 2;

  // Nền trắng toàn màn hình
  tft.fillScreen(ST77XX_WHITE);

  // Vẽ từng module QR
  for (int y = 0; y < size; y++) {{
    for (int x = 0; x < size; x++) {{
      if (esp_qrcode_get_module(qrcode, x, y)) {{
        tft.fillRect(startX + (x * scale),
                     startY + (y * scale),
                     scale, scale, ST77XX_BLACK);
      }}
    }}
  }}
}}

// Duplicate drawQRCodeFullScreen implementation removed
void drawQRCodeFullScreen(String text) {{
  text.trim();
  if (text.length() == 0) return;

  esp_qrcode_config_t cfg = {{
    .display_func = drawQrCodeCallback,
    .max_qrcode_version = 10,
    .qrcode_ecc_level = ESP_QRCODE_ECC_LOW
  }};

  esp_err_t res = esp_qrcode_generate(&cfg, text.c_str());
  if (res != ESP_OK) {{
    text.toUpperCase();
    res = esp_qrcode_generate(&cfg, text.c_str());
  }}

  if (res != ESP_OK) {{
    tft.fillScreen(ST77XX_BLACK);
    tft.setTextColor(ST77XX_RED);
    tft.setTextSize(1);
    tft.setCursor(10, 70);
    tft.println(""LOI DU LIEU QR!"");
  }}
}}
";
    }
}
