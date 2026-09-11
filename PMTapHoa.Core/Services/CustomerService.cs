using Dapper;
using PMTapHoa.Core.Data;
using PMTapHoa.Core.Models;

namespace PMTapHoa.Core.Services;

public class CustomerService
{
    private readonly DatabaseContext _databaseContext;

    public CustomerService(DatabaseContext databaseContext)
    {
        _databaseContext = databaseContext;
    }

    public List<Customer> GetAll()
    {
        const string sql = """
                           SELECT CustomerID, CustomerName, Phone, Address, Note, CreatedDate
                           FROM Customers
                           ORDER BY CustomerName;
                           """;
        using var connection = _databaseContext.CreateConnection();
        return connection.Query<Customer>(sql).ToList();
    }

    public List<Customer> Search(string keyword)
    {
        const string sql = """
                           SELECT CustomerID, CustomerName, Phone, Address, Note, CreatedDate
                           FROM Customers
                           WHERE CustomerName LIKE @Keyword OR Phone LIKE @Keyword
                           ORDER BY CustomerName;
                           """;
        using var connection = _databaseContext.CreateConnection();
        return connection.Query<Customer>(sql, new { Keyword = $"%{keyword}%" }).ToList();
    }

    public Customer? GetById(int customerId)
    {
        const string sql = """
                           SELECT CustomerID, CustomerName, Phone, Address, Note, CreatedDate
                           FROM Customers
                           WHERE CustomerID = @CustomerID LIMIT 1;
                           """;
        using var connection = _databaseContext.CreateConnection();
        return connection.QueryFirstOrDefault<Customer>(sql, new { CustomerID = customerId });
    }

    public Customer? GetByPhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return null;
        const string sql = """
                           SELECT CustomerID, CustomerName, Phone, Address, Note, CreatedDate
                           FROM Customers
                           WHERE Phone = @Phone LIMIT 1;
                           """;
        using var connection = _databaseContext.CreateConnection();
        return connection.QueryFirstOrDefault<Customer>(sql, new { Phone = phone.Trim() });
    }

    public int Create(Customer customer)
    {
        if (string.IsNullOrWhiteSpace(customer.CustomerName))
            throw new InvalidOperationException("Tên khách hàng không được để trống.");

        const string sql = """
                           INSERT INTO Customers (CustomerName, Phone, Address, Note)
                           VALUES (@CustomerName, @Phone, @Address, @Note);
                           SELECT last_insert_rowid();
                           """;
        using var connection = _databaseContext.CreateConnection();
        return connection.ExecuteScalar<int>(sql, customer);
    }

    public void Update(Customer customer)
    {
        if (string.IsNullOrWhiteSpace(customer.CustomerName))
            throw new InvalidOperationException("Tên khách hàng không được để trống.");

        const string sql = """
                           UPDATE Customers
                           SET CustomerName = @CustomerName,
                               Phone = @Phone,
                               Address = @Address,
                               Note = @Note
                           WHERE CustomerID = @CustomerID;
                           """;
        using var connection = _databaseContext.CreateConnection();
        connection.Execute(sql, customer);
    }

    public void Delete(int customerId)
    {
        using var connection = _databaseContext.CreateConnection();
        connection.Execute("DELETE FROM Customers WHERE CustomerID = @CustomerID;", new { CustomerID = customerId });
    }

    /// <summary>Lấy tổng chi tiêu và số lần mua của khách hàng.</summary>
    public (decimal TotalSpent, int OrderCount, DateTime? LastPurchase) GetCustomerStats(int customerId)
    {
        const string sql = """
                           SELECT COALESCE(SUM(TotalAmount - COALESCE(DiscountAmount, 0)), 0) AS TotalSpent,
                                  COUNT(*) AS OrderCount,
                                  MAX(SaleDate) AS LastPurchase
                           FROM Sales
                           WHERE CustomerID = @CustomerID AND IsDebt = 0;
                           """;
        using var connection = _databaseContext.CreateConnection();
        var result = connection.QueryFirstOrDefault<(decimal TotalSpent, int OrderCount, DateTime? LastPurchase)>(
            sql, new { CustomerID = customerId });
        return result;
    }
}
