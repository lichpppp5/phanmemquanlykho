namespace PMTapHoa.Desktop.Models;

public class Customer
{
    public int CustomerID { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedDate { get; set; }
}
