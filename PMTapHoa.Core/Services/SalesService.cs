using Dapper;
using PMTapHoa.Core.Data;
using PMTapHoa.Core.Models;
using System.Data;

namespace PMTapHoa.Core.Services;

public class SalesService
{
    private readonly DatabaseContext _databaseContext;

    public SalesService(DatabaseContext databaseContext)
    {
        _databaseContext = databaseContext;
    }

    public int SaveSale(List<CartItem> cartItems, string? customerName, bool isDebt,
        decimal discountAmount = 0, string? discountNote = null, int? customerId = null)
    {
        if (cartItems.Count == 0)
        {
            throw new InvalidOperationException("Giỏ hàng đang trống.");
        }

        var subtotal = cartItems.Sum(i => i.LineTotal);
        var total = Math.Max(0, subtotal - discountAmount);

        using var connection = _databaseContext.CreateConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            var saleId = connection.ExecuteScalar<int>(
                """
                INSERT INTO Sales (SaleDate, TotalAmount, DiscountAmount, DiscountNote, CustomerName, CustomerID, IsDebt)
                VALUES (CURRENT_TIMESTAMP, @TotalAmount, @DiscountAmount, @DiscountNote, @CustomerName, @CustomerID, @IsDebt);
                SELECT last_insert_rowid();
                """,
                new
                {
                    TotalAmount = total,
                    DiscountAmount = discountAmount,
                    DiscountNote = string.IsNullOrWhiteSpace(discountNote) ? null : discountNote,
                    CustomerName = customerName,
                    CustomerID = customerId,
                    IsDebt = isDebt ? 1 : 0
                },
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

    /// <summary>Doanh thu 7 ngày gần nhất (mỗi ngày 1 dòng, bao gồm ngày không có doanh thu = 0).</summary>
    public List<(DateTime Date, decimal Revenue)> GetLast7DaysRevenue()
    {
        const string sql = """
                           SELECT date(SaleDate, 'localtime') AS SaleDay,
                                  COALESCE(SUM(TotalAmount), 0) AS Revenue
                           FROM Sales
                           WHERE date(SaleDate, 'localtime') >= date('now', 'localtime', '-6 days')
                           GROUP BY date(SaleDate, 'localtime')
                           ORDER BY SaleDay ASC;
                           """;
        using var connection = _databaseContext.CreateConnection();
        var rows = connection.Query<(string SaleDay, decimal Revenue)>(sql).ToList();

        // Đảm bảo đủ 7 ngày, ngày không có doanh thu = 0
        var result = new List<(DateTime Date, decimal Revenue)>();
        for (var i = 6; i >= 0; i--)
        {
            var day = DateTime.Today.AddDays(-i);
            var dayStr = day.ToString("yyyy-MM-dd");
            var found = rows.FirstOrDefault(r => r.SaleDay == dayStr);
            result.Add((day, found.Revenue));
        }
        return result;
    }

    /// <summary>Top N sản phẩm bán chạy hôm nay.</summary>
    public List<(string ProductName, int TotalQty, decimal TotalRevenue)> GetTopSellingToday(int top = 5)
    {
        const string sql = """
                           SELECT p.ProductName,
                                  CAST(SUM(sd.Quantity) AS INTEGER) AS TotalQty,
                                  SUM(sd.Quantity * sd.UnitPrice) AS TotalRevenue
                           FROM SaleDetails sd
                           INNER JOIN Sales s ON s.SaleID = sd.SaleID
                           LEFT JOIN Products p ON p.ProductID = sd.ProductID
                           WHERE date(s.SaleDate, 'localtime') = date('now', 'localtime')
                           GROUP BY sd.ProductID
                           ORDER BY TotalQty DESC
                           LIMIT @Top;
                           """;
        using var connection = _databaseContext.CreateConnection();
        return connection.Query<(string ProductName, int TotalQty, decimal TotalRevenue)>(
            sql, new { Top = top }).ToList();
    }

    /// <summary>Top N sản phẩm bán chạy theo kỳ.</summary>
    public List<(string ProductName, int TotalQty, decimal TotalRevenue)> GetTopSelling(
        DateTime? from, DateTime? to, int top = 10)
    {
        const string sql = """
                           SELECT p.ProductName,
                                  CAST(SUM(sd.Quantity) AS INTEGER) AS TotalQty,
                                  SUM(sd.Quantity * sd.UnitPrice) AS TotalRevenue
                           FROM SaleDetails sd
                           INNER JOIN Sales s ON s.SaleID = sd.SaleID
                           LEFT JOIN Products p ON p.ProductID = sd.ProductID
                           WHERE (@FromDate IS NULL OR datetime(s.SaleDate) >= datetime(@FromDate))
                             AND (@ToDate IS NULL OR datetime(s.SaleDate) <= datetime(@ToDate))
                           GROUP BY sd.ProductID
                           ORDER BY TotalQty DESC
                           LIMIT @Top;
                           """;
        using var connection = _databaseContext.CreateConnection();
        return connection.Query<(string ProductName, int TotalQty, decimal TotalRevenue)>(
            sql, new { FromDate = from, ToDate = to, Top = top }).ToList();
    }

    /// <summary>Báo cáo lợi nhuận theo kỳ.</summary>
    public (decimal Revenue, decimal Capital, decimal Profit) GetProfitReport(DateTime? from, DateTime? to)
    {
        const string sql = """
                           SELECT
                               COALESCE(SUM(sd.Quantity * sd.UnitPrice), 0) AS Revenue,
                               COALESCE(SUM(sd.Quantity * COALESCE(p.CostPrice, 0)), 0) AS Capital
                           FROM Sales s
                           INNER JOIN SaleDetails sd ON sd.SaleID = s.SaleID
                           LEFT JOIN Products p ON p.ProductID = sd.ProductID
                           WHERE (@FromDate IS NULL OR datetime(s.SaleDate) >= datetime(@FromDate))
                             AND (@ToDate IS NULL OR datetime(s.SaleDate) <= datetime(@ToDate));
                           """;
        using var connection = _databaseContext.CreateConnection();
        var row = connection.QueryFirst<(decimal Revenue, decimal Capital)>(
            sql, new { FromDate = from, ToDate = to });
        return (row.Revenue, row.Capital, row.Revenue - row.Capital);
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
                           SELECT SaleID, SaleDate, TotalAmount, DiscountAmount, DiscountNote, CustomerName, CustomerID, IsDebt
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
                                  sd.UnitPrice,
                                  sd.Note
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
            INSERT INTO SaleDetails (SaleID, ProductID, Quantity, UnitPrice, Note)
            VALUES (@SaleID, @ProductID, @Quantity, @UnitPrice, @Note);
            """,
            new
            {
                SaleID = saleId,
                item.ProductID,
                item.Quantity,
                item.UnitPrice,
                item.Note
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

    public void ReturnItem(int saleId, int productId, int quantity, decimal refundAmount, string? reason)
    {
        if (quantity <= 0) throw new InvalidOperationException("Số lượng trả phải lớn hơn 0.");

        using var connection = _databaseContext.CreateConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            var currentQty = connection.ExecuteScalar<int>(
                "SELECT Quantity FROM SaleDetails WHERE SaleID = @SaleID AND ProductID = @ProductID LIMIT 1;",
                new { SaleID = saleId, ProductID = productId }, transaction);

            if (quantity > currentQty)
            {
                throw new InvalidOperationException($"Số lượng trả ({quantity}) vượt quá số lượng trong đơn ({currentQty}).");
            }

            // Cộng lại kho
            connection.Execute(
                "UPDATE Products SET StockQuantity = StockQuantity + @Quantity WHERE ProductID = @ProductID;",
                new { Quantity = quantity, ProductID = productId }, transaction);

            // Cập nhật chi tiết đơn
            if (quantity == currentQty)
            {
                connection.Execute(
                    "DELETE FROM SaleDetails WHERE SaleID = @SaleID AND ProductID = @ProductID;",
                    new { SaleID = saleId, ProductID = productId }, transaction);
            }
            else
            {
                connection.Execute(
                    "UPDATE SaleDetails SET Quantity = Quantity - @Quantity WHERE SaleID = @SaleID AND ProductID = @ProductID;",
                    new { Quantity = quantity, SaleID = saleId, ProductID = productId }, transaction);
            }

            // Giảm tổng tiền hoá đơn
            connection.Execute(
                "UPDATE Sales SET TotalAmount = MAX(0, TotalAmount - @RefundAmount) WHERE SaleID = @SaleID;",
                new { RefundAmount = refundAmount, SaleID = saleId }, transaction);

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    // ==================== THUẾ & BÁO CÁO DOANH THU ====================

    /// <summary>Doanh thu từng tháng trong năm (12 tháng, tháng không có = 0).</summary>
    public List<MonthlyRevenueSummary> GetMonthlyRevenueSummary(int year)
    {
        const string sql = """
                           SELECT
                               CAST(strftime('%m', SaleDate, 'localtime') AS INTEGER) AS Month,
                               COUNT(*) AS InvoiceCount,
                               COALESCE(SUM(TotalAmount), 0) AS Revenue
                           FROM Sales
                           WHERE strftime('%Y', SaleDate, 'localtime') = @Year
                             AND IsDebt = 0
                           GROUP BY strftime('%m', SaleDate, 'localtime')
                           ORDER BY Month;
                           """;
        using var connection = _databaseContext.CreateConnection();
        var rows = connection.Query<(int Month, int InvoiceCount, decimal Revenue)>(
            sql, new { Year = year.ToString("D4") }).ToList();

        // Đảm bảo đủ 12 tháng
        return Enumerable.Range(1, 12).Select(m =>
        {
            var found = rows.FirstOrDefault(r => r.Month == m);
            return new MonthlyRevenueSummary
            {
                Year = year,
                Month = m,
                InvoiceCount = found.InvoiceCount,
                Revenue = found.Revenue
            };
        }).ToList();
    }

    /// <summary>Doanh thu từng quý trong năm.</summary>
    public List<QuarterlyRevenueSummary> GetQuarterlyRevenueSummary(int year)
    {
        var monthly = GetMonthlyRevenueSummary(year);
        return Enumerable.Range(1, 4).Select(q => new QuarterlyRevenueSummary
        {
            Year = year,
            Quarter = q,
            InvoiceCount = monthly.Where(m => (m.Month - 1) / 3 + 1 == q).Sum(m => m.InvoiceCount),
            Revenue = monthly.Where(m => (m.Month - 1) / 3 + 1 == q).Sum(m => m.Revenue)
        }).ToList();
    }

    /// <summary>Tổng doanh thu theo nhiều năm (để so sánh).</summary>
    public List<YearlyRevenueSummary> GetYearlyRevenueSummary(int years = 3)
    {
        const string sql = """
                           SELECT
                               CAST(strftime('%Y', SaleDate, 'localtime') AS INTEGER) AS Year,
                               COUNT(*) AS InvoiceCount,
                               COALESCE(SUM(TotalAmount), 0) AS Revenue
                           FROM Sales
                           WHERE IsDebt = 0
                             AND strftime('%Y', SaleDate, 'localtime') >= @MinYear
                           GROUP BY strftime('%Y', SaleDate, 'localtime')
                           ORDER BY Year DESC;
                           """;
        var minYear = DateTime.Now.Year - years + 1;
        using var connection = _databaseContext.CreateConnection();
        return connection.Query<YearlyRevenueSummary>(sql, new { MinYear = minYear.ToString() }).ToList();
    }

    /// <summary>Danh sách hoá đơn chi tiết theo kỳ (cho xuất CSV kê khai thuế).</summary>
    public List<TaxInvoiceRow> GetTaxInvoiceRows(DateTime from, DateTime to)
    {
        const string sql = """
                           SELECT
                               s.SaleID,
                               strftime('%d/%m/%Y', s.SaleDate, 'localtime') AS SaleDate,
                               strftime('%H:%M', s.SaleDate, 'localtime') AS SaleTime,
                               COALESCE(s.CustomerName, 'Khách lẻ') AS CustomerName,
                               COALESCE(s.TotalAmount, 0) AS Revenue,
                               COALESCE(s.DiscountAmount, 0) AS DiscountAmount,
                               COUNT(sd.DetailID) AS ItemCount,
                               CASE WHEN s.IsDebt = 1 THEN 'Ghi nợ' ELSE 'Tiền mặt/CK' END AS PaymentMethod
                           FROM Sales s
                           LEFT JOIN SaleDetails sd ON sd.SaleID = s.SaleID
                           WHERE datetime(s.SaleDate) >= datetime(@From)
                             AND datetime(s.SaleDate) <= datetime(@To)
                             AND s.IsDebt = 0
                           GROUP BY s.SaleID
                           ORDER BY s.SaleDate ASC;
                           """;
        using var connection = _databaseContext.CreateConnection();
        return connection.Query<TaxInvoiceRow>(sql, new { From = from, To = to }).ToList();
    }
}

// ==================== MODELS BÁO CÁO THUẾ ====================
public class MonthlyRevenueSummary
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int InvoiceCount { get; set; }
    public decimal Revenue { get; set; }
}

public class QuarterlyRevenueSummary
{
    public int Year { get; set; }
    public int Quarter { get; set; }
    public int InvoiceCount { get; set; }
    public decimal Revenue { get; set; }
}

public class YearlyRevenueSummary
{
    public int Year { get; set; }
    public int InvoiceCount { get; set; }
    public decimal Revenue { get; set; }
}

public class TaxInvoiceRow
{
    public int SaleID { get; set; }
    public string SaleDate { get; set; } = "";
    public string SaleTime { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public decimal Revenue { get; set; }
    public decimal DiscountAmount { get; set; }
    public int ItemCount { get; set; }
    public string PaymentMethod { get; set; } = "";
}

