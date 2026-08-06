using System.Data.Common;

namespace SharpDb;

public interface ISqlRunner
{
    /// <summary>
    /// Retrieves information about the underlying database.
    /// Useful for raw SQL operations.
    /// </summary>
    /// <returns>Info object</returns>
    DbConnectionInfo GetConnectionInfo();

    ValueTask<DbQueryResult<T>> SingleAsync<T>(FormattableString sql, Func<DbDataReader, T> reader, CancellationToken cancellation = default);
    ValueTask<DbQueryResult<T>> RawSingleAsync<T>(string sql, Func<DbDataReader, T> reader, CancellationToken cancellation, params DbParameter[] parameters);

    ValueTask<DbQueryResult<T?>> FirstOrDefaultAsync<T>(FormattableString sql, Func<DbDataReader, T> reader, CancellationToken cancellation = default);
    ValueTask<DbQueryResult<T?>> RawFirstOrDefaultAsync<T>(string sql, Func<DbDataReader, T> reader, CancellationToken cancellation, params DbParameter[] parameters);

    ValueTask<DbQueryResult<IReadOnlyList<T>>> ManyAsync<T>(FormattableString sql, Func<DbDataReader, T> reader, CancellationToken cancellation = default);
    ValueTask<DbQueryResult<IReadOnlyList<T>>> RawManyAsync<T>(string sql, Func<DbDataReader, T> reader, CancellationToken cancellation, params DbParameter[] parameters);

    ValueTask<DbExecResult> ExecuteAsync(FormattableString sql, CancellationToken cancellation = default);
    ValueTask<DbExecResult> RawExecuteAsync(string sql, CancellationToken cancellation, params DbParameter[] parameters);
}
