using Dapper;
using PMTapHoa.Desktop.Data;
using PMTapHoa.Desktop.Models;
using System.Data;

namespace PMTapHoa.Desktop.Services;

public class DemoDataService
{
    private readonly DatabaseContext _databaseContext;

    public DemoDataService(DatabaseContext databaseContext)
    {
        _databaseContext = databaseContext;
    }

    public bool SeedIfEmpty()
    {
        using var connection = _databaseContext.CreateConnection();
        var productCount = connection.ExecuteScalar<int>("SELECT COUNT(1) FROM Products;");
        if (productCount > 0)
        {
            return false;
        }

        using var transaction = connection.BeginTransaction();
        try
        {
            var categoryMap = EnsureCategories(connection, transaction);
            InsertProducts(connection, transaction, categoryMap);
            InsertSampleSales(connection, transaction);
            transaction.Commit();
            return true;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static Dictionary<string, int> EnsureCategories(IDbConnection connection, IDbTransaction transaction)
    {
        var names = new[] { "Nước giải khát", "Đồ ăn vặt", "Gia vị", "Đồ gia dụng" };
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var name in names)
        {
            var id = connection.ExecuteScalar<int?>(
                "SELECT CategoryID FROM Categories WHERE CategoryName = @CategoryName LIMIT 1;",
                new { CategoryName = name },
                transaction);

            if (!id.HasValue)
            {
                id = connection.ExecuteScalar<int>(
                    """
                    INSERT INTO Categories (CategoryName)
                    VALUES (@CategoryName);
                    SELECT last_insert_rowid();
                    """,
                    new { CategoryName = name },
                    transaction);
            }

            result[name] = id.Value;
        }

        return result;
    }

    private static void InsertProducts(IDbConnection connection, IDbTransaction transaction, Dictionary<string, int> categories)
    {
        var products = new[]
        {
            new Product { Barcode = "8938505974011", ProductName = "Coca Cola lon", CategoryID = categories["Nước giải khát"], Unit = "Lon", CostPrice = 7500, SellingPrice = 10000, StockQuantity = 120, MinStock = 20 },
            new Product { Barcode = "8938505974028", ProductName = "Pepsi lon", CategoryID = categories["Nước giải khát"], Unit = "Lon", CostPrice = 7300, SellingPrice = 9800, StockQuantity = 90, MinStock = 20 },
            new Product { Barcode = "8934803043210", ProductName = "Nước suối 500ml", CategoryID = categories["Nước giải khát"], Unit = "Chai", CostPrice = 3500, SellingPrice = 5000, StockQuantity = 150, MinStock = 30 },
            new Product { Barcode = "8936036020019", ProductName = "Mì gói Hảo Hảo", CategoryID = categories["Đồ ăn vặt"], Unit = "Gói", CostPrice = 3000, SellingPrice = 4500, StockQuantity = 200, MinStock = 50 },
            new Product { Barcode = "8934564601012", ProductName = "Bánh Oreo", CategoryID = categories["Đồ ăn vặt"], Unit = "Gói", CostPrice = 8000, SellingPrice = 12000, StockQuantity = 80, MinStock = 20 },
            new Product { Barcode = "8934564601029", ProductName = "Snack khoai tây", CategoryID = categories["Đồ ăn vặt"], Unit = "Gói", CostPrice = 5500, SellingPrice = 8000, StockQuantity = 70, MinStock = 20 },
            new Product { Barcode = "8935001710012", ProductName = "Nước mắm 500ml", CategoryID = categories["Gia vị"], Unit = "Chai", CostPrice = 18000, SellingPrice = 25000, StockQuantity = 35, MinStock = 10 },
            new Product { Barcode = "8935001710029", ProductName = "Đường cát 1kg", CategoryID = categories["Gia vị"], Unit = "Kg", CostPrice = 21000, SellingPrice = 26000, StockQuantity = 40, MinStock = 10 },
            new Product { Barcode = "8936017362015", ProductName = "Dầu ăn 1L", CategoryID = categories["Gia vị"], Unit = "Chai", CostPrice = 38000, SellingPrice = 45000, StockQuantity = 30, MinStock = 8 },
            new Product { Barcode = "8938508009017", ProductName = "Nước rửa chén 750ml", CategoryID = categories["Đồ gia dụng"], Unit = "Chai", CostPrice = 19000, SellingPrice = 27000, StockQuantity = 25, MinStock = 8 },
            new Product { Barcode = "8938508009024", ProductName = "Bột giặt 3kg", CategoryID = categories["Đồ gia dụng"], Unit = "Túi", CostPrice = 92000, SellingPrice = 115000, StockQuantity = 18, MinStock = 5 },
            new Product { Barcode = "8938508009031", ProductName = "Giấy vệ sinh 10 cuộn", CategoryID = categories["Đồ gia dụng"], Unit = "Lốc", CostPrice = 52000, SellingPrice = 65000, StockQuantity = 16, MinStock = 5 }
        };

        foreach (var p in products)
        {
            connection.Execute(
                """
                INSERT INTO Products (Barcode, ProductName, CategoryID, Unit, CostPrice, SellingPrice, StockQuantity, MinStock, ExpiryDate)
                VALUES (@Barcode, @ProductName, @CategoryID, @Unit, @CostPrice, @SellingPrice, @StockQuantity, @MinStock, NULL);
                """,
                p,
                transaction);
        }
    }

    private static void InsertSampleSales(IDbConnection connection, IDbTransaction transaction)
    {
        var cokeId = connection.ExecuteScalar<int>("SELECT ProductID FROM Products WHERE Barcode = '8938505974011' LIMIT 1;", transaction: transaction);
        var noodleId = connection.ExecuteScalar<int>("SELECT ProductID FROM Products WHERE Barcode = '8936036020019' LIMIT 1;", transaction: transaction);

        var saleId1 = connection.ExecuteScalar<int>(
            """
            INSERT INTO Sales (SaleDate, TotalAmount, CustomerName, IsDebt)
            VALUES (datetime('now', '-2 hour'), 39000, 'Khách lẻ', 0);
            SELECT last_insert_rowid();
            """,
            transaction: transaction);
        connection.Execute(
            """
            INSERT INTO SaleDetails (SaleID, ProductID, Quantity, UnitPrice)
            VALUES (@SaleID, @ProductID1, 3, 10000),
                   (@SaleID, @ProductID2, 2, 4500);
            """,
            new { SaleID = saleId1, ProductID1 = cokeId, ProductID2 = noodleId },
            transaction);

        var saleId2 = connection.ExecuteScalar<int>(
            """
            INSERT INTO Sales (SaleDate, TotalAmount, CustomerName, IsDebt)
            VALUES (datetime('now', '-1 day'), 80000, 'Nguyễn Văn A', 1);
            SELECT last_insert_rowid();
            """,
            transaction: transaction);
        connection.Execute(
            """
            INSERT INTO SaleDetails (SaleID, ProductID, Quantity, UnitPrice)
            VALUES (@SaleID, @ProductID1, 5, 10000),
                   (@SaleID, @ProductID2, 3, 10000);
            """,
            new { SaleID = saleId2, ProductID1 = cokeId, ProductID2 = cokeId },
            transaction);

        connection.Execute(
            """
            INSERT INTO DebtPayments (SaleID, PaymentDate, Amount, Note)
            VALUES (@SaleID, datetime('now', '-12 hour'), 30000, 'Thu nợ mẫu');
            """,
            new { SaleID = saleId2 },
            transaction);

        connection.Execute(
            """
            UPDATE Products SET StockQuantity = StockQuantity - 8 WHERE ProductID = @ProductID1;
            UPDATE Products SET StockQuantity = StockQuantity - 2 WHERE ProductID = @ProductID2;
            """,
            new { ProductID1 = cokeId, ProductID2 = noodleId },
            transaction);
    }
}
