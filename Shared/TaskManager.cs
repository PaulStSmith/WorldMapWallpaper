using System.Diagnostics;

namespace WorldMapWallpaper.Shared;

/// <summary>
/// Manages the Windows scheduled task for wallpaper updates using schtasks command.
/// </summary>
public static class TaskManager
{
    private const string TaskName = "WorldMapWallpaperUpdate";

    /// <summary>
    /// Updates the scheduled task with a new update interval.
    /// </summary>
    /// <param name="interval">The new update interval.</param>
    /// <returns>True if the task was updated successfully, false otherwise.</returns>
    public static bool UpdateTaskSchedule(UpdateInterval interval)
    {
        try
        {
            var intervalText = interval switch
            {
                UpdateInterval.Every5Minutes => "5",
                UpdateInterval.Every10Minutes => "10", 
                UpdateInterval.Every15Minutes => "15",
                UpdateInterval.Every30Minutes => "30",
                UpdateInterval.Hourly => "60",
                _ => "60"
            };

            // Update the task schedule using schtasks command
            var startInfo = new ProcessStartInfo
            {
                FileName = "schtasks",
                Arguments = $"/Change /TN \"{TaskName}\" /RI {intervalText}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            process?.WaitForExit();
            return process?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Enables or disables the scheduled task.
    /// </summary>
    /// <param name="enable">True to enable the task, false to disable it.</param>
    /// <returns>True if the operation was successful, false otherwise.</returns>
    public static bool EnableTask(bool enable)
    {
        try
        {
            var action = enable ? "ENABLE" : "DISABLE";
            
            var startInfo = new ProcessStartInfo
            {
                FileName = "schtasks",
                Arguments = $"/Change /TN \"{TaskName}\" /{action}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            process?.WaitForExit();
            return process?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if the scheduled task exists and is enabled.
    /// </summary>
    /// <returns>True if the task exists and is enabled, false otherwise.</returns>
    public static bool IsTaskEnabled()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "schtasks",
                Arguments = $"/Query /TN \"{TaskName}\" /FO CSV /NH",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null) return false;
            
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            
            if (process.ExitCode != 0) return false;
            
            // Parse CSV output to check if task is enabled
            // Format: "TaskName","Next Run Time","Status","Logon Mode","Last Run Time","Last Result","Author","Task To Run","Start In","Comment","Scheduled Task State"
            var parts = output.Split(',');
            return parts.Length > 10 && parts[10].Trim('"') == "Enabled";
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the current update interval from the scheduled task.
    /// </summary>
    /// <returns>The current update interval, or Hourly if it can't be determined.</returns>
    public static UpdateInterval GetCurrentInterval()
    {
        // For simplicity, return the setting from registry
        // The actual task parsing would be more complex
        return Settings.UpdateInterval;
    }

    /// <summary>
    /// Runs the wallpaper update task immediately.
    /// </summary>
    /// <returns>True if the task was started successfully, false otherwise.</returns>
    public static bool RunTaskNow()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "schtasks",
                Arguments = $"/Run /TN \"{TaskName}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            process?.WaitForExit();
            return process?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}