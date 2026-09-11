namespace PMTapHoa.Core.Models;

public class SaleDetail
{
    public int DetailID { get; set; }
    public int SaleID { get; set; }
    public int ProductID { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
