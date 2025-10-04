using WorldMapWallpaper.Shared;

namespace WorldMapWallpaper.Settings;

/// <summary>
/// The main entry point for the World Map Wallpaper Settings application.
/// </summary>
internal static class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // Check if we should start minimized to tray (for startup launch)
        var minimizeToTray = args.Contains("--minimized") || args.Contains("/minimized");

        using var form = new SettingsForm(minimizeToTray);
        Application.Run(form);
    }
}