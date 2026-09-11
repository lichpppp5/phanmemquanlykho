namespace PMTapHoa.Core.Models;

public class AuditLog
{
    public int LogID { get; set; }
    public DateTime LogTime { get; set; }
    public string? Username { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Detail { get; set; }
}
