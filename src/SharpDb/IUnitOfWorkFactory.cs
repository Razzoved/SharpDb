namespace SharpDb;

/// <summary>
/// Contract for a factory that creates instances of <see cref="IUnitOfWork"/>.
/// </summary>
/// <typeparam name="T">Type of implementation</typeparam>
public interface IUnitOfWorkFactory<out T> where T : IUnitOfWork
{
    /// <summary>
    /// Creates a new instance of a specific unit of work type.
    /// </summary>
    /// <returns>New instance of T</returns>
    T Create();
}
