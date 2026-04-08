using Dapper;
using PMTapHoa.Desktop.Data;
using PMTapHoa.Desktop.Models;
using System.Security.Cryptography;
using System.Text;

namespace PMTapHoa.Desktop.Services;

public class AuthService
{
    private readonly DatabaseContext _databaseContext;

    public AuthService(DatabaseContext databaseContext)
    {
        _databaseContext = databaseContext;
    }

    public void EnsureDefaultUsers()
    {
        using var connection = _databaseContext.CreateConnection();
        var count = connection.ExecuteScalar<int>("SELECT COUNT(1) FROM Users;");
        if (count > 0)
        {
            return;
        }

        connection.Execute(
            """
            INSERT INTO Users (Username, PasswordHash, FullName, Role, IsActive)
            VALUES (@Username, @PasswordHash, @FullName, @Role, 1);
            """,
            new[]
            {
                new
                {
                    Username = "admin",
                    PasswordHash = HashPassword("admin123"),
                    FullName = "Quản lý hệ thống",
                    Role = "Admin"
                },
                new
                {
                    Username = "staff",
                    PasswordHash = HashPassword("staff123"),
                    FullName = "Nhân viên bán hàng",
                    Role = "User"
                }
            });
    }

    public UserAccount? Authenticate(string username, string password)
    {
        var normalizedUser = (username ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedUser) || string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        using var connection = _databaseContext.CreateConnection();
        var user = connection.QueryFirstOrDefault<UserAccount>(
            """
            SELECT UserID, Username, PasswordHash, FullName, Role, IsActive
            FROM Users
            WHERE Username = @Username
              AND IsActive = 1
            LIMIT 1;
            """,
            new { Username = normalizedUser });

        if (user == null)
        {
            return null;
        }

        var hash = HashPassword(password);
        return string.Equals(hash, user.PasswordHash, StringComparison.OrdinalIgnoreCase)
            ? user
            : null;
    }

    public bool ChangePassword(int userId, string currentPassword, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
        {
            throw new InvalidOperationException("Mật khẩu mới phải tối thiểu 6 ký tự.");
        }

        using var connection = _databaseContext.CreateConnection();
        var user = connection.QueryFirstOrDefault<UserAccount>(
            """
            SELECT UserID, Username, PasswordHash, FullName, Role, IsActive
            FROM Users
            WHERE UserID = @UserID
            LIMIT 1;
            """,
            new { UserID = userId });

        if (user == null)
        {
            return false;
        }

        var currentHash = HashPassword(currentPassword);
        if (!string.Equals(currentHash, user.PasswordHash, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var newHash = HashPassword(newPassword);
        connection.Execute(
            """
            UPDATE Users
            SET PasswordHash = @PasswordHash
            WHERE UserID = @UserID;
            """,
            new { PasswordHash = newHash, UserID = userId });

        return true;
    }

    public static string HashPassword(string password)
    {
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
