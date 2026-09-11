namespace PMTapHoa.Core.Models;

public class HealthCheckItem
{
    public string CheckName { get; set; } = string.Empty;
    public string Status { get; set; } = "OK";
    public string Message { get; set; } = string.Empty;
}
