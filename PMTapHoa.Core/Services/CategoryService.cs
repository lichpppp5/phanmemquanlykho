using Dapper;
using PMTapHoa.Core.Data;
using PMTapHoa.Core.Models;

namespace PMTapHoa.Core.Services;

public class CategoryService
{
    private readonly DatabaseContext _databaseContext;

    public CategoryService(DatabaseContext databaseContext)
    {
        _databaseContext = databaseContext;
    }

    public List<Category> GetAll()
    {
        const string sql = """
                           SELECT CategoryID, CategoryName
                           FROM Categories
                           ORDER BY CategoryName;
                           """;
        using var connection = _databaseContext.CreateConnection();
        return connection.Query<Category>(sql).ToList();
    }

    public int Create(string categoryName)
    {
        var trimmed = Validate(categoryName);
        using var connection = _databaseContext.CreateConnection();
        var exists = connection.ExecuteScalar<int>(
            "SELECT COUNT(1) FROM Categories WHERE CategoryName = @CategoryName;",
            new { CategoryName = trimmed });
        if (exists > 0)
        {
            throw new InvalidOperationException("Danh mục đã tồn tại.");
        }

        return connection.ExecuteScalar<int>(
            """
            INSERT INTO Categories (CategoryName)
            VALUES (@CategoryName);
            SELECT last_insert_rowid();
            """,
            new { CategoryName = trimmed });
    }

    public void Update(int categoryId, string categoryName)
    {
        var trimmed = Validate(categoryName);
        using var connection = _databaseContext.CreateConnection();
        connection.Execute(
            """
            UPDATE Categories
            SET CategoryName = @CategoryName
            WHERE CategoryID = @CategoryID;
            """,
            new { CategoryID = categoryId, CategoryName = trimmed });
    }

    public void Delete(int categoryId)
    {
        using var connection = _databaseContext.CreateConnection();
        var usedCount = connection.ExecuteScalar<int>(
            "SELECT COUNT(1) FROM Products WHERE CategoryID = @CategoryID;",
            new { CategoryID = categoryId });
        if (usedCount > 0)
        {
            throw new InvalidOperationException("Danh mục đang được dùng bởi sản phẩm, không thể xóa.");
        }

        connection.Execute("DELETE FROM Categories WHERE CategoryID = @CategoryID;", new { CategoryID = categoryId });
    }

    private static string Validate(string categoryName)
    {
        var trimmed = categoryName?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new InvalidOperationException("Tên danh mục không được để trống.");
        }

        return trimmed;
    }
}
