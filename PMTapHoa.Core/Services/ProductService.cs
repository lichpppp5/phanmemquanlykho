using Dapper;
using PMTapHoa.Core.Data;
using PMTapHoa.Core.Models;
using System.Data;

namespace PMTapHoa.Core.Services;

public class ProductService
{
    private readonly DatabaseContext _databaseContext;

    public ProductService(DatabaseContext databaseContext)
    {
        _databaseContext = databaseContext;
    }

    public Product? GetByBarcode(string barcode)
    {
        const string sql = """
                           SELECT p.ProductID, p.Barcode, p.ProductName, p.CategoryID, c.CategoryName,
                                  p.Unit, p.CostPrice, p.SellingPrice, p.StockQuantity, p.MinStock, p.ExpiryDate, p.SupplierID, p.ImageUrl
                           FROM Products p
                           LEFT JOIN Categories c ON p.CategoryID = c.CategoryID
                           WHERE Barcode = @Barcode
                           LIMIT 1;
                           """;
        using var connection = _databaseContext.CreateConnection();
        return connection.QueryFirstOrDefault<Product>(sql, new { Barcode = barcode });
    }

    public Product? GetById(int productId)
    {
        const string sql = """
                           SELECT p.ProductID, p.Barcode, p.ProductName, p.CategoryID, c.CategoryName,
                                  p.Unit, p.CostPrice, p.SellingPrice, p.StockQuantity, p.MinStock, p.ExpiryDate, p.SupplierID, p.ImageUrl
                           FROM Products p
                           LEFT JOIN Categories c ON p.CategoryID = c.CategoryID
                           WHERE p.ProductID = @ProductID
                           LIMIT 1;
                           """;
        using var connection = _databaseContext.CreateConnection();
        return connection.QueryFirstOrDefault<Product>(sql, new { ProductID = productId });
    }

    public List<Product> Search(string keyword)
    {
        const string sql = """
                           SELECT p.ProductID, p.Barcode, p.ProductName, p.CategoryID, c.CategoryName,
                                  p.Unit, p.CostPrice, p.SellingPrice, p.StockQuantity, p.MinStock, p.ExpiryDate, p.SupplierID, p.ImageUrl
                           FROM Products p
                           LEFT JOIN Categories c ON p.CategoryID = c.CategoryID
                           WHERE p.ProductName LIKE @Keyword OR p.Barcode LIKE @Keyword
                           ORDER BY p.ProductName;
                           """;
        using var connection = _databaseContext.CreateConnection();
        return connection.Query<Product>(sql, new { Keyword = $"%{keyword}%" }).ToList();
    }

    public List<Product> GetAll()
    {
        const string sql = """
                           SELECT p.ProductID, p.Barcode, p.ProductName, p.CategoryID, c.CategoryName,
                                  p.Unit, p.CostPrice, p.SellingPrice, p.StockQuantity, p.MinStock, p.ExpiryDate, p.SupplierID, p.ImageUrl
                           FROM Products p
                           LEFT JOIN Categories c ON p.CategoryID = c.CategoryID
                           ORDER BY p.ProductName;
                           """;
        using var connection = _databaseContext.CreateConnection();
        return connection.Query<Product>(sql).ToList();
    }

    public List<Product> GetByCategory(int? categoryId)
    {
        if (categoryId == null) return GetAll();
        const string sql = """
                           SELECT p.ProductID, p.Barcode, p.ProductName, p.CategoryID, c.CategoryName,
                                  p.Unit, p.CostPrice, p.SellingPrice, p.StockQuantity, p.MinStock, p.ExpiryDate, p.SupplierID, p.ImageUrl
                           FROM Products p
                           LEFT JOIN Categories c ON p.CategoryID = c.CategoryID
                           WHERE p.CategoryID = @CategoryID
                           ORDER BY p.ProductName;
                           """;
        using var connection = _databaseContext.CreateConnection();
        return connection.Query<Product>(sql, new { CategoryID = categoryId }).ToList();
    }

