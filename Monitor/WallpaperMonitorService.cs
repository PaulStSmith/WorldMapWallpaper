using WorldMapWallpaper.Shared;

namespace WorldMapWallPaper.Monitor;

/// <summary>
/// Background service that monitors wallpaper changes and manages World Map Wallpaper automation.
/// Detects when users switch away from our wallpaper and disables the scheduled task.
/// </summary>
public class WallpaperMonitorService : BackgroundService
{
    private readonly ILogger<WallpaperMonitorService> _logger;
    private readonly WallpaperMonitor _wallpaperMonitor;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(5); // Check every 5 seconds
    private bool _isOurWallpaperActive = true;
    private DateTime _lastCheck = DateTime.MinValue;

    public WallpaperMonitorService(ILogger<WallpaperMonitorService> logger)
    {
        _logger = logger;
        _wallpaperMonitor = new WallpaperMonitor();
    }

    /// <summary>
    /// Executes the monitoring loop continuously while the service is running.
    /// </summary>
    /// <param name="stoppingToken">Token to signal service shutdown.</param>
    /// <returns>Task representing the monitoring operation.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("World Map Wallpaper Monitor Service started at {Time}", DateTimeOffset.Now);

        // Set up wallpaper change event handler
        _wallpaperMonitor.WallpaperChanged += OnWallpaperChanged;

        try
        {
            // Start the wallpaper monitor
            _wallpaperMonitor.Start();
            _logger.LogInformation("Wallpaper monitoring started - checking every {Interval} seconds", _checkInterval.TotalSeconds);

            // Initial state check
            await CheckInitialState();

            // Main monitoring loop
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PerformPeriodicCheck();
                    await Task.Delay(_checkInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during periodic wallpaper check");
                    // Continue monitoring despite errors
                    await Task.Delay(_checkInterval, stoppingToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in wallpaper monitoring service");
            throw;
        }
        finally
        {
            // Clean up resources
            _wallpaperMonitor.WallpaperChanged -= OnWallpaperChanged;
            _wallpaperMonitor.Stop();
            _wallpaperMonitor.Dispose();
            _logger.LogInformation("World Map Wallpaper Monitor Service stopped at {Time}", DateTimeOffset.Now);
        }
    }

    /// <summary>
    /// Checks the initial state when the service starts.
    /// </summary>
    private async Task CheckInitialState()
    {
        try
        {
            var isActive = WallpaperMonitor.IsCurrentlyActive();
            var taskExists = TaskManager.TaskExists();
            var taskEnabled = TaskManager.IsTaskEnabled();

            _logger.LogInformation("Initial state check - Wallpaper active: {WallpaperActive}, Task exists: {TaskExists}, Task enabled: {TaskEnabled}", 
                isActive, taskExists, taskEnabled);

            // If our wallpaper is not active but the task is still enabled, disable it
            if (!isActive && taskEnabled)
            {
                _logger.LogInformation("Our wallpaper is not active but task is enabled - disabling task");
                var disabled = TaskManager.EnableTask(false);
                if (disabled)
                {
                    Settings.IsActive = false;
                    _logger.LogInformation("Successfully disabled scheduled task - user has switched to different wallpaper");
                }
                else
                {
                    _logger.LogWarning("Failed to disable scheduled task");
                }
            }

            _isOurWallpaperActive = isActive;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during initial state check");
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Performs periodic checks to ensure the service is functioning correctly.
    /// </summary>
    private async Task PerformPeriodicCheck()
    {
        var now = DateTime.UtcNow;
        
        // Log status every 5 minutes
        if (now - _lastCheck >= TimeSpan.FromMinutes(5))
        {
            try
            {
                var currentWallpaper = WallpaperMonitor.GetCurrentWallpaperPath();
                var isActive = WallpaperMonitor.IsCurrentlyActive();
                var taskInfo = TaskManager.GetTaskInfo();
                
                _logger.LogInformation("Status check - Our wallpaper active: {IsActive}, Current wallpaper: {CurrentWallpaper}, Task state: {TaskState}", 
                    isActive, 
                    Path.GetFileName(currentWallpaper), 
                    taskInfo?.State.ToString() ?? "Unknown");

                _lastCheck = now;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during periodic status check");
            }
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Handles wallpaper change events from the monitor.
    /// </summary>
    /// <param name="isOurWallpaper">True if our wallpaper is currently active, false otherwise.</param>
    private void OnWallpaperChanged(bool isOurWallpaper)
    {
        try
        {
            // State change from our wallpaper to something else
            if (_isOurWallpaperActive && !isOurWallpaper)
            {
                _logger.LogInformation("User switched away from World Map Wallpaper - disabling scheduled task");
                
                var disabled = TaskManager.EnableTask(false);
                if (disabled)
                {
                    Settings.IsActive = false;
                    _logger.LogInformation("Successfully disabled scheduled task - respecting user's wallpaper choice");
                }
                else
                {
                    _logger.LogWarning("Failed to disable scheduled task after wallpaper change");
                }
            }
            // State change from other wallpaper back to ours
            else if (!_isOurWallpaperActive && isOurWallpaper)
            {
                _logger.LogInformation("User switched back to World Map Wallpaper");
                
                // Don't automatically re-enable the task - let the user decide via settings
                // This prevents unwanted automation restart
            }

            _isOurWallpaperActive = isOurWallpaper;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling wallpaper change event");
        }
    }

    /// <summary>
    /// Handles service shutdown gracefully.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for shutdown.</param>
    /// <returns>Task representing the shutdown operation.</returns>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("World Map Wallpaper Monitor Service is stopping...");
        await base.StopAsync(cancellationToken);
    }
}