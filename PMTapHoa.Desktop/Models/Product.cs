namespace PMTapHoa.Desktop.Models;

public class Product
{
    public int ProductID { get; set; }
    public string? Barcode { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int? CategoryID { get; set; }
    public string? CategoryName { get; set; }
    public string? Unit { get; set; }
    public decimal CostPrice { get; set; }
    public decimal SellingPrice { get; set; }
    public double StockQuantity { get; set; }
    public int MinStock { get; set; } = 5;
    public DateTime? ExpiryDate { get; set; }
}
