namespace PMTapHoa.Desktop.Models;

public class DebtSaleItem
{
    public int SaleID { get; set; }
    public DateTime SaleDate { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal OutstandingAmount { get; set; }
}
