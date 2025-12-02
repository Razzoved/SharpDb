using System.Diagnostics.CodeAnalysis;

namespace SharpDb;

public interface IDbError
{
    string Message { get; }
}

public static class DbError
{
    public static bool Equals(IDbError? a, IDbError? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        return a.Equals(b) && b.Equals(a);
    }
}

/// <summary>
/// Represents a non-error state.
/// </summary>
public readonly struct NoDbError : IDbError
{
    public static readonly NoDbError Instance = new();

    public string Message => string.Empty;

    public override bool Equals([NotNullWhen(true)] object? obj) => obj is NoDbError;
    public override int GetHashCode() => typeof(NoDbError).GetHashCode();
    public override string? ToString() => Message;

    public static bool operator ==(NoDbError left, NoDbError right) => left.Equals(right);
    public static bool operator !=(NoDbError left, NoDbError right) => !(left == right);
}

/// <summary>
/// Represents a simple string-based error. Use whenever you don't need to
/// discern between different error types.
/// </summary>
public readonly struct StringDbError : IDbError
{
    public StringDbError(string message)
    {
        Message = message;
    }

    public string Message { get; }

    public override bool Equals([NotNullWhen(true)] object? obj) => obj is StringDbError other && Message == other.Message;
    public override int GetHashCode() => HashCode.Combine(typeof(StringDbError), Message);
    public override string? ToString() => Message;

    public static bool operator ==(StringDbError left, StringDbError right) => left.Equals(right);
    public static bool operator !=(StringDbError left, StringDbError right) => !(left == right);
}

/// <summary>
/// Abstract base class for custom string-based errors. Use whenever you need to
/// discern between different error types by deriving from this class.
/// </summary>
public abstract class CustomStringDbError : IDbError
{
    public abstract string Message { get; }

    public override bool Equals(object? obj) => obj is CustomStringDbError other && Message == other.Message;
    public override int GetHashCode() => HashCode.Combine(typeof(CustomStringDbError), Message);
    public override string? ToString() => Message;

    public static bool operator ==(CustomStringDbError left, CustomStringDbError right) => left.Equals(right);
    public static bool operator !=(CustomStringDbError left, CustomStringDbError right) => !(left == right);
}



/// <summary>
/// Represents a simple exception-based error. Use whenever you don't need to
/// discern between different error types.
/// </summary>
public readonly struct ExceptionDbError : IDbError
{
    public ExceptionDbError(Exception exception, bool asBase = false)
    {
        Exception = exception;
        Message = asBase ? exception.GetBaseException().Message : exception.Message;
    }

    public ExceptionDbError(Exception exception, string customMessage)
    {
        Exception = exception;
        Message = customMessage;
    }

    public Exception Exception { get; }
    public string Message { get; }

    public override bool Equals([NotNullWhen(true)] object? obj) => obj is ExceptionDbError other && Exception.Equals(other.Exception);
    public override int GetHashCode() => HashCode.Combine(typeof(ExceptionDbError), Exception);
    public override string? ToString() => $"{Message}\n{Exception.StackTrace}";

    public static bool operator ==(ExceptionDbError left, ExceptionDbError right) => left.Equals(right);
    public static bool operator !=(ExceptionDbError left, ExceptionDbError right) => !(left == right);
}

/// <summary>
/// Abstract base class for custom exception-based errors. Use whenever you need to
/// discern between different error types by deriving from this class.
/// </summary>
public abstract class CustomExceptionDbError : IDbError
{
    public abstract Exception Exception { get; }
    public abstract string Message { get; }

    public override bool Equals(object? obj) => obj is CustomExceptionDbError other && Exception.Equals(other.Exception);
    public override int GetHashCode() => HashCode.Combine(typeof(CustomExceptionDbError), Exception);
    public override string? ToString() => $"{Message}\n{Exception.StackTrace}";

    public static bool operator ==(CustomExceptionDbError left, CustomExceptionDbError right) => left.Equals(right);
    public static bool operator !=(CustomExceptionDbError left, CustomExceptionDbError right) => !(left == right);
}

