namespace SharpDb.Services.Impl;

/// <summary>
/// DateTime service that provides UTC-based date and time values.
/// </summary>
public sealed class UtcDateTimeService : IDateTimeService
{
    public DateTimeOffset Now => DateTimeOffset.UtcNow;

    public DateTimeOffset Today
    {
        get
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset today = new(now.Year, now.Month, now.Day, 0, 0, 0, TimeSpan.Zero);
            return today;
        }
    }
}
