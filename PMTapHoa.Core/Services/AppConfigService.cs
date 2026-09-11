using Dapper;
using PMTapHoa.Core.Data;

namespace PMTapHoa.Core.Services;

public class AppConfigService
{
    private readonly DatabaseContext _databaseContext;

    public AppConfigService(DatabaseContext databaseContext)
    {
        _databaseContext = databaseContext;
    }

    public string? Get(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        using var connection = _databaseContext.CreateConnection();
        return connection.ExecuteScalar<string?>(
            "SELECT SettingValue FROM AppSettings WHERE SettingKey = @SettingKey LIMIT 1;",
            new { SettingKey = key.Trim() });
    }

    public void Set(string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        using var connection = _databaseContext.CreateConnection();
        connection.Execute(
            """
            INSERT INTO AppSettings (SettingKey, SettingValue)
            VALUES (@SettingKey, @SettingValue)
            ON CONFLICT(SettingKey) DO UPDATE SET SettingValue = excluded.SettingValue;
            """,
            new
            {
                SettingKey = key.Trim(),
                SettingValue = value?.Trim()
            });
    }

    // ── Thông tin cửa hàng ──────────────────────────────────────────────────────
    public string StoreName
    {
        get => Get("store_name") ?? "Cửa Hàng Tạp Hóa";
        set => Set("store_name", value);
    }

    public string StoreAddress
    {
        get => Get("store_address") ?? string.Empty;
        set => Set("store_address", value);
    }

    public string StorePhone
    {
        get => Get("store_phone") ?? string.Empty;
        set => Set("store_phone", value);
    }

    public string ReceiptFooter
    {
        get => Get("receipt_footer") ?? "Cảm ơn quý khách! Hẹn gặp lại.";
        set => Set("receipt_footer", value);
    }

    // ── Máy in ──────────────────────────────────────────────────────────────────
    public string? DefaultPrinter
    {
        get => Get("default_printer");
        set => Set("default_printer", value);
    }

    public int DefaultPaperWidth
    {
        get
        {
            var value = Get("default_paper_width");
            return int.TryParse(value, out var parsed) && (parsed == 58 || parsed == 80) ? parsed : 58;
        }
        set => Set("default_paper_width", value == 80 ? "80" : "58");
    }

    public bool AutoPrintReceipt
    {
        get => string.Equals(Get("auto_print_receipt") ?? "1", "1", StringComparison.OrdinalIgnoreCase);
        set => Set("auto_print_receipt", value ? "1" : "0");
    }

    public string PrinterModel
    {
        get => Get("printer_model") ?? "Xprinter XP-N160M / Q200 (K80)";
        set => Set("printer_model", value);
    }

    public string PrinterConnectionType
    {
        get => Get("printer_conn_type") ?? "USB";
        set => Set("printer_conn_type", value);
    }

    public int PrintCopies
    {
        get
        {
            var value = Get("print_copies");
            return int.TryParse(value, out var c) && (c == 1 || c == 2) ? c : 1;
        }
        set => Set("print_copies", value == 2 ? "2" : "1");
    }

    public bool PrintQrOnReceipt
    {
        get => string.Equals(Get("print_qr_on_receipt") ?? "1", "1", StringComparison.OrdinalIgnoreCase);
        set => Set("print_qr_on_receipt", value ? "1" : "0");
    }

    public string ReceiptFontSize
    {
        get => Get("receipt_font_size") ?? "standard";
        set => Set("receipt_font_size", value);
    }

    // ── Cấu hình máy quét mã vạch / tay bấm QR ─────────────────────────────────
    public string ScannerType
    {
        get => Get("scanner_type") ?? "usb_handheld";
        set => Set("scanner_type", value);
    }

    public string ScannerModel
    {
        get => Get("scanner_model") ?? "Tay bấm QR / Barcode 2D phổ thông";
        set => Set("scanner_model", value);
    }

    public string ScannerSuffix
    {
        get => Get("scanner_suffix") ?? "enter";
        set => Set("scanner_suffix", value);
    }

    public bool ScannerBeepSound
    {
        get => !string.Equals(Get("scanner_beep_sound"), "0", StringComparison.OrdinalIgnoreCase);
        set => Set("scanner_beep_sound", value ? "1" : "0");
    }

    public bool ScannerAutoAdd
    {
        get => !string.Equals(Get("scanner_auto_add"), "0", StringComparison.OrdinalIgnoreCase);
        set => Set("scanner_auto_add", value ? "1" : "0");
    }

    // ── Cấu hình hoá đơn tổng A4 / A5 ──────────────────────────────────────────
    public string StoreTaxId
    {
        get => Get("store_tax_id") ?? string.Empty;
        set => Set("store_tax_id", value);
    }

    public string A4InvoiceTitle
    {
        get => Get("a4_invoice_title") ?? "HÓA ĐƠN BÁN HÀNG";
        set => Set("a4_invoice_title", value);
    }

    public string A4PaperSize
    {
        get => Get("a4_paper_size") ?? "A4";
        set => Set("a4_paper_size", value);
    }

    public string A4Orientation
    {
        get => Get("a4_orientation") ?? "portrait";
        set => Set("a4_orientation", value);
    }

    public bool A4ShowSignatures
    {
        get => !string.Equals(Get("a4_show_signatures"), "0", StringComparison.OrdinalIgnoreCase);
        set => Set("a4_show_signatures", value ? "1" : "0");
    }

    public bool A4ShowBankQr
    {
        get => !string.Equals(Get("a4_show_bank_qr"), "0", StringComparison.OrdinalIgnoreCase);
        set => Set("a4_show_bank_qr", value ? "1" : "0");
    }

