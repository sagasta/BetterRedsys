namespace BetterRedsys.Models;

/// <summary>
/// Redsys SIS reports Ds_Date / Ds_Hour in Spanish peninsular time (Europe/Madrid), regardless of where the merchant server runs.
/// </summary>
internal static class RedsysTimeZone
{
    /// <summary>
    /// Europe/Madrid time zone, resolved by IANA id first and Windows id as fallback.
    /// Null when neither is available (e.g. invariant globalization without ICU or tzdata).
    /// </summary>
    public static TimeZoneInfo? Madrid { get; } = Resolve();

    /// <summary>
    /// Attaches the Europe/Madrid UTC offset to a wall-clock time received from Redsys.
    /// </summary>
    public static DateTimeOffset FromRedsysLocalTime(DateTime wallClock)
    {
        var unspecified = DateTime.SpecifyKind(wallClock, DateTimeKind.Unspecified);
        var offset = Madrid?.GetUtcOffset(unspecified) ?? TimeZoneInfo.Local.GetUtcOffset(unspecified);
        return new DateTimeOffset(unspecified, offset);
    }

    /// <summary>
    /// Current wall-clock time in Europe/Madrid, as Redsys would report it.
    /// </summary>
    public static DateTime Now() =>
        Madrid is null ? DateTime.Now : TimeZoneInfo.ConvertTime(DateTime.UtcNow, Madrid);

    private static TimeZoneInfo? Resolve()
    {
        foreach (var id in new[] { "Europe/Madrid", "Romance Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return null;
    }
}
