using Dapper;
using PMTapHoa.Desktop.Data;

namespace PMTapHoa.Desktop.Services;

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
}