    // ── Tự động nhận diện thanh toán VietQR (SePay / Casso / SMS Điện thoại) ────────
    /// <summary>
    /// Chế độ nhận diện thanh toán: "both" (Cả 2 - Khuyến nghị), "sepay" (Chỉ cổng SePay/Casso), "phone" (Chỉ SMS/Noti điện thoại)
    /// </summary>
    public string PaymentAutoDetectMode
    {
        get => Get("payment_auto_detect_mode") ?? "both";
        set => Set("payment_auto_detect_mode", value ?? "both");
    }

    /// <summary>
    /// Mã bí mật Webhook (API Key / Secret Token) bảo mật tuỳ chọn
    /// </summary>
    public string PaymentWebhookSecret
    {
        get => Get("payment_webhook_secret") ?? "";
        set => Set("payment_webhook_secret", value ?? "");
    }

    public bool PaymentSoundEnabled
    {
        get => !string.Equals(Get("payment_sound_enabled"), "0", StringComparison.OrdinalIgnoreCase);
        set => Set("payment_sound_enabled", value ? "1" : "0");
    }

    // ── Tồn kho & Hạn sử dụng ───────────────────────────────────────────────────
    public bool LowStockReminderEnabled
    {
        get
        {
            var value = Get("low_stock_reminder_enabled");
            return !string.Equals(value, "0", StringComparison.OrdinalIgnoreCase);
        }
        set => Set("low_stock_reminder_enabled", value ? "1" : "0");
    }

    /// <summary>Số ngày trước hết hạn để cảnh báo (mặc định 14 ngày).</summary>
    public int NearExpiryWarningDays
    {
        get
        {
            var value = Get("near_expiry_days");
            return int.TryParse(value, out var d) && d > 0 ? d : 14;
        }
        set => Set("near_expiry_days", value.ToString());
    }

    // ── Thanh toán QR ───────────────────────────────────────────────────────────
    public bool QrPaymentEnabled
    {
        get
        {
            var value = Get("qr_payment_enabled");
            return !string.Equals(value, "0", StringComparison.OrdinalIgnoreCase);
        }
        set => Set("qr_payment_enabled", value ? "1" : "0");
    }

    public string QrBankBin
    {
        get => Get("qr_bank_bin") ?? "970436";
        set => Set("qr_bank_bin", value);
    }

    public string QrAccountNo
    {
        get => Get("qr_account_no") ?? string.Empty;
        set => Set("qr_account_no", value);
    }

    public string QrAccountName
    {
        get => Get("qr_account_name") ?? "CUA HANG TAP HOA";
        set => Set("qr_account_name", value);
    }

    public string QrTransferPrefix
    {
        get => Get("qr_transfer_prefix") ?? "HD";
        set => Set("qr_transfer_prefix", value);
    }

    // ── Hiển thị ────────────────────────────────────────────────────────────────
    public string DisplayMode
    {
        get
        {
            var value = Get("display_mode");
            return string.Equals(value, "fullscreen", StringComparison.OrdinalIgnoreCase)
                ? "fullscreen"
                : "standard";
        }
        set => Set(
            "display_mode",
            string.Equals(value, "fullscreen", StringComparison.OrdinalIgnoreCase) ? "fullscreen" : "standard");
    }

    public bool DisplayFullScreen
    {
        get => string.Equals(DisplayMode, "fullscreen", StringComparison.OrdinalIgnoreCase);
        set => DisplayMode = value ? "fullscreen" : "standard";
    }

    // ── Thư mục lưu trữ hoá đơn theo ngày (HoaDon/Ban & HoaDon/Nhap) ────────────
    public string InvoiceStoragePath
    {
        get => Get("invoice_storage_path") ?? string.Empty;
        set => Set("invoice_storage_path", value);
    }

    public static string GetDefaultInvoiceStoragePath()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrWhiteSpace(userProfile))
        {
            userProfile = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }
        return Path.Combine(userProfile, "HoaDon");
    }

    public bool IsInvoiceFolderConfigured()
    {
        var path = InvoiceStoragePath;
        if (string.IsNullOrWhiteSpace(path)) return false;
        try
        {
            if (!Directory.Exists(path)) return false;
            var banPath = Path.Combine(path, "Ban");
            var nhapPath = Path.Combine(path, "Nhap");
            return Directory.Exists(banPath) && Directory.Exists(nhapPath);
        }
        catch
        {
            return false;
        }
    }

    public string SetupInvoiceFolder(string? customPath = null)
    {
        var targetPath = string.IsNullOrWhiteSpace(customPath) 
            ? (string.IsNullOrWhiteSpace(InvoiceStoragePath) ? GetDefaultInvoiceStoragePath() : InvoiceStoragePath)
            : customPath.Trim();

        var banPath = Path.Combine(targetPath, "Ban");
        var nhapPath = Path.Combine(targetPath, "Nhap");

        Directory.CreateDirectory(targetPath);
        Directory.CreateDirectory(banPath);
        Directory.CreateDirectory(nhapPath);

        InvoiceStoragePath = targetPath;
        return targetPath;
    }

    // ── Cấu hình Màn hình phụ ESP32 TFT 1.8 ──────────────────────────────────────
    public string Esp32ConnMode
    {
        get => Get("esp32_conn_mode") ?? "usb";
        set => Set("esp32_conn_mode", value ?? "usb");
    }

    public string Esp32ServerIp
    {
        get => Get("esp32_server_ip") ?? string.Empty;
        set => Set("esp32_server_ip", value);
    }

    public string Esp32WifiSsid
    {
        get => Get("esp32_wifi_ssid") ?? "WiFi_CuaHang";
        set => Set("esp32_wifi_ssid", value);
    }

    public string Esp32WifiPass
    {
        get => Get("esp32_wifi_pass") ?? "12345678";
        set => Set("esp32_wifi_pass", value);
    }
}

