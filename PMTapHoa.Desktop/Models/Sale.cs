namespace PMTapHoa.Desktop.Models;

public class Sale
{
    public int SaleID { get; set; }
    public DateTime SaleDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string? CustomerName { get; set; }
    public bool IsDebt { get; set; }
}
