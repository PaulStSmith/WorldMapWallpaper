using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace WorldMapWallpaper.Shared;

/// <summary>
/// Manages integration with Windows Personalization settings as a background provider.
/// Provides proper registration and COM interface integration for Windows Settings.
/// This class handles wallpaper management, Windows registry integration, and COM interop
/// for seamless integration with Windows desktop personalization features.
/// </summary>
public static partial class PersonalizationProvider
{
    #region Constants
    
    /// <summary>
    /// Unique application identifier used for Windows registry entries and personalization integration.
    /// </summary>
    private const string AppId = "WorldMapWallpaper";
    
    /// <summary>
    /// Display name of the application as shown in Windows Settings and personalization dialogs.
    /// </summary>
    private const string AppName = "World Map Wallpaper";
    
    /// <summary>
    /// Descriptive text about the application's functionality for Windows Settings display.
    /// </summary>
    private const string AppDescription = "Dynamic wallpaper with real-time day/night cycle, ISS tracking, and timezone clocks";
    
    /// <summary>
    /// Registry path for Windows personalization settings in the current user hive.
    /// </summary>
    private const string PersonalizationBasePath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    
    /// <summary>
    /// Registry path for background providers registration in the local machine hive.
    /// </summary>
    private const string BackgroundProvidersPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\BackgroundProviders";
    
    /// <summary>
    /// Registry path for application path registration in the local machine hive.
    /// Used by Windows to locate executable files by name.
    /// </summary>
    private const string AppPathsBasePath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths";
    
    #endregion

    #region COM Interfaces and Enums

    /// <summary>
    /// COM interface for desktop wallpaper management (IDesktopWallpaper).
    /// Provides methods to set wallpapers, manage slideshow settings, and query monitor information.
    /// This interface is part of the Windows Shell API for desktop wallpaper management.
    /// </summary>
    [ComImport]
    [Guid("B92B56A9-8B55-4E14-9A89-0199BBB6F93B")]
    public partial interface IDesktopWallpaper
    {
        /// <summary>
        /// Sets the wallpaper for a specific monitor.
        /// </summary>
        /// <param name="monitorID">Device path of the target monitor, or null for all monitors.</param>
        /// <param name="wallpaper">Full path to the wallpaper image file.</param>
        void SetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string monitorID, [MarshalAs(UnmanagedType.LPWStr)] string wallpaper);
        
        /// <summary>
        /// Gets the current wallpaper path for a specific monitor.
        /// </summary>
        /// <param name="monitorID">Device path of the target monitor.</param>
        /// <returns>Full path to the current wallpaper image file.</returns>
        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string monitorID);
        
        /// <summary>
        /// Gets the device path for a monitor at the specified index.
        /// </summary>
        /// <param name="monitorIndex">Zero-based index of the monitor.</param>
        /// <returns>Device path string for the monitor.</returns>
        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetMonitorDevicePathAt(uint monitorIndex);
        
        /// <summary>
        /// Gets the total number of monitors in the system.
        /// </summary>
        /// <returns>Number of available monitors.</returns>
        uint GetMonitorDevicePathCount();
        
        /// <summary>
        /// Gets the rectangle coordinates for a specific monitor.
        /// </summary>
        /// <param name="monitorID">Device path of the target monitor.</param>
        /// <returns>String representation of the monitor's rectangle coordinates.</returns>
        [return: MarshalAs(UnmanagedType.LPWStr)]
        string GetMonitorRECT([MarshalAs(UnmanagedType.LPWStr)] string monitorID);
        
        /// <summary>
        /// Sets the desktop background color.
        /// </summary>
        /// <param name="color">Color value in RGB format.</param>
        void SetBackgroundColor(uint color);
        
        /// <summary>
        /// Gets the current desktop background color.
        /// </summary>
        /// <returns>Current background color in RGB format.</returns>
        uint GetBackgroundColor();
        
        /// <summary>
        /// Sets the wallpaper display position (fit, fill, stretch, etc.).
        /// </summary>
        /// <param name="position">Wallpaper positioning mode.</param>
        void SetPosition(DesktopWallpaperPosition position);
        
        /// <summary>
        /// Gets the current wallpaper display position.
        /// </summary>
        /// <returns>Current wallpaper positioning mode.</returns>
        [PreserveSig]
        DesktopWallpaperPosition GetPosition();
        
        /// <summary>
        /// Sets the slideshow image collection.
        /// </summary>
        /// <param name="items">Pointer to an IShellItemArray containing slideshow images.</param>
        void SetSlideshow(IntPtr items);
        
        /// <summary>
        /// Gets the current slideshow image collection.
        /// </summary>
        /// <returns>Pointer to an IShellItemArray containing slideshow images.</returns>
        IntPtr GetSlideshow();
        
        /// <summary>
        /// Sets slideshow options and timing.
        /// </summary>
        /// <param name="options">Slideshow behavior options.</param>
        /// <param name="slideshowTick">Time interval between slides in milliseconds.</param>
        void SetSlideshowOptions(DesktopSlideshowOptions options, uint slideshowTick);
        
        /// <summary>
        /// Gets current slideshow options and timing.
        /// </summary>
        /// <param name="options">Current slideshow behavior options.</param>
        /// <param name="slideshowTick">Current time interval between slides in milliseconds.</param>
        void GetSlideshowOptions(out DesktopSlideshowOptions options, out uint slideshowTick);
        
        /// <summary>
        /// Advances the slideshow to the next or previous image.
        /// </summary>
        /// <param name="monitorID">Device path of the target monitor, or null for all monitors.</param>
        /// <param name="direction">Direction to advance (forward or backward).</param>
        void AdvanceSlideshow([MarshalAs(UnmanagedType.LPWStr)] string monitorID, DesktopSlideshowDirection direction);
        
        /// <summary>
        /// Gets the current slideshow status.
        /// </summary>
        /// <returns>Current slideshow state flags.</returns>
        DesktopSlideshowState GetStatus();
        
        /// <summary>
        /// Enables or disables the slideshow functionality.
        /// </summary>
        /// <param name="enable">True to enable slideshow, false to disable.</param>
        void Enable([MarshalAs(UnmanagedType.Bool)] bool enable);
    }

    /// <summary>
    /// Defines how wallpaper images are positioned and scaled on the desktop.
    /// These values correspond to the Windows desktop background positioning options.
    /// </summary>
    public enum DesktopWallpaperPosition
    {
        /// <summary>
        /// Centers the image without scaling. Image appears at actual size in the center of the screen.
        /// </summary>
        Center = 0,
        
        /// <summary>
        /// Tiles the image to fill the entire desktop by repeating it horizontally and vertically.
        /// </summary>
        Tile = 1,
        
        /// <summary>
        /// Stretches the image to fill the entire desktop, potentially distorting the aspect ratio.
        /// </summary>
        Stretch = 2,
        
        /// <summary>
        /// Scales the image to fit within the desktop while maintaining aspect ratio. May leave empty space.
        /// </summary>
        Fit = 3,
        
        /// <summary>
        /// Scales the image to fill the entire desktop while maintaining aspect ratio. May crop parts of the image.
        /// </summary>
        Fill = 4,
        
        /// <summary>
        /// Spans the image across multiple monitors as a single large desktop on multi-monitor setups.
        /// </summary>
        Span = 5
    }

    /// <summary>
    /// Flags that control desktop slideshow behavior.
    /// These options determine how the slideshow operates when enabled.
    /// </summary>
    [Flags]
    public enum DesktopSlideshowOptions
    {
        /// <summary>
        /// Randomizes the order of images in the slideshow instead of displaying them sequentially.
        /// </summary>
        ShuffleImages = 0x01
    }

    /// <summary>
    /// Defines the direction for slideshow navigation.
    /// Used when manually advancing through slideshow images.
    /// </summary>
    public enum DesktopSlideshowDirection
    {
        /// <summary>
        /// Advances to the next image in the slideshow sequence.
        /// </summary>
        Forward = 0,
        
        /// <summary>
        /// Goes back to the previous image in the slideshow sequence.
        /// </summary>
        Backward = 1
    }

    /// <summary>
    /// Flags that indicate the current state of the desktop slideshow.
    /// Multiple flags can be combined to represent the complete slideshow status.
    /// </summary>
    public enum DesktopSlideshowState
    {
        /// <summary>
        /// Slideshow functionality is currently enabled.
        /// </summary>
        Enabled = 0x01,
        
        /// <summary>
        /// A slideshow is currently active and cycling through images.
        /// </summary>
        Slideshow = 0x02,
        
        /// <summary>
        /// Slideshow has been disabled due to a remote desktop session being active.
        /// </summary>
        DisabledByRemoteSession = 0x04
    }

    /// <summary>
    /// COM class for instantiating the Desktop Wallpaper interface.
    /// This class provides the implementation of IDesktopWallpaper for wallpaper management.
    /// </summary>
    [ComImport]
    [Guid("C2CF3110-460E-4fc1-B9D0-8A1C0C9CC4BD")]
    public class DesktopWallpaperClass
    {
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Registers World Map Wallpaper as a background provider in Windows Personalization settings.
    /// This enables the application to appear in Windows Settings as a wallpaper source and
    /// integrates with the Windows theming system for proper desktop background management.
    /// </summary>
    /// <param name="applicationPath">Full path to the main application executable file.</param>
    /// <param name="settingsPath">Full path to the settings application executable file.</param>
    /// <returns>True if registration succeeded and all registry entries were created successfully; false otherwise.</returns>
    /// <remarks>
    /// This method requires administrative privileges to write to HKEY_LOCAL_MACHINE registry keys.
    /// Registration includes background provider registration, personalization integration,
    /// application path registration, and background access enablement.
    /// </remarks>
    public static bool RegisterAsBackgroundProvider(string applicationPath, string settingsPath)
    {
        try
        {
            // Register as background provider
            RegisterBackgroundProvider(applicationPath, settingsPath);
            
            // Register for personalization integration
            RegisterPersonalizationIntegration(applicationPath);
            
            // Register app paths for Windows
            RegisterAppPaths(applicationPath, settingsPath);
            
            // Enable background access if needed
            EnableBackgroundAccess();
            
            return true;
        }
        catch (Exception ex)
        {
            // Log error but don't fail installation
            System.Diagnostics.Debug.WriteLine($"Failed to register background provider: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Unregisters World Map Wallpaper from Windows Personalization settings.
    /// Removes all registry entries created during registration, effectively removing
    /// the application from Windows Settings and theming integration.
    /// </summary>
    /// <returns>True if unregistration succeeded and all registry entries were removed successfully; false otherwise.</returns>
    /// <remarks>
    /// This method attempts to clean up all registry entries created during registration.
    /// It requires appropriate privileges to modify registry keys in both HKEY_LOCAL_MACHINE and HKEY_CURRENT_USER.
    /// </remarks>
    public static bool UnregisterBackgroundProvider()
    {
        try
        {
            // Remove background provider registration
            using (var key = Registry.LocalMachine.OpenSubKey(BackgroundProvidersPath, true))
            {
                key?.DeleteSubKeyTree(AppId, false);
            }
            
            // Remove personalization integration
            using (var key = Registry.CurrentUser.OpenSubKey(PersonalizationBasePath, true))
            {
                key?.DeleteValue($"{AppId}_Enabled", false);
            }
            
            // Remove app paths
            using (var key = Registry.LocalMachine.OpenSubKey(AppPathsBasePath, true))
            {
                key?.DeleteSubKeyTree("WorldMapWallpaper.exe", false);
                key?.DeleteSubKeyTree("WorldMapWallpaper.Settings.exe", false);
            }
            
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to unregister background provider: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Sets the desktop wallpaper using the Windows Shell COM interface for maximum compatibility.
    /// This method uses the official Windows API for wallpaper management and supports multi-monitor setups.
    /// </summary>
    /// <param name="wallpaperPath">Full path to the wallpaper image file. Must be a valid image format supported by Windows.</param>
    /// <param name="position">How the wallpaper should be positioned and scaled on the desktop. Defaults to Fill for best appearance.</param>
    /// <returns>True if the wallpaper was set successfully on all monitors; false if any error occurred.</returns>
    /// <remarks>
    /// This method sets the wallpaper on all available monitors in a multi-monitor setup.
    /// The image file must exist and be accessible, and the format must be supported by Windows (JPG, PNG, BMP, etc.).
    /// </remarks>
    public static bool SetWallpaperViaCOM(string wallpaperPath, DesktopWallpaperPosition position = DesktopWallpaperPosition.Fill)
    {
        try
        {
            var desktopWallpaper = (IDesktopWallpaper)new DesktopWallpaperClass();
            
            // Set position first
            desktopWallpaper.SetPosition(position);
            
            // Set wallpaper for all monitors
            var monitorCount = desktopWallpaper.GetMonitorDevicePathCount();
            for (uint i = 0; i < monitorCount; i++)
            {
                var monitorId = desktopWallpaper.GetMonitorDevicePathAt(i);
                desktopWallpaper.SetWallpaper(monitorId, wallpaperPath);
            }
            
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to set wallpaper via COM: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Retrieves the current wallpaper file path using the Windows Shell COM interface.
    /// This method queries the primary monitor for its current wallpaper setting.
    /// </summary>
    /// <returns>Full path to the current wallpaper image file, or null if the query failed or no wallpaper is set.</returns>
    /// <remarks>
    /// This method only queries the primary monitor. In multi-monitor setups where different
    /// wallpapers are set per monitor, only the primary monitor's wallpaper path is returned.
    /// </remarks>
    public static string? GetCurrentWallpaperViaCOM()
    {
        try
        {
            var desktopWallpaper = (IDesktopWallpaper)new DesktopWallpaperClass();
            
            // Get wallpaper from primary monitor
            if (desktopWallpaper.GetMonitorDevicePathCount() > 0)
            {
                var primaryMonitorId = desktopWallpaper.GetMonitorDevicePathAt(0);
                return desktopWallpaper.GetWallpaper(primaryMonitorId);
            }
            
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to get wallpaper via COM: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Determines if a wallpaper generated by this application is currently active on the desktop.
    /// This method checks the filename of the current wallpaper to identify if it was created by World Map Wallpaper.
    /// </summary>
    /// <returns>True if a wallpaper file starting with "worldmap" is currently set; false otherwise.</returns>
    /// <remarks>
    /// This method uses filename pattern matching to identify wallpapers created by this application.
    /// It assumes that all wallpapers generated by World Map Wallpaper have filenames starting with "worldmap".
    /// The comparison is case-insensitive for better reliability.
    /// </remarks>
    public static bool IsOurWallpaperActiveCOM()
    {
        try
        {
            var currentWallpaper = GetCurrentWallpaperViaCOM();
            if (string.IsNullOrEmpty(currentWallpaper))
                return false;

            var fileName = Path.GetFileName(currentWallpaper);
            return fileName?.StartsWith("worldmap", StringComparison.OrdinalIgnoreCase) == true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Opens the Windows Settings app to the Personalization > Background page.
    /// This allows users to access wallpaper and background settings through the standard Windows interface.
    /// </summary>
    /// <returns>True if the Settings app was launched successfully; false if the launch failed.</returns>
    /// <remarks>
    /// This method uses the Windows Settings URI scheme to directly navigate to the background settings page.
    /// The method requires Windows 10 or later for the Settings URI to be recognized.
    /// </remarks>
    public static bool OpenPersonalizationSettings()
    {
        try
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "ms-settings:personalization-background",
                UseShellExecute = true
            };
            
            System.Diagnostics.Process.Start(startInfo);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to open personalization settings: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Verifies if the application is properly registered as a Windows background provider.
    /// This method checks for the existence of the required registry entries that indicate successful registration.
    /// </summary>
    /// <returns>True if the application is properly registered with the required registry entries; false otherwise.</returns>
    /// <remarks>
    /// This method only checks for the existence of the main background provider registry key.
    /// A successful check indicates that the basic registration is in place, but does not verify
    /// the integrity or completeness of all registration data.
    /// </remarks>
    public static bool IsRegisteredAsBackgroundProvider()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey($@"{BackgroundProvidersPath}\{AppId}");
            return key != null;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Creates registry entries to register the application as a Windows background provider.
    /// This enables the application to be recognized by Windows as a source of desktop backgrounds.
    /// </summary>
    /// <param name="applicationPath">Full path to the main application executable.</param>
    /// <param name="settingsPath">Full path to the settings application executable.</param>
    /// <remarks>
    /// This method creates entries in HKEY_LOCAL_MACHINE that describe the application's capabilities,
    /// supported formats, and file paths. These entries are used by Windows to integrate the application
    /// with the personalization and theming system.
    /// </remarks>
    private static void RegisterBackgroundProvider(string applicationPath, string settingsPath)
    {
        using var providerKey = Registry.LocalMachine.CreateSubKey($@"{BackgroundProvidersPath}\{AppId}");
        if (providerKey != null)
        {
            providerKey.SetValue("", AppName, RegistryValueKind.String);
            providerKey.SetValue("DisplayName", AppName, RegistryValueKind.String);
            providerKey.SetValue("Description", AppDescription, RegistryValueKind.String);
            providerKey.SetValue("ApplicationPath", applicationPath, RegistryValueKind.String);
            providerKey.SetValue("SettingsPath", settingsPath, RegistryValueKind.String);
            providerKey.SetValue("Category", "Dynamic", RegistryValueKind.String);
            providerKey.SetValue("SupportedFormats", "jpg,jpeg,png,bmp", RegistryValueKind.String);
            providerKey.SetValue("Version", "1.0", RegistryValueKind.String);
            
            // Add capabilities
            providerKey.SetValue("SupportsSlideshow", 0, RegistryValueKind.DWord);
            providerKey.SetValue("SupportsRealTime", 1, RegistryValueKind.DWord);
            providerKey.SetValue("SupportsScheduling", 1, RegistryValueKind.DWord);
            providerKey.SetValue("SupportsMultiMonitor", 1, RegistryValueKind.DWord);
        }
    }

    /// <summary>
    /// Creates registry entries for Windows themes system integration.
    /// This enables deeper integration with Windows personalization features beyond basic background provision.
    /// </summary>
    /// <param name="applicationPath">Full path to the main application executable.</param>
    /// <remarks>
    /// This method creates entries in the current user's registry hive for theme integration.
    /// These entries allow the application to participate more fully in the Windows theming system.
    /// </remarks>
    private static void RegisterPersonalizationIntegration(string applicationPath)
    {
        // Register with Windows themes system
        using var themeKey = Registry.CurrentUser.CreateSubKey($@"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\{AppId}");
        if (themeKey != null)
        {
            themeKey.SetValue("DisplayName", AppName, RegistryValueKind.String);
            themeKey.SetValue("Description", AppDescription, RegistryValueKind.String);
            themeKey.SetValue("ApplicationPath", applicationPath, RegistryValueKind.String);
            themeKey.SetValue("ThemeId", AppId, RegistryValueKind.String);
            themeKey.SetValue("IsBackgroundProvider", 1, RegistryValueKind.DWord);
        }
    }

    /// <summary>
    /// Registers application executable paths with Windows for improved discoverability.
    /// This allows Windows to locate the application executables when referenced by name only.
    /// </summary>
    /// <param name="applicationPath">Full path to the main application executable.</param>
    /// <param name="settingsPath">Full path to the settings application executable.</param>
    /// <remarks>
    /// This method creates entries in the App Paths registry location that Windows uses to resolve
    /// executable names to full paths. This improves integration with Windows shell operations
    /// and allows the applications to be found more easily by the system.
    /// </remarks>
    private static void RegisterAppPaths(string applicationPath, string settingsPath)
    {
        var applicationDir = Path.GetDirectoryName(applicationPath);
        Debug.Assert(applicationDir != null, "Application directory should not be null");

        // Register main application
        using var mainAppKey = Registry.LocalMachine.CreateSubKey($@"{AppPathsBasePath}\WorldMapWallpaper.exe");
        if (mainAppKey != null)
        {
            mainAppKey.SetValue("", applicationPath, RegistryValueKind.String);
            mainAppKey.SetValue("Path", applicationDir, RegistryValueKind.String);
        }
        
        // Register settings application
        using var settingsAppKey = Registry.LocalMachine.CreateSubKey($@"{AppPathsBasePath}\WorldMapWallpaper.Settings.exe");
        if (settingsAppKey != null)
        {
            settingsAppKey.SetValue("", settingsPath, RegistryValueKind.String);
            settingsAppKey.SetValue("Path", applicationDir, RegistryValueKind.String);
        }
    }

    /// <summary>
    /// Configures Windows background access permissions for the application.
    /// This helps ensure the application can continue running and updating wallpapers when not in the foreground.
    /// </summary>
    /// <remarks>
    /// This method creates registry entries that indicate to Windows that the application
    /// should be allowed to run background tasks. These settings help prevent the application
    /// from being suspended or throttled when running background wallpaper updates.
    /// This registration is optional and failure does not prevent the application from functioning.
    /// </remarks>
    private static void EnableBackgroundAccess()
    {
        try
        {
            using var backgroundKey = Registry.CurrentUser.CreateSubKey($@"SOFTWARE\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications\{AppId}");
            if (backgroundKey != null)
            {
                backgroundKey.SetValue("Disabled", 0, RegistryValueKind.DWord);
                backgroundKey.SetValue("DisabledByUser", 0, RegistryValueKind.DWord);
            }
        }
        catch
        {
            // Background access registration is optional
        }
    }

    #endregion
}