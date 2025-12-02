namespace SharpDb;

public interface ISqlRunner
{
    /// <summary>
    /// Retrives information about the underlying database.
    /// Useful for raw sql operations.
    /// </summary>
    /// <returns>Info object or null if not available</returns>
    DbConnectionInfo? GetConnectionInfo();
}
