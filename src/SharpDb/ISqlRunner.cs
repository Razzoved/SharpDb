namespace SharpDb;

public interface ISqlRunner
{
    /// <summary>
    /// Retrieves information about the underlying database.
    /// Useful for raw SQL operations.
    /// </summary>
    /// <returns>Info object</returns>
    DbConnectionInfo GetConnectionInfo();
}
