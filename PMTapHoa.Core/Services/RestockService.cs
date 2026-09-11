using Dapper;
using PMTapHoa.Core.Data;
using PMTapHoa.Core.Models;
using System.Data;

namespace PMTapHoa.Core.Services;

public class RestockService
{
    private readonly DatabaseContext _databaseContext;

    public RestockService(DatabaseContext databaseContext)
    {
        _databaseContext = databaseContext;
    }

    public int CreateRequest(
        int productId,
        string supplierName,
        string? contactName,
        string? phone,
        string? address,
        double requestedQty,
        decimal? expectedCostPrice,
        string? note)
    {
        if (productId <= 0)
        {
            throw new InvalidOperationException("Sản phẩm không hợp lệ.");
        }

        if (requestedQty <= 0)
        {
            throw new InvalidOperationException("Số lượng đề nghị nhập phải lớn hơn 0.");
        }

        var supplier = (supplierName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(supplier))
        {
            throw new InvalidOperationException("Tên mối nhập hàng không được để trống.");
        }

        using var connection = _databaseContext.CreateConnection();
        using var transaction = connection.BeginTransaction();
        try
        {
            var supplierId = EnsureSupplier(connection, transaction, supplier, contactName, phone, address);
            var requestId = connection.ExecuteScalar<int>(
                """
                INSERT INTO RestockRequests (ProductID, SupplierID, RequestedQty, ExpectedCostPrice, Status, RequestDate, Note)
                VALUES (@ProductID, @SupplierID, @RequestedQty, @ExpectedCostPrice, 'Open', CURRENT_TIMESTAMP, @Note);
                SELECT last_insert_rowid();
                """,
                new
                {
                    ProductID = productId,
                    SupplierID = supplierId,
                    RequestedQty = requestedQty,
                    ExpectedCostPrice = expectedCostPrice,
                    Note = note?.Trim()
                },
                transaction);

            transaction.Commit();
            return requestId;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public List<RestockRequestItem> GetRequests(string? statusFilter = null)
    {
        const string sql = """
                           SELECT rr.RequestID,
                                  rr.ProductID,
                                  p.ProductName,
                                  p.Barcode,
                                  rr.SupplierID,
                                  s.SupplierName,
                                  rr.RequestedQty,
                                  rr.ExpectedCostPrice,
                                  rr.Status,
                                  rr.RequestDate,
                                  rr.Note
                           FROM RestockRequests rr
                           INNER JOIN Products p ON p.ProductID = rr.ProductID
                           LEFT JOIN Suppliers s ON s.SupplierID = rr.SupplierID
                           WHERE (@StatusFilter IS NULL OR rr.Status = @StatusFilter)
                           ORDER BY rr.RequestDate DESC;
                           """;
        var status = string.IsNullOrWhiteSpace(statusFilter) ? null : NormalizeStatus(statusFilter);
        using var connection = _databaseContext.CreateConnection();
        return connection.Query<RestockRequestItem>(sql, new { StatusFilter = status }).ToList();
    }

    public void UpdateStatus(int requestId, string newStatus)
    {
        var status = NormalizeStatus(newStatus);
        using var connection = _databaseContext.CreateConnection();
        connection.Execute(
            """
            UPDATE RestockRequests
            SET Status = @Status
            WHERE RequestID = @RequestID;
            """,
            new { Status = status, RequestID = requestId });
    }

    public List<LowStockSuggestionItem> GetLowStockSuggestions()
    {
        const string sql = """
                           SELECT p.ProductID,
                                  p.ProductName,
                                  p.Barcode,
                                  p.StockQuantity,
                                  p.MinStock,
                                  CASE
                                    WHEN (p.MinStock - p.StockQuantity) > 0 THEN (p.MinStock - p.StockQuantity)
                                    ELSE 1
                                  END AS SuggestedQty,
                                  CASE
                                    WHEN EXISTS (
                                        SELECT 1
                                        FROM RestockRequests rr
                                        WHERE rr.ProductID = p.ProductID
                                          AND rr.Status IN ('Open', 'Ordered')
                                    ) THEN 1
                                    ELSE 0
                                  END AS HasOpenRequest
                           FROM Products p
                           WHERE p.StockQuantity < p.MinStock
                           ORDER BY (p.MinStock - p.StockQuantity) DESC, p.ProductName;
                           """;
        using var connection = _databaseContext.CreateConnection();
        var rows = connection.Query<LowStockSuggestionItem>(sql).ToList();
        foreach (var row in rows)
        {
            row.IsSelected = !row.HasOpenRequest;
        }

        return rows;
    }

    private static int EnsureSupplier(
        IDbConnection connection,
        IDbTransaction transaction,
        string supplierName,
        string? contactName,
        string? phone,
        string? address)
    {
        var existing = connection.QueryFirstOrDefault<Supplier>(
            """
            SELECT SupplierID, SupplierName, ContactName, Phone, Address, Note
            FROM Suppliers
            WHERE SupplierName = @SupplierName
            LIMIT 1;
            """,
            new { SupplierName = supplierName },
            transaction);

        if (existing != null)
        {
            connection.Execute(
                """
                UPDATE Suppliers
                SET ContactName = COALESCE(@ContactName, ContactName),
                    Phone = COALESCE(@Phone, Phone),
                    Address = COALESCE(@Address, Address)
                WHERE SupplierID = @SupplierID;
                """,
                new
                {
                    SupplierID = existing.SupplierID,
                    ContactName = NullIfEmpty(contactName),
                    Phone = NullIfEmpty(phone),
                    Address = NullIfEmpty(address)
                },
                transaction);
            return existing.SupplierID;
        }

        return connection.ExecuteScalar<int>(
            """
            INSERT INTO Suppliers (SupplierName, ContactName, Phone, Address, Note)
            VALUES (@SupplierName, @ContactName, @Phone, @Address, NULL);
            SELECT last_insert_rowid();
            """,
            new
            {
                SupplierName = supplierName,
                ContactName = NullIfEmpty(contactName),
                Phone = NullIfEmpty(phone),
                Address = NullIfEmpty(address)
            },
            transaction);
    }

    private static string NormalizeStatus(string status)
    {
        var value = (status ?? string.Empty).Trim();
        return value switch
        {
            "Ordered" => "Ordered",
            "Received" => "Received",
            "Cancelled" => "Cancelled",
            _ => "Open"
        };
    }

    private static string? NullIfEmpty(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}
