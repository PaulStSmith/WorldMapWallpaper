namespace WorldMapWallpaper.Shared;

/// <summary>
/// Defines the available update intervals for wallpaper generation.
/// Shorter intervals ensure the day/night cycle remains accurate.
/// </summary>
public enum UpdateInterval
{
    Every5Minutes,
    Every10Minutes,
    Every15Minutes,
    Every30Minutes,
    Hourly
}

/// <summary>
/// Extension methods for UpdateInterval enum.
/// </summary>
public static class UpdateIntervalExtensions
{
    /// <summary>
    /// Converts the UpdateInterval to a TimeSpan.
    /// </summary>
    /// <param name="interval">The update interval.</param>
    /// <returns>The corresponding TimeSpan.</returns>
    public static TimeSpan ToTimeSpan(this UpdateInterval interval) => interval switch
    {
        UpdateInterval.Every5Minutes => TimeSpan.FromMinutes(5),
        UpdateInterval.Every10Minutes => TimeSpan.FromMinutes(10),
        UpdateInterval.Every15Minutes => TimeSpan.FromMinutes(15),
        UpdateInterval.Every30Minutes => TimeSpan.FromMinutes(30),
        UpdateInterval.Hourly => TimeSpan.FromHours(1),
        _ => TimeSpan.FromHours(1)
    };

    /// <summary>
    /// Gets a user-friendly display name for the interval.
    /// </summary>
    /// <param name="interval">The update interval.</param>
    /// <returns>A formatted display string.</returns>
    public static string ToDisplayString(this UpdateInterval interval) => interval switch
    {
        UpdateInterval.Every5Minutes => "Every 5 minutes",
        UpdateInterval.Every10Minutes => "Every 10 minutes",
        UpdateInterval.Every15Minutes => "Every 15 minutes",
        UpdateInterval.Every30Minutes => "Every 30 minutes",
        UpdateInterval.Hourly => "Every hour",
        _ => "Every hour"
    };
}