namespace PMTapHoa.Desktop.Models;

public class LowStockSuggestionItem
{
    public bool IsSelected { get; set; }
    public int ProductID { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public double StockQuantity { get; set; }
    public int MinStock { get; set; }
    public double SuggestedQty { get; set; }
    public bool HasOpenRequest { get; set; }
}
