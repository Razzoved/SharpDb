using System.Diagnostics.CodeAnalysis;

namespace SharpDb;

/// <summary>
/// Represents an error that occurred during a database operation. Serves as a common interface
/// for all database error types in this library, allowing for consistent error handling and comparison.
/// </summary>
public interface IDbError
{
    string Message { get; }

    IDbError Prefix(string message) => Set(string.IsNullOrWhiteSpace(Message) ? message : (message + " - " + Message));
    IDbError Set(string message);
    Exception ToException() => new(Message);

    static bool AreEqual(IDbError? a, IDbError? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        return a.Equals(b) && b.Equals(a);
    }
}

/// <summary>
/// Represents a non-error state.
/// </summary>
public sealed class NoDbError : IDbError
{
    public static readonly NoDbError Instance = new();

    public string Message => string.Empty;

    IDbError IDbError.Prefix(string message) => throw new NotSupportedException(nameof(NoDbError) + " message cannot be prefixed");
    IDbError IDbError.Set(string message) => throw new NotSupportedException(nameof(NoDbError) + " message cannot be replaced");
    Exception IDbError.ToException() => throw new NotSupportedException(nameof(NoDbError) + " cannot be cast to an exception");

    public override bool Equals([NotNullWhen(true)] object? obj) => obj is NoDbError;
    public override int GetHashCode() => typeof(NoDbError).GetHashCode();
    public override string ToString() => Message;

    public static bool operator ==(NoDbError left, NoDbError right) => left.Equals(right);
    public static bool operator !=(NoDbError left, NoDbError right) => !(left == right);
}

/// <summary>
/// Represents a simple string-based error. Use whenever you don't need to
/// discern between different error types.
/// </summary>
public class StringDbError(string message) : IDbError
{
    public string Message { get; private set; } = message;

    public StringDbError Set(string message) { Message = message; return this; }
    IDbError IDbError.Set(string message) => Set(message);

    public override bool Equals([NotNullWhen(true)] object? obj) => obj is StringDbError other && Message == other.Message;
    public override int GetHashCode() => HashCode.Combine(typeof(StringDbError), Message);
    public override string ToString() => Message;

    public static bool operator ==(StringDbError left, StringDbError right) => left.Equals(right);
    public static bool operator !=(StringDbError left, StringDbError right) => !(left == right);
}

/// <summary>
/// Represents a simple exception-based error. Use whenever you don't need to
/// discern between different error types.
/// </summary>
public class ExceptionDbError(Exception exception) : IDbError
{
    public ExceptionDbError(Exception exception, string customMessage) : this(exception)
    {
        Message = customMessage;
        IsTransient = exception.HasTransientDbError();
    }

    public Exception Exception { get; } = exception;
    public string Message { get; private set; } = exception.Message;
    public bool IsTransient { get; private init; }

    public ExceptionDbError Set(string message) { Message = message; return this; }
    IDbError IDbError.Set(string message) => Set(message);
    Exception IDbError.ToException() => string.Equals(Exception.Message, Message) ? Exception : new(Message, Exception);

    public override bool Equals([NotNullWhen(true)] object? obj) => obj is ExceptionDbError other && Exception.Equals(other.Exception) && IsTransient == other.IsTransient;
    public override int GetHashCode() => HashCode.Combine(typeof(ExceptionDbError), Exception, IsTransient);
    public override string ToString() => $"{Message}{Environment.NewLine}{Exception.StackTrace}";

    public static bool operator ==(ExceptionDbError left, ExceptionDbError right) => left.Equals(right);
    public static bool operator !=(ExceptionDbError left, ExceptionDbError right) => !(left == right);
}
