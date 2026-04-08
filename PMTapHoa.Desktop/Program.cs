using PMTapHoa.Desktop.Forms;
using System.Windows.Forms;

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
        var primaryScreen = Screen.PrimaryScreen?.WorkingArea;
        var forceFullHdMode = primaryScreen.HasValue && primaryScreen.Value.Width >= 1920;
        UiStyle.FullScreenEnabled = forceFullHdMode || services.AppConfigService.DisplayFullScreen;

        while (true)
        {
            using var loginForm = new LoginForm(services);
            if (loginForm.ShowDialog() != DialogResult.OK || services.Session.CurrentUser == null)
            {
                return;
            }

            using var mainForm = new MainForm(services);
            Application.Run(mainForm);
            if (!mainForm.RequestLogout)
            {
                break;
            }
        }
    }
}
