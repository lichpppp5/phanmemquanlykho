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

    public bool LowStockReminderEnabled
    {
        get
        {
            var value = Get("low_stock_reminder_enabled");
            return !string.Equals(value, "0", StringComparison.OrdinalIgnoreCase);
        }
        set => Set("low_stock_reminder_enabled", value ? "1" : "0");
    }
}
