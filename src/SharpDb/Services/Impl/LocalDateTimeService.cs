namespace SharpDb.Services.Impl;

/// <summary>
/// DateTime service implementation that returns the local system date and time.
/// </summary>
public sealed class LocalDateTimeService : IDateTimeService
{
    public DateTimeOffset Now => DateTimeOffset.Now;

    public DateTimeOffset Today
    {
        get
        {
            DateTimeOffset now = DateTimeOffset.Now;
            DateTimeOffset today = new(now.Year, now.Month, now.Day, 0, 0, 0, now.Offset);
            return today;
        }
    }
}