    public int CreateProduct(Product product, string? categoryName)
    {
        ValidateProduct(product);
        using var connection = _databaseContext.CreateConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            var categoryId = EnsureCategory(connection, transaction, categoryName);
            var productId = connection.ExecuteScalar<int>(
                """
                INSERT INTO Products (Barcode, ProductName, CategoryID, Unit, CostPrice, SellingPrice, StockQuantity, MinStock, ExpiryDate, SupplierID, ImageUrl)
                VALUES (@Barcode, @ProductName, @CategoryID, @Unit, @CostPrice, @SellingPrice, @StockQuantity, @MinStock, @ExpiryDate, @SupplierID, @ImageUrl);
                SELECT last_insert_rowid();
                """,
                new
                {
                    product.Barcode,
                    product.ProductName,
                    CategoryID = categoryId,
                    product.Unit,
                    product.CostPrice,
                    product.SellingPrice,
                    product.StockQuantity,
                    product.MinStock,
                    product.ExpiryDate,
                    product.SupplierID,
                    product.ImageUrl
                },
                transaction);

            transaction.Commit();
            return productId;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public void UpdateProduct(Product product, string? categoryName)
    {
        ValidateProduct(product);
        using var connection = _databaseContext.CreateConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            var categoryId = EnsureCategory(connection, transaction, categoryName);
            connection.Execute(
                """
                UPDATE Products
                SET Barcode = @Barcode,
                    ProductName = @ProductName,
                    CategoryID = @CategoryID,
                    Unit = @Unit,
                    CostPrice = @CostPrice,
                    SellingPrice = @SellingPrice,
                    StockQuantity = @StockQuantity,
                    MinStock = @MinStock,
                    ExpiryDate = @ExpiryDate,
                    SupplierID = @SupplierID,
                    ImageUrl = @ImageUrl
                WHERE ProductID = @ProductID;
                """,
                new
                {
                    product.ProductID,
                    product.Barcode,
                    product.ProductName,
                    CategoryID = categoryId,
                    product.Unit,
                    product.CostPrice,
                    product.SellingPrice,
                    product.StockQuantity,
                    product.MinStock,
                    product.ExpiryDate,
                    product.SupplierID,
                    product.ImageUrl
                },
                transaction);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public void DeleteProduct(int productId)
    {
        using var connection = _databaseContext.CreateConnection();
        connection.Execute("DELETE FROM Products WHERE ProductID = @ProductID;", new { ProductID = productId });
    }

    /// <summary>Nhập hàng nhanh - cập nhật tồn kho và ghi lịch sử nhập.</summary>
    public int QuickImportStock(int productId, double quantityToAdd, decimal? newCostPrice, string? importedBy = null, string? note = null, int? supplierId = null)
    {
        using var connection = _databaseContext.CreateConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            connection.Execute(
                """
                UPDATE Products
                SET StockQuantity = StockQuantity + @QuantityToAdd,
                    CostPrice = COALESCE(@NewCostPrice, CostPrice)
                WHERE ProductID = @ProductID;
                """,
                new { ProductID = productId, QuantityToAdd = quantityToAdd, NewCostPrice = newCostPrice },
                transaction);

            // Lấy CostPrice hiện tại nếu không truyền vào
            var costPrice = newCostPrice;
            if (costPrice == null)
            {
                costPrice = connection.ExecuteScalar<decimal?>(
                    "SELECT CostPrice FROM Products WHERE ProductID = @ProductID;",
                    new { ProductID = productId }, transaction);
            }

            var importId = connection.ExecuteScalar<int>(
                """
                INSERT INTO StockImportHistory (ProductID, SupplierID, Quantity, CostPrice, ImportedBy, Note)
                VALUES (@ProductID, @SupplierID, @Quantity, @CostPrice, @ImportedBy, @Note);
                SELECT last_insert_rowid();
                """,
                new
                {
                    ProductID = productId,
                    SupplierID = supplierId,
                    Quantity = quantityToAdd,
                    CostPrice = costPrice,
                    ImportedBy = importedBy,
                    Note = note
                },
                transaction);

            transaction.Commit();
            return importId;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public List<StockImportHistoryItem> GetImportHistory(int? productId = null, int limit = 200)
    {
        const string sql = """
                           SELECT h.ImportID, h.ProductID, p.ProductName,
                                  s.SupplierName, h.Quantity, h.CostPrice,
                                  h.ImportDate, h.ImportedBy, h.Note
                           FROM StockImportHistory h
                           LEFT JOIN Products p ON p.ProductID = h.ProductID
                           LEFT JOIN Suppliers s ON s.SupplierID = h.SupplierID
                           WHERE (@ProductID IS NULL OR h.ProductID = @ProductID)
                           ORDER BY h.ImportDate DESC
                           LIMIT @Limit;
                           """;
        using var connection = _databaseContext.CreateConnection();
        return connection.Query<StockImportHistoryItem>(sql, new { ProductID = productId, Limit = limit }).ToList();
    }

    private static int? EnsureCategory(IDbConnection connection, IDbTransaction transaction, string? categoryName)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
        {
            return null;
        }

        var trimmed = categoryName.Trim();
        var existingId = connection.ExecuteScalar<int?>(
            "SELECT CategoryID FROM Categories WHERE CategoryName = @CategoryName LIMIT 1;",
            new { CategoryName = trimmed },
            transaction);

        if (existingId.HasValue)
        {
            return existingId.Value;
        }

        return connection.ExecuteScalar<int>(
            """
            INSERT INTO Categories (CategoryName)
            VALUES (@CategoryName);
            SELECT last_insert_rowid();
            """,
            new { CategoryName = trimmed },
            transaction);
    }

    private static void ValidateProduct(Product product)
    {
        if (string.IsNullOrWhiteSpace(product.ProductName))
        {
            throw new InvalidOperationException("Tên sản phẩm không được để trống.");
        }

        if (product.SellingPrice < 0 || product.CostPrice < 0)
        {
            throw new InvalidOperationException("Giá nhập/giá bán không hợp lệ.");
        }

        if (product.MinStock < 0 || product.StockQuantity < 0)
        {
            throw new InvalidOperationException("Tồn kho hoặc mức tối thiểu không hợp lệ.");
        }
    }
}
