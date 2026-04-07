namespace PMTapHoa.Desktop.Models;

public class DebtPayment
{
    public int PaymentID { get; set; }
    public int SaleID { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string? Note { get; set; }
}
