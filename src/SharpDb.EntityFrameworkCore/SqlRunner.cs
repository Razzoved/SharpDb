using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace SharpDb.EntityFrameworkCore;

public sealed class SqlRunner : ISqlRunner
{
    private readonly DatabaseFacade _db;
    private DbConnectionInfo? _connectionInfo;

    public SqlRunner(DatabaseFacade db)
    {
        ArgumentNullException.ThrowIfNull(db, nameof(db));
        _db = db;
        _connectionInfo = null;
    }

    public DbConnectionInfo GetConnectionInfo()
    {
        if (!_connectionInfo.HasValue)
        {
            if (_db.GetConnectionString() is { } connectionString)
            {
                _connectionInfo = DbConnectionInfo.FromConnectionString(connectionString);
            }
            else
            {
                _connectionInfo = DbConnectionInfo.FromConnectionString("");
            }
        }
        return _connectionInfo.Value;
    }

    public ValueTask<DbQueryResult<T>> SingleAsync<T>(FormattableString sql, Func<DbDataReader, T> reader)
        => _db.SqlSingleAsync(sql, reader);

    public ValueTask<DbQueryResult<T>> RawSingleAsync<T>(string sql, Func<DbDataReader, T> reader, params DbParameter[] parameters)
        => _db.RawSqlSingleAsync(sql, reader, parameters);

    public ValueTask<DbQueryResult<T[]>> ManyAsync<T>(FormattableString sql, Func<DbDataReader, T> reader)
        => _db.SqlManyAsync(sql, reader);

    public ValueTask<DbQueryResult<T[]>> RawManyAsync<T>(string sql, Func<DbDataReader, T> reader, params DbParameter[] parameters)
        => _db.RawSqlManyAsync(sql, reader, parameters);

    public ValueTask<DbExecResult> ExecuteAsync(FormattableString sql)
        => _db.SqlExecuteAsync(sql);

    public ValueTask<DbExecResult> RawExecuteAsync(string sql, params DbParameter[] parameters)
        => _db.RawSqlExecuteAsync(sql, parameters);

    public ValueTask<DbQueryResult<T>> StoredProcedureSingleAsync<T>(string procedureName, Func<DbDataReader, T> reader, params DbParameter[] parameters)
        => _db.StoredProcedureSingleAsync(procedureName, reader, parameters);

    public ValueTask<DbQueryResult<T[]>> StoredProcedureManyAsync<T>(string procedureName, Func<DbDataReader, T> reader, params DbParameter[] parameters)
        => _db.StoredProcedureManyAsync(procedureName, reader, parameters);

    public ValueTask<DbExecResult> StoredProcedureExecuteAsync(string procedureName, params DbParameter[] parameters)
        => _db.StoredProcedureExecuteAsync(procedureName, parameters);
}
