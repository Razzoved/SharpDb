using System.Runtime.CompilerServices;

namespace SharpDb;

/// <summmary>
/// Represents the result of an action executed within a transaction.
/// Depending on the state, transaction may be committed or rolled back.
/// </summary>
public sealed class ActionState
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
    public static ActionState Abort(IDbError error) => new(error is null || error is NoDbError ? new StringDbError("Unspecified error") : error);

    /// <summary>
    /// Creates a new instance of <see cref="ActionState"/> indicating failure.
    /// This state indicates that the transaction should be rolled back.
    /// </summary>
    public static ActionState Abort(string message, [CallerMemberName] string? mn = null, [CallerLineNumber] int? ln = null) => new(new StringDbError($"{mn?.Trim() ?? ""}[{ln ?? 0}] {message.Trim()}"));
}
