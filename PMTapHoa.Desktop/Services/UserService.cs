using Dapper;
using PMTapHoa.Desktop.Data;
using PMTapHoa.Desktop.Models;

namespace PMTapHoa.Desktop.Services;

public class UserService
{
    private readonly DatabaseContext _databaseContext;

    public UserService(DatabaseContext databaseContext)
    {
        _databaseContext = databaseContext;
    }

    public List<UserAccount> GetAll()
    {
        using var connection = _databaseContext.CreateConnection();
        return connection.Query<UserAccount>(
            """
            SELECT UserID, Username, PasswordHash, FullName, Role, IsActive
            FROM Users
            ORDER BY Username;
            """).ToList();
    }

    public int Create(string username, string fullName, string role, string password)
    {
        var normalizedRole = NormalizeRole(role);
        var user = (username ?? string.Empty).Trim();
        var name = (fullName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Tài khoản và họ tên không được để trống.");
        }

        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
        {
            throw new InvalidOperationException("Mật khẩu phải tối thiểu 6 ký tự.");
        }

        using var connection = _databaseContext.CreateConnection();
        var exists = connection.ExecuteScalar<int>("SELECT COUNT(1) FROM Users WHERE Username = @Username;", new { Username = user });
        if (exists > 0)
        {
            throw new InvalidOperationException("Tài khoản đã tồn tại.");
        }

        return connection.ExecuteScalar<int>(
            """
            INSERT INTO Users (Username, PasswordHash, FullName, Role, IsActive)
            VALUES (@Username, @PasswordHash, @FullName, @Role, 1);
            SELECT last_insert_rowid();
            """,
            new
            {
                Username = user,
                PasswordHash = AuthService.HashPassword(password),
                FullName = name,
                Role = normalizedRole
            });
    }

    public void UpdateRoleAndStatus(int userId, string role, bool isActive)
    {
        var normalizedRole = NormalizeRole(role);
        using var connection = _databaseContext.CreateConnection();
        connection.Execute(
            """
            UPDATE Users
            SET Role = @Role,
                IsActive = @IsActive
            WHERE UserID = @UserID;
            """,
            new
            {
                UserID = userId,
                Role = normalizedRole,
                IsActive = isActive ? 1 : 0
            });
    }

    public void ResetPassword(int userId, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
        {
            throw new InvalidOperationException("Mật khẩu mới phải tối thiểu 6 ký tự.");
        }

        using var connection = _databaseContext.CreateConnection();
        connection.Execute(
            """
            UPDATE Users
            SET PasswordHash = @PasswordHash
            WHERE UserID = @UserID;
            """,
            new
            {
                UserID = userId,
                PasswordHash = AuthService.HashPassword(newPassword)
            });
    }

    private static string NormalizeRole(string role)
    {
        var normalized = (role ?? string.Empty).Trim();
        return string.Equals(normalized, "Admin", StringComparison.OrdinalIgnoreCase)
               || string.Equals(normalized, "Manager", StringComparison.OrdinalIgnoreCase)
            ? "Admin"
            : "User";
    }
}
