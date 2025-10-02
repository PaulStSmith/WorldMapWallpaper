using Microsoft.Win32.TaskScheduler;

namespace WorldMapWallpaper.Shared;

/// <summary>
/// Manages the Windows scheduled task for wallpaper updates using the TaskScheduler library.
/// </summary>
public static class TaskManager
{
    private const string TaskName = "World Map Wallpaper";

    /// <summary>
    /// Updates the scheduled task with a new update interval.
    /// Only modifies the TimeTrigger repetition interval while preserving 
    /// Boot, Logon, and Event triggers.
    /// </summary>
    /// <param name="interval">The new update interval.</param>
    /// <returns>True if the task was updated successfully, false otherwise.</returns>
    public static bool UpdateTaskSchedule(UpdateInterval interval)
    {
        try
        {
            using var ts = new TaskService();
            var task = ts.GetTask(TaskName);

            if (task == null)
                return false;

            var td = task.Definition;
            
            // Find the existing TimeTrigger and update its repetition interval
            var timeTrigger = td.Triggers.OfType<TimeTrigger>().FirstOrDefault();
            if (timeTrigger != null)
                timeTrigger.Repetition.Interval = interval.ToTimeSpan();
            else
            {
                // If no TimeTrigger exists, add one (shouldn't happen with proper installation)
                var newTimeTrigger = CreateTimeTrigger(interval.ToTimeSpan());
                td.Triggers.Add(newTimeTrigger);
            }

            // Re-register the task (preserves all other triggers)
            ts.RootFolder.RegisterTaskDefinition(TaskName, td);
            return true;
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
            using var ts = new TaskService();
            var task = ts.GetTask(TaskName);

            if (task == null)
                return false;

            task.Enabled = enable;
            return true;
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
            using var ts = new TaskService();
            var task = ts.GetTask(TaskName);
            return task?.Enabled == true;
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
        try
        {
            using var ts = new TaskService();
            var task = ts.GetTask(TaskName);

            if (task?.Definition.Triggers.OfType<TimeTrigger>().FirstOrDefault() is TimeTrigger trigger && 
                trigger.Repetition.Interval != TimeSpan.Zero)
            {
                var interval = trigger.Repetition.Interval;
                return interval.TotalMinutes switch
                {
                    5 => UpdateInterval.Every5Minutes,
                    10 => UpdateInterval.Every10Minutes,
                    15 => UpdateInterval.Every15Minutes,
                    30 => UpdateInterval.Every30Minutes,
                    60 => UpdateInterval.Hourly,
                    _ => UpdateInterval.Hourly
                };
            }
        }
        catch
        {
            // Fall through to default
        }

        return UpdateInterval.Hourly;
    }

    /// <summary>
    /// Runs the wallpaper update task immediately.
    /// </summary>
    /// <returns>True if the task was started successfully, false otherwise.</returns>
    public static bool RunTaskNow()
    {
        try
        {
            using var ts = new TaskService();
            var task = ts.GetTask(TaskName);
            task?.Run();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if the scheduled task exists.
    /// </summary>
    /// <returns>True if the task exists, false otherwise.</returns>
    public static bool TaskExists()
    {
        try
        {
            using var ts = new TaskService();
            var task = ts.GetTask(TaskName);
            return task != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets detailed information about the scheduled task.
    /// </summary>
    /// <returns>A tuple containing task state and next run time, or null if task doesn't exist.</returns>
    public static (TaskState State, DateTime? NextRunTime)? GetTaskInfo()
    {
        try
        {
            using var ts = new TaskService();
            var task = ts.GetTask(TaskName);
            
            if (task == null)
                return null;

            return (task.State, task.NextRunTime);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets information about all triggers configured for the task.
    /// </summary>
    /// <returns>A list of trigger descriptions, or empty list if task doesn't exist.</returns>
    public static List<string> GetTriggerInfo()
    {
        var triggerInfo = new List<string>();
        
        try
        {
            using var ts = new TaskService();
            var task = ts.GetTask(TaskName);
            
            if (task == null)
                return triggerInfo;

            foreach (var trigger in task.Definition.Triggers)
            {
                var description = trigger.TriggerType switch
                {
                    TaskTriggerType.Boot => "At system startup",
                    TaskTriggerType.Logon => "At user logon",
                    TaskTriggerType.Event => "On system wake from sleep",
                    TaskTriggerType.Time when trigger is TimeTrigger timeTrigger => 
                        $"Every {timeTrigger.Repetition.Interval.TotalMinutes} minutes",
                    _ => trigger.ToString()
                };
                
                triggerInfo.Add($"{description} (Enabled: {trigger.Enabled})");
            }
        }
        catch
        {
            // Return empty list on error
        }

        return triggerInfo;
    }

    /// <summary>
    /// Creates a time trigger with the specified repetition interval.
    /// </summary>
    /// <param name="interval">The repetition interval.</param>
    /// <returns>A configured TimeTrigger.</returns>
    private static TimeTrigger CreateTimeTrigger(TimeSpan interval)
    {
        var trigger = new TimeTrigger
        {
            StartBoundary = DateTime.Now.AddMinutes(1), // Start in 1 minute
            Repetition = new RepetitionPattern(interval, TimeSpan.Zero) // Repeat indefinitely
        };

        return trigger;
    }
}