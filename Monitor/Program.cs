using WorldMapWallPaper.Monitor;

namespace WorldMapWallPaper.Monitor;

/// <summary>
/// Entry point for the World Map Wallpaper Monitor Service.
/// This service runs in the background to monitor wallpaper changes and manage task scheduling.
/// </summary>
public class Program
{
    /// <summary>
    /// Main entry point for the service application.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        
        // Configure the service to run as a Windows Service
        builder.Services.AddWindowsService(options =>
        {
            options.ServiceName = "WorldMapWallpaperMonitor";
        });

        // Add the wallpaper monitoring service
        builder.Services.AddHostedService<WallpaperMonitorService>();

        // Configure logging
        builder.Services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.AddEventLog(eventLogSettings =>
            {
                eventLogSettings.SourceName = "World Map Wallpaper Monitor";
                eventLogSettings.LogName = "Application";
            });
        });

        var host = builder.Build();

        // Handle command line arguments
        if (args.Length > 0)
        {
            var command = args[0].ToLowerInvariant();
            switch (command)
            {
                case "install":
                    InstallService();
                    return;
                case "uninstall":
                    UninstallService();
                    return;
                case "start":
                    StartService();
                    return;
                case "stop":
                    StopService();
                    return;
                case "--help":
                case "-h":
                case "help":
                    ShowHelp();
                    return;
            }
        }

        // Run the service
        try
        {
            host.Run();
        }
        catch (Exception ex)
        {
            // Log to event log if possible, otherwise console
            try
            {
                var logger = host.Services.GetService<ILogger<Program>>();
                logger?.LogCritical(ex, "Fatal error running World Map Wallpaper Monitor Service");
            }
            catch
            {
                Console.WriteLine($"Fatal error: {ex.Message}");
            }
            
            Environment.Exit(1);
        }
    }

    /// <summary>
    /// Installs the service using Windows Service Control Manager.
    /// </summary>
    private static void InstallService()
    {
        try
        {
            var exePath = Environment.ProcessPath ?? System.Reflection.Assembly.GetExecutingAssembly().Location;
            var serviceName = "WorldMapWallpaperMonitor";
            var displayName = "World Map Wallpaper Monitor";
            var description = "Monitors wallpaper changes and manages World Map Wallpaper automation";

            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = $"create \"{serviceName}\" binPath=\"{exePath}\" DisplayName=\"{displayName}\" start=auto",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(startInfo);
            if (process != null)
            {
                process.WaitForExit();
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();

                if (process.ExitCode == 0)
                {
                    Console.WriteLine($"Service '{serviceName}' installed successfully.");
                    
                    // Set description
                    var descStartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "sc.exe",
                        Arguments = $"description \"{serviceName}\" \"{description}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    
                    using var descProcess = System.Diagnostics.Process.Start(descStartInfo);
                    descProcess?.WaitForExit();
                }
                else
                {
                    Console.WriteLine($"Failed to install service: {error}");
                    Environment.Exit(1);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error installing service: {ex.Message}");
            Environment.Exit(1);
        }
    }

    /// <summary>
    /// Uninstalls the service using Windows Service Control Manager.
    /// </summary>
    private static void UninstallService()
    {
        try
        {
            var serviceName = "WorldMapWallpaperMonitor";

            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = $"delete \"{serviceName}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(startInfo);
            if (process != null)
            {
                process.WaitForExit();
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();

                if (process.ExitCode == 0)
                {
                    Console.WriteLine($"Service '{serviceName}' uninstalled successfully.");
                }
                else
                {
                    Console.WriteLine($"Failed to uninstall service: {error}");
                    Environment.Exit(1);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error uninstalling service: {ex.Message}");
            Environment.Exit(1);
        }
    }

    /// <summary>
    /// Starts the service using Windows Service Control Manager.
    /// </summary>
    private static void StartService()
    {
        try
        {
            var serviceName = "WorldMapWallpaperMonitor";

            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = $"start \"{serviceName}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(startInfo);
            if (process != null)
            {
                process.WaitForExit();
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();

                if (process.ExitCode == 0)
                {
                    Console.WriteLine($"Service '{serviceName}' started successfully.");
                }
                else
                {
                    Console.WriteLine($"Failed to start service: {error}");
                    Environment.Exit(1);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting service: {ex.Message}");
            Environment.Exit(1);
        }
    }

    /// <summary>
    /// Stops the service using Windows Service Control Manager.
    /// </summary>
    private static void StopService()
    {
        try
        {
            var serviceName = "WorldMapWallpaperMonitor";

            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = $"stop \"{serviceName}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(startInfo);
            if (process != null)
            {
                process.WaitForExit();
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();

                if (process.ExitCode == 0)
                {
                    Console.WriteLine($"Service '{serviceName}' stopped successfully.");
                }
                else
                {
                    Console.WriteLine($"Failed to stop service: {error}");
                    Environment.Exit(1);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error stopping service: {ex.Message}");
            Environment.Exit(1);
        }
    }

    /// <summary>
    /// Shows help information for the service commands.
    /// </summary>
    private static void ShowHelp()
    {
        Console.WriteLine("World Map Wallpaper Monitor Service");
        Console.WriteLine("===================================");
        Console.WriteLine();
        Console.WriteLine("Usage: WorldMapWallPaper.Monitor.exe [command]");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  install   - Install the service");
        Console.WriteLine("  uninstall - Uninstall the service");
        Console.WriteLine("  start     - Start the service");
        Console.WriteLine("  stop      - Stop the service");
        Console.WriteLine("  help      - Show this help message");
        Console.WriteLine();
        Console.WriteLine("When run without arguments, the service runs in console mode for debugging.");
        Console.WriteLine("When installed as a Windows service, it runs automatically in the background.");
    }
}
