using Dapper;
using PMTapHoa.Core.Data;
using PMTapHoa.Core.Models;

namespace PMTapHoa.Core.Services;

public class DebtService
{
    private readonly DatabaseContext _databaseContext;

    public DebtService(DatabaseContext databaseContext)
    {
        _databaseContext = databaseContext;
    }

    public List<DebtSaleItem> GetOutstandingDebts(string? keyword)
    {
        const string sql = """
                           SELECT s.SaleID,
                                  s.SaleDate,
                                  s.CustomerName,
                                  c.Phone AS CustomerPhone,
                                  s.TotalAmount,
                                  COALESCE(dp.PaidAmount, 0) AS PaidAmount,
                                  (s.TotalAmount - COALESCE(dp.PaidAmount, 0)) AS OutstandingAmount
                           FROM Sales s
                           LEFT JOIN Customers c ON c.CustomerID = s.CustomerID
                           LEFT JOIN (
                               SELECT SaleID, SUM(Amount) AS PaidAmount
                               FROM DebtPayments
                               GROUP BY SaleID
                           ) dp ON dp.SaleID = s.SaleID
                           WHERE s.IsDebt = 1
                             AND (s.TotalAmount - COALESCE(dp.PaidAmount, 0)) > 0
                             AND (@Keyword IS NULL
                                  OR s.CustomerName LIKE @LikeKeyword
                                  OR CAST(s.SaleID AS TEXT) LIKE @LikeKeyword
                                  OR c.Phone LIKE @LikeKeyword)
                           ORDER BY s.SaleDate DESC;
                           """;
        var trimmed = string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim();
        using var connection = _databaseContext.CreateConnection();
        return connection.Query<DebtSaleItem>(
            sql,
            new
            {
                Keyword = trimmed,
                LikeKeyword = $"%{trimmed}%"
            }).ToList();
    }

    public List<DebtPayment> GetPaymentsBySale(int saleId)
    {
        const string sql = """
                           SELECT PaymentID, SaleID, PaymentDate, Amount, Note
                           FROM DebtPayments
                           WHERE SaleID = @SaleID
                           ORDER BY PaymentDate DESC;
                           """;
        using var connection = _databaseContext.CreateConnection();
        return connection.Query<DebtPayment>(sql, new { SaleID = saleId }).ToList();
    }

    public void AddPayment(int saleId, decimal amount, string? note)
    {
        if (amount <= 0)
        {
            throw new InvalidOperationException("Số tiền trả phải lớn hơn 0.");
        }

        using var connection = _databaseContext.CreateConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            var debt = connection.QueryFirstOrDefault<DebtSaleItem>(
                """
                SELECT s.SaleID,
                       s.SaleDate,
                       s.CustomerName,
                       s.TotalAmount,
                       COALESCE(dp.PaidAmount, 0) AS PaidAmount,
                       (s.TotalAmount - COALESCE(dp.PaidAmount, 0)) AS OutstandingAmount
                FROM Sales s
                LEFT JOIN (
                    SELECT SaleID, SUM(Amount) AS PaidAmount
                    FROM DebtPayments
                    GROUP BY SaleID
                ) dp ON dp.SaleID = s.SaleID
                WHERE s.SaleID = @SaleID
                LIMIT 1;
                """,
                new { SaleID = saleId },
                transaction);

            if (debt == null)
            {
                throw new InvalidOperationException("Không tìm thấy hóa đơn công nợ.");
            }

            if (amount > debt.OutstandingAmount)
            {
                throw new InvalidOperationException("Số tiền trả vượt quá số tiền còn nợ.");
            }

            connection.Execute(
                """
                INSERT INTO DebtPayments (SaleID, PaymentDate, Amount, Note)
                VALUES (@SaleID, CURRENT_TIMESTAMP, @Amount, @Note);
                """,
                new { SaleID = saleId, Amount = amount, Note = note?.Trim() },
                transaction);

            var remaining = debt.OutstandingAmount - amount;
            if (remaining <= 0.0001m)
            {
                connection.Execute(
                    """
                    UPDATE Sales
                    SET IsDebt = 0
                    WHERE SaleID = @SaleID;
                    """,
                    new { SaleID = saleId },
                    transaction);
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public decimal GetTotalOutstanding()
    {
        const string sql = """
                           SELECT COALESCE(SUM(s.TotalAmount - COALESCE(dp.PaidAmount, 0)), 0)
                           FROM Sales s
                           LEFT JOIN (
                               SELECT SaleID, SUM(Amount) AS PaidAmount
                               FROM DebtPayments
                               GROUP BY SaleID
                           ) dp ON dp.SaleID = s.SaleID
                           WHERE s.IsDebt = 1
                             AND (s.TotalAmount - COALESCE(dp.PaidAmount, 0)) > 0;
                           """;
        using var connection = _databaseContext.CreateConnection();
        return connection.ExecuteScalar<decimal>(sql);
    }

    /// <summary>Đếm số nợ quá hạn (quá X ngày kể từ ngày bán).</summary>
    public int GetOverdueDebtCount(int overdueDays = 30)
    {
        const string sql = """
                           SELECT COUNT(*)
                           FROM Sales s
                           LEFT JOIN (
                               SELECT SaleID, SUM(Amount) AS PaidAmount
                               FROM DebtPayments
                               GROUP BY SaleID
                           ) dp ON dp.SaleID = s.SaleID
                           WHERE s.IsDebt = 1
                             AND (s.TotalAmount - COALESCE(dp.PaidAmount, 0)) > 0
                             AND julianday('now','localtime') - julianday(s.SaleDate) > @OverdueDays;
                           """;
        using var connection = _databaseContext.CreateConnection();
        return connection.ExecuteScalar<int>(sql, new { OverdueDays = overdueDays });
    }

    /// <summary>Lấy danh sách nợ quá hạn theo ngày.</summary>
    public List<DebtSaleItem> GetOverdueDebts(int overdueDays = 30)
    {
        const string sql = """
                           SELECT s.SaleID,
                                  s.SaleDate,
                                  s.CustomerName,
                                  c.Phone AS CustomerPhone,
                                  s.TotalAmount,
                                  COALESCE(dp.PaidAmount, 0) AS PaidAmount,
                                  (s.TotalAmount - COALESCE(dp.PaidAmount, 0)) AS OutstandingAmount
                           FROM Sales s
                           LEFT JOIN Customers c ON c.CustomerID = s.CustomerID
                           LEFT JOIN (
                               SELECT SaleID, SUM(Amount) AS PaidAmount
                               FROM DebtPayments
                               GROUP BY SaleID
                           ) dp ON dp.SaleID = s.SaleID
                           WHERE s.IsDebt = 1
                             AND (s.TotalAmount - COALESCE(dp.PaidAmount, 0)) > 0
                             AND julianday('now','localtime') - julianday(s.SaleDate) > @OverdueDays
                           ORDER BY s.SaleDate ASC;
                           """;
        using var connection = _databaseContext.CreateConnection();
        return connection.Query<DebtSaleItem>(sql, new { OverdueDays = overdueDays }).ToList();
    }
}
