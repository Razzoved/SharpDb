namespace SharpDb;

/// <summary>
/// Contract based on 'unit of work' pattern for abstracting database transactions.
/// It is designed to be used in short-lived contexts only, for long-running tasks
/// manage instantiation and disposal cycles carefully to avoid memory or context leaks.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Adds the entity to change tracking, this has no effect when the entity is already
    /// being tracked. Only the given entity is attached, this method ignores reference
    /// properties. Attaching an entity may prevent it from being marked as added or updated
    /// when calling Add or Update on related entities, in which case you may need to
    /// call the operation on the entity explicitly.
    /// </summary>
    /// <remarks>
    /// Use this method whenever you need to update a root entity that was (previously)
    /// loaded without being added to change tracker (e.g. via non-tracking queries or raw SQL).
    /// </remarks>
    /// <typeparam name="TEntity">Type of the entity, must be accessible by UoW</typeparam>
    /// <param name="entity">Single object to be attached</param>
    void Attach<TEntity>(TEntity entity) where TEntity : class;

    /// <summary>
    /// Removes the entity from change tracking, this has no effect on reference properties.
    /// Detaching an entity will prevent it from being added, updated or deleted when calling
    /// <see cref="IUnitOfWork.SaveChanges()"/> or equivalent methods.
    /// </summary>
    /// <remarks>
    /// Use this method whenever you need to manually clean up before starting another operation
    /// (e.g. in case of exception or when re-using the UoW for multiple unrelated operations).
    /// </remarks>
    /// <typeparam name="TEntity">Type of the entity, must be accesible by UoW</typeparam>
    /// <param name="entity">Single object to be detached</param>
    void Detach<TEntity>(TEntity entity) where TEntity : class;

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
    /// Runs the specified action in a single asynchronous transaction.
    /// The implementation must:
    /// <br/>- prevent all exceptions
    /// <br/>- allow nested calls
    /// </summary>
    /// <param name="asyncAction">Action to be run</param>
    /// <returns>Awaitable transaction task</returns>
    ValueTask<DbTransactionResult> InTransactionAsync(Func<Task> asyncAction);
}
