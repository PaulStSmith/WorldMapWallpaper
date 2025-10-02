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
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        using var form = new SettingsForm();
        Application.Run(form);
    }
}