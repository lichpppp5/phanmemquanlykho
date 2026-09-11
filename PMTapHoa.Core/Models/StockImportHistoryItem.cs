namespace PMTapHoa.Core.Models;

public class StockImportHistoryItem
{
    public int ImportID { get; set; }
    public int ProductID { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? SupplierName { get; set; }
    public double Quantity { get; set; }
    public decimal CostPrice { get; set; }
    public DateTime ImportDate { get; set; }
    public string? ImportedBy { get; set; }
    public string? Note { get; set; }
}
