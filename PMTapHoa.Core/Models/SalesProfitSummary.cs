namespace PMTapHoa.Core.Models;

public class SalesProfitSummary
{
    public decimal RevenueAmount { get; set; }
    public decimal CapitalAmount { get; set; }
    public decimal ProfitAmount => RevenueAmount - CapitalAmount;
}
