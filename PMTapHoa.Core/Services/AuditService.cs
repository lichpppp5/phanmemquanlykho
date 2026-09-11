using Dapper;
using PMTapHoa.Core.Data;
using PMTapHoa.Core.Models;

namespace PMTapHoa.Core.Services;

public class AuditService
{
    private readonly DatabaseContext _databaseContext;

    public AuditService(DatabaseContext databaseContext)
    {
        _databaseContext = databaseContext;
    }

    public void Log(string? username, string action, string? detail)
    {
        if (string.IsNullOrWhiteSpace(action))
        {
            return;
        }

        try
        {
            using var connection = _databaseContext.CreateConnection();
            connection.Execute(
                """
                INSERT INTO AuditLogs (LogTime, Username, Action, Detail)
                VALUES (CURRENT_TIMESTAMP, @Username, @Action, @Detail);
                """,
                new
                {
                    Username = string.IsNullOrWhiteSpace(username) ? "system" : username.Trim(),
                    Action = action.Trim(),
                    Detail = detail?.Trim()
                });
        }
        catch
        {
            // Không làm gián đoạn luồng chính nếu ghi log lỗi.
        }
    }

    public List<AuditLog> GetRecent(int limit = 200)
    {
        var safeLimit = limit <= 0 ? 200 : Math.Min(limit, 1000);
        using var connection = _databaseContext.CreateConnection();
        return connection.Query<AuditLog>(
            """
            SELECT LogID, LogTime, Username, Action, Detail
            FROM AuditLogs
            ORDER BY LogTime DESC
            LIMIT @Limit;
            """,
            new { Limit = safeLimit }).ToList();
    }
}
