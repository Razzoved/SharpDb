using System.Runtime.CompilerServices;

namespace SharpDb;

/// <summary>
/// Represents the result of an action executed within a transaction.
/// Depending on the state, transaction may be committed or rolled back.
/// </summary>
public readonly struct ActionState
{
    private readonly IDbError? _error;

    private ActionState(IDbError? error) => _error = error;

    public bool IsAborted => _error is not null;
    public IDbError Error => _error ?? NoDbError.Instance;

    /// <summary>
    /// Creates a new instance of <see cref="ActionState"/> indicating successful completion.
    /// This state indicates that the transaction should be committed.
    /// </summary>
    public static ActionState Complete() => new(null);

    /// <summary>
    /// Creates a new instance of <see cref="ActionState"/> indicating failure.
    /// This state indicates that the transaction should be rolled back.
    /// </summary>
    /// <param name="error">The error associated with the failure.</param>
    public static ActionState Abort(IDbError error) => new(error is null or NoDbError ? new StringDbError("Unspecified error") : error);

    /// <summary>
    /// Creates a new instance of <see cref="ActionState"/> indicating failure.
    /// This state indicates that the transaction should be rolled back.
    /// </summary>
    /// <param name="message">A message to be associated with the error.</param>
    /// <param name="mn">The name of the method where the error occurred. Automatically provided by the compiler. Prepended to message.</param>
    /// <param name="ln">The line number where the error occurred. Automatically provided by the compiler. Prepended to message.</param>
    public static ActionState Abort(string message, [CallerMemberName] string? mn = null, [CallerLineNumber] int? ln = null) => new(new StringDbError($"{mn?.Trim() ?? ""}[{ln ?? 0}] {message.Trim()}"));

    /// <summary>
    /// Creates a new instance of <see cref="ActionState"/> indicating failure.
    /// This state indicates that the transaction should be rolled back.
    /// </summary>
    /// <param name="message">A message to be associated with the error. The message is passed as is.</param>
    public static ActionState AbortRaw(string message) => new(new StringDbError(message));
}
