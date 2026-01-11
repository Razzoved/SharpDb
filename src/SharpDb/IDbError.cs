using System.Diagnostics.CodeAnalysis;

namespace SharpDb;

public interface IDbError
{
    string Message { get; }

    public static bool AreEqual(IDbError? a, IDbError? b)
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
    public string Message { get; } = message;

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
    }

    public Exception Exception { get; } = exception;
    public string Message { get; } = exception.Message;

    public override bool Equals([NotNullWhen(true)] object? obj) => obj is ExceptionDbError other && Exception.Equals(other.Exception);
    public override int GetHashCode() => HashCode.Combine(typeof(ExceptionDbError), Exception);
    public override string ToString() => $"{Message}{Environment.NewLine}{Exception.StackTrace}";

    public static bool operator ==(ExceptionDbError left, ExceptionDbError right) => left.Equals(right);
    public static bool operator !=(ExceptionDbError left, ExceptionDbError right) => !(left == right);
}
