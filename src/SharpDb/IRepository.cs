namespace SharpDb;

/// <summary>
/// Contract based on 'repository' pattern for grouping operations behind an abstraction layer.
/// This non-generic version should be used as a marker on every repository implementation.
/// </summary>
public interface IRepository { }

/// <summary>
/// Contract based on 'repository' pattern for grouping operations
/// for a specified entity behind an abstraction layer.
/// </summary>
/// <typeparam name="TEntity"></typeparam>
public interface IRepository<in TEntity> : IRepository where TEntity : class
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
    /// <param name="entity">Single object instance to be attached</param>
    void Attach(TEntity entity);

    /// <summary>
    /// Removes the entity from change tracking, this has no effect on reference properties.
    /// Detaching an entity will prevent it from being added, updated or deleted when calling
    /// <see cref="IUnitOfWork.SaveChanges()"/> or equivalent methods. Calling this method
    /// invalidates all pending changes made to the entity.
    /// </summary>
    /// <remarks>
    /// Use this method whenever you need to manually clean up before starting another operation
    /// (e.g. in case of exception or when re-using the UoW for multiple unrelated operations).
    /// </remarks>
    /// <param name="entity">Single object instance to be detached</param>
    void Detach(TEntity entity);

    /// <summary>
    /// Marks the entity as a new entry, possibly including all of its configured navigations
    /// (cascades) as a change to be saved. No changes are made to the underlying database at this time.
    /// </summary>
    /// <remarks>!!! It's up to implementor to ensure correct cascading !!!</remarks>
    /// <param name="entity"></param>
    /// <param name="cascade">Whether to cascade changes on the object graph</param>
    void Add(TEntity entity, bool cascade = true);

    /// <summary>
    /// Marks the entity for update, possibly including its configured navigations (cascades)
    /// as a change to be saved. No changes are made to the underlying database at this time.
    /// </summary>
    /// <remarks>!!! It's up to implementor to ensure correct cascading !!!</remarks>
    /// <param name="entity">Object to be updated (values should be updated beforehand)</param>
    /// <param name="cascade">Whether to cascade changes on the object graph</param>
    void Update(TEntity entity, bool cascade = true);

    /// <summary>
    /// Marks the entity for removal, possibly including its configured navigations (cascades)
    /// as a change to be saved. No changes are made to the underlying database at this time.
    /// </summary>
    /// <remarks>!!! It's up to implementor to ensure correct cascading !!!</remarks>
    /// <param name="entity">Object to be deleted</param>
    /// <param name="cascade">Whether to cascade changes on the object graph</param>
    void Delete(TEntity entity, bool cascade = true);
}
