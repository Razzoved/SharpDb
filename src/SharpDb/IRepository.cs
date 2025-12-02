namespace SharpDb;

/// <summary>
/// Contract based on 'repository' pattern for grouping operations
/// for a single entity behind an abstraction layer.
/// </summary>
/// <typeparam name="TEntity"></typeparam>
public interface IRepository<TEntity> where TEntity : class
{
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
