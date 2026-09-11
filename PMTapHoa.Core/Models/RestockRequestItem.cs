namespace PMTapHoa.Core.Models;

public class RestockRequestItem
{
    public int RequestID { get; set; }
    public int ProductID { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public int? SupplierID { get; set; }
    public string? SupplierName { get; set; }
    public double RequestedQty { get; set; }
    public decimal? ExpectedCostPrice { get; set; }
    public string Status { get; set; } = "Open";
    public DateTime RequestDate { get; set; }
    public string? Note { get; set; }
}
