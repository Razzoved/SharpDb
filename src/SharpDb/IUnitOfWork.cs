namespace SharpDb;

/// <summary>
/// Contract based on 'unit of work' pattern for abstracting database transactions.
/// It is designed to be used in short-lived contexts only, for long-running tasks
/// manage instantiation and disposal cycles carefully to avoid memory or context leaks.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Saves all pending changes to the database.
    /// </summary>
    /// <returns>Count of affected rows</returns>
    /// <exception cref="Exception">If an error occurs during save</exception>
    int SaveChanges();

    /// <summary>
    /// Asynchronously saves all pending changes to the database.
    /// </summary>
    /// <returns>Count of affected rows</returns>
    /// <exception cref="Exception">If an error occurs during save</exception>
    ValueTask<int> SaveChangesAsync();

    /// <summary>
    /// Discards all pending changes. Has no effect on the database.
    /// </summary>
    void DiscardChanges();

    /// <summary>
    /// Runs the specified action as a single transaction.
    /// The implementation must:
    /// <br/>- prevent all exceptions
    /// <br/>- allow nested calls
    /// </summary>
    /// <param name="action">Action to be run</param>
    DbTransactionResult InTransaction(Action action);

    /// <summary>
    /// Runs the specified action as a single transaction.
    /// The implementation must:
    /// <br/>- prevent all exceptions
    /// <br/>- allow nested calls
    /// </summary>
    /// <param name="action">Action to be run with explicit result</param>
    DbTransactionResult InTransaction(Func<ActionState> action);

    /// <summary>
    /// Runs the specified action in a single asynchronous transaction.
    /// The implementation must:
    /// <br/>- prevent all exceptions
    /// <br/>- allow nested calls
    /// </summary>
    /// <param name="asyncAction">Action to be run</param>
    /// <returns>Awaitable transaction task</returns>
    ValueTask<DbTransactionResult> InTransactionAsync(Func<Task> asyncAction);

    /// <summary>
    /// Runs the specified action in a single asynchronous transaction.
    /// The implementation must:
    /// <br/>- prevent all exceptions
    /// <br/>- allow nested calls
    /// </summary>
    /// <param name="asyncAction">Action to be run with explicit result</param>
    /// <returns>Awaitable transaction task</returns>
    ValueTask<DbTransactionResult> InTransactionAsync(Func<Task<ActionState>> asyncAction);
}
