using PMTapHoa.Desktop.Forms;

namespace PMTapHoa.Desktop;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        var appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PMTapHoa");
        var sqlScriptPath = Path.Combine(AppContext.BaseDirectory, "database.sql");
        var services = new AppServices(appDataDir, sqlScriptPath);

        using var loginForm = new LoginForm(services);
        if (loginForm.ShowDialog() != DialogResult.OK || services.Session.CurrentUser == null)
        {
            return;
        }

        Application.Run(new MainForm(services));
    }
}
