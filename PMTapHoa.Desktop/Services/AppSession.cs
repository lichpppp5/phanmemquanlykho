using PMTapHoa.Desktop.Models;

namespace PMTapHoa.Desktop.Services;

public class AppSession
{
    public UserAccount? CurrentUser { get; private set; }

    public bool IsAdmin =>
        string.Equals(CurrentUser?.Role, "Admin", StringComparison.OrdinalIgnoreCase)
        || string.Equals(CurrentUser?.Role, "Manager", StringComparison.OrdinalIgnoreCase);

    public void SetUser(UserAccount user)
    {
        CurrentUser = user;
    }

    public void Clear()
    {
        CurrentUser = null;
    }
}
