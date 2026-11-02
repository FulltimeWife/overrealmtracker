using OverrealmTracker.Config;
using OverrealmTracker.UI;

namespace OverrealmTracker;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        var config = ConfigManager.Load();
        Application.Run(new OverlayForm(config));
    }
}
