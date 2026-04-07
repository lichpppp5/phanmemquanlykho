using Dapper;
using PMTapHoa.Desktop.Data;
using PMTapHoa.Desktop.Models;
using System.Drawing.Printing;

namespace PMTapHoa.Desktop.Services;

public class HealthCheckService
{
    private readonly DatabaseContext _databaseContext;
    private readonly AppConfigService _appConfigService;

    public HealthCheckService(DatabaseContext databaseContext, AppConfigService appConfigService)
    {
        _databaseContext = databaseContext;
        _appConfigService = appConfigService;
    }

    public List<HealthCheckItem> RunAll(UserAccount? currentUser)
    {
        var result = new List<HealthCheckItem>();
        CheckDatabaseFile(result);
        CheckDatabaseRead(result);
        CheckDatabaseWrite(result);
        CheckPrinterConfig(result);
        CheckUserSession(result, currentUser);
        return result;
    }

    private void CheckDatabaseFile(List<HealthCheckItem> list)
    {
        var exists = File.Exists(_databaseContext.DatabaseFilePath);
        var info = exists ? new FileInfo(_databaseContext.DatabaseFilePath) : null;
        list.Add(new HealthCheckItem
        {
            CheckName = "Database file",
            Status = exists ? "OK" : "ERROR",
            Message = exists
                ? $"Có file, dung lượng {info!.Length:N0} bytes."
                : "Không tìm thấy file SQLite."
        });
    }

    private void CheckDatabaseRead(List<HealthCheckItem> list)
    {
        try
        {
            using var connection = _databaseContext.CreateConnection();
            var tableCount = connection.ExecuteScalar<int>("SELECT COUNT(1) FROM sqlite_master WHERE type = 'table';");
            list.Add(new HealthCheckItem
            {
                CheckName = "Database read",
                Status = "OK",
                Message = $"Kết nối đọc thành công. Số bảng: {tableCount}."
            });
        }
        catch (Exception ex)
        {
            list.Add(new HealthCheckItem
            {
                CheckName = "Database read",
                Status = "ERROR",
                Message = ex.Message
            });
        }
    }

    private void CheckDatabaseWrite(List<HealthCheckItem> list)
    {
        try
        {
            _appConfigService.Set("healthcheck_last_run", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            list.Add(new HealthCheckItem
            {
                CheckName = "Database write",
                Status = "OK",
                Message = "Ghi thử dữ liệu cấu hình thành công."
            });
        }
        catch (Exception ex)
        {
            list.Add(new HealthCheckItem
            {
                CheckName = "Database write",
                Status = "ERROR",
                Message = ex.Message
            });
        }
    }

    private void CheckPrinterConfig(List<HealthCheckItem> list)
    {
        var configured = _appConfigService.DefaultPrinter;
        if (string.IsNullOrWhiteSpace(configured))
        {
            list.Add(new HealthCheckItem
            {
                CheckName = "Printer config",
                Status = "WARN",
                Message = "Chưa cấu hình máy in mặc định."
            });
            return;
        }

        var installed = PrinterSettings.InstalledPrinters.Cast<string>().Any(p => string.Equals(p, configured, StringComparison.OrdinalIgnoreCase));
        list.Add(new HealthCheckItem
        {
            CheckName = "Printer config",
            Status = installed ? "OK" : "WARN",
            Message = installed
                ? $"Máy in '{configured}' sẵn sàng."
                : $"Máy in '{configured}' không còn trong danh sách máy in."
        });
    }

    private static void CheckUserSession(List<HealthCheckItem> list, UserAccount? currentUser)
    {
        if (currentUser == null)
        {
            list.Add(new HealthCheckItem
            {
                CheckName = "User session",
                Status = "ERROR",
                Message = "Chưa có thông tin user đăng nhập."
            });
            return;
        }

        list.Add(new HealthCheckItem
        {
            CheckName = "User session",
            Status = "OK",
            Message = $"User={currentUser.Username}, Role={currentUser.Role}."
        });
    }
}
