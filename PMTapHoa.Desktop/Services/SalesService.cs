using Dapper;
using PMTapHoa.Desktop.Data;
using PMTapHoa.Desktop.Models;
using System.Data;

namespace PMTapHoa.Desktop.Services;

public class SalesService
{
    private readonly DatabaseContext _databaseContext;

    public SalesService(DatabaseContext databaseContext)
    {
        _databaseContext = databaseContext;
    }

    public int SaveSale(List<CartItem> cartItems, string? customerName, bool isDebt)
    {
        if (cartItems.Count == 0)
        {
            throw new InvalidOperationException("Giỏ hàng đang trống.");
        }

        var total = cartItems.Sum(i => i.LineTotal);

        using var connection = _databaseContext.CreateConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            var saleId = connection.ExecuteScalar<int>(
                """
                INSERT INTO Sales (SaleDate, TotalAmount, CustomerName, IsDebt)
                VALUES (CURRENT_TIMESTAMP, @TotalAmount, @CustomerName, @IsDebt);
                SELECT last_insert_rowid();
                """,
                new { TotalAmount = total, CustomerName = customerName, IsDebt = isDebt ? 1 : 0 },
                transaction);

            foreach (var item in cartItems)
            {
                InsertSaleDetail(connection, transaction, saleId, item);
                DecreaseStock(connection, transaction, item.ProductID, item.Quantity);
            }

            transaction.Commit();
            return saleId;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public decimal GetTodayRevenue()
    {
        const string sql = """
                           SELECT COALESCE(SUM(TotalAmount), 0)
                           FROM Sales
                           WHERE date(SaleDate) = date('now', 'localtime');
                           """;
        using var connection = _databaseContext.CreateConnection();
        return connection.ExecuteScalar<decimal>(sql);
    }

    public decimal GetCurrentMonthRevenue()
    {
        const string sql = """
                           SELECT COALESCE(SUM(TotalAmount), 0)
                           FROM Sales
                           WHERE strftime('%Y-%m', SaleDate) = strftime('%Y-%m', 'now', 'localtime');
                           """;
        using var connection = _databaseContext.CreateConnection();
        return connection.ExecuteScalar<decimal>(sql);
    }

    public int GetTodaySaleCount()
    {
        const string sql = """
                           SELECT COUNT(1)
                           FROM Sales
                           WHERE date(SaleDate) = date('now', 'localtime');
                           """;
        using var connection = _databaseContext.CreateConnection();
        return connection.ExecuteScalar<int>(sql);
    }

    public SalesProfitSummary GetTodayProfitSummary()
    {
        const string sql = """
                           SELECT
                               COALESCE(SUM(sd.Quantity * sd.UnitPrice), 0) AS RevenueAmount,
                               COALESCE(SUM(sd.Quantity * COALESCE(p.CostPrice, 0)), 0) AS CapitalAmount
                           FROM Sales s
                           INNER JOIN SaleDetails sd ON sd.SaleID = s.SaleID
                           LEFT JOIN Products p ON p.ProductID = sd.ProductID
                           WHERE date(s.SaleDate) = date('now', 'localtime');
                           """;
        using var connection = _databaseContext.CreateConnection();
        return connection.QueryFirst<SalesProfitSummary>(sql);
    }

    public SalesProfitSummary GetCurrentMonthProfitSummary()
    {
        const string sql = """
                           SELECT
                               COALESCE(SUM(sd.Quantity * sd.UnitPrice), 0) AS RevenueAmount,
                               COALESCE(SUM(sd.Quantity * COALESCE(p.CostPrice, 0)), 0) AS CapitalAmount
                           FROM Sales s
                           INNER JOIN SaleDetails sd ON sd.SaleID = s.SaleID
                           LEFT JOIN Products p ON p.ProductID = sd.ProductID
                           WHERE strftime('%Y-%m', s.SaleDate) = strftime('%Y-%m', 'now', 'localtime');
                           """;
        using var connection = _databaseContext.CreateConnection();
        return connection.QueryFirst<SalesProfitSummary>(sql);
    }

    public List<SaleHistoryItem> GetSales(string? keyword, DateTime? fromDate, DateTime? toDate)
    {
        const string sql = """
                           SELECT SaleID, SaleDate, TotalAmount, CustomerName, IsDebt
                           FROM Sales
                           WHERE (@Keyword IS NULL
                                  OR CustomerName LIKE @LikeKeyword
                                  OR CAST(SaleID AS TEXT) LIKE @LikeKeyword)
                             AND (@FromDate IS NULL OR datetime(SaleDate) >= datetime(@FromDate))
                             AND (@ToDate IS NULL OR datetime(SaleDate) <= datetime(@ToDate))
                           ORDER BY SaleDate DESC;
                           """;

        var trimmed = string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim();
        using var connection = _databaseContext.CreateConnection();
        return connection.Query<SaleHistoryItem>(
            sql,
            new
            {
                Keyword = trimmed,
                LikeKeyword = $"%{trimmed}%",
                FromDate = fromDate,
                ToDate = toDate
            }).ToList();
    }

    public Sale? GetSaleById(int saleId)
    {
        const string sql = """
                           SELECT SaleID, SaleDate, TotalAmount, CustomerName, IsDebt
                           FROM Sales
                           WHERE SaleID = @SaleID
                           LIMIT 1;
                           """;
        using var connection = _databaseContext.CreateConnection();
        return connection.QueryFirstOrDefault<Sale>(sql, new { SaleID = saleId });
    }

    public List<CartItem> GetSaleDetailsForReceipt(int saleId)
    {
        const string sql = """
                           SELECT sd.ProductID,
                                  COALESCE(p.Barcode, '') AS Barcode,
                                  COALESCE(p.ProductName, '[Sản phẩm đã xóa]') AS ProductName,
                                  sd.Quantity,
                                  sd.UnitPrice
                           FROM SaleDetails sd
                           LEFT JOIN Products p ON p.ProductID = sd.ProductID
                           WHERE sd.SaleID = @SaleID
                           ORDER BY sd.DetailID;
                           """;
        using var connection = _databaseContext.CreateConnection();
        return connection.Query<CartItem>(sql, new { SaleID = saleId }).ToList();
    }

    private static void InsertSaleDetail(IDbConnection connection, IDbTransaction transaction, int saleId, CartItem item)
    {
        connection.Execute(
            """
            INSERT INTO SaleDetails (SaleID, ProductID, Quantity, UnitPrice)
            VALUES (@SaleID, @ProductID, @Quantity, @UnitPrice);
            """,
            new
            {
                SaleID = saleId,
                item.ProductID,
                item.Quantity,
                item.UnitPrice
            },
            transaction);
    }

    private static void DecreaseStock(IDbConnection connection, IDbTransaction transaction, int productId, int quantity)
    {
        var rows = connection.Execute(
            """
            UPDATE Products
            SET StockQuantity = StockQuantity - @Quantity
            WHERE ProductID = @ProductID
              AND StockQuantity >= @Quantity;
            """,
            new { ProductID = productId, Quantity = quantity },
            transaction);

        if (rows == 0)
        {
            throw new InvalidOperationException($"Không đủ tồn kho cho ProductID={productId}.");
        }
    }
}
