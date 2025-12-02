namespace SharpDb.Services;

/// <summary>
/// Interface for a service that provides the current date and time.
/// </summary>
public interface IDateTimeService
{
    DateTimeOffset Now { get; }
    DateTimeOffset Today { get; }
}
