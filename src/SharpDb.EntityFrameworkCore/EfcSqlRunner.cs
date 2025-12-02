using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace SharpDb.EntityFrameworkCore;

public sealed class EfcSqlRunner : ISqlRunner
{
    private readonly DatabaseFacade _db;
    private DbConnectionInfo? _connectionInfo;

    public EfcSqlRunner(DatabaseFacade db)
    {
        ArgumentNullException.ThrowIfNull(db, nameof(db));
        _db = db;
        _connectionInfo = null;
    }

    public DbConnectionInfo? GetConnectionInfo()
    {
        if (_connectionInfo is null)
        {
            if (_db.GetConnectionString() is string connectionString)
            {
                _connectionInfo = DbConnectionInfo.FromConnectionString(connectionString);
            }
        }
        return _connectionInfo;
    }

    public ValueTask<DbQueryResult<T>> SqlSingleAsync<T>(FormattableString sql, Func<DbDataReader, T> reader)
        => _db.SqlSingleAsync(sql, reader);

    public ValueTask<DbQueryResult<T>> RawSqlSingleAsync<T>(string sql, Func<DbDataReader, T> reader, params DbParameter[] parameters)
        => _db.RawSqlSingleAsync(sql, reader, parameters);

    public ValueTask<DbQueryResult<T[]>> SqlManyAsync<T>(FormattableString sql, Func<DbDataReader, T> reader)
        => _db.SqlManyAsync(sql, reader);

    public ValueTask<DbQueryResult<T[]>> RawSqlManyAsync<T>(string sql, Func<DbDataReader, T> reader, params DbParameter[] parameters)
        => _db.RawSqlManyAsync(sql, reader, parameters);

    public ValueTask<DbExecResult> SqlExecuteAsync(FormattableString sql)
        => _db.SqlExecuteAsync(sql);

    public ValueTask<DbExecResult> RawSqlExecuteAsync(string sql, params DbParameter[] parameters)
        => _db.RawSqlExecuteAsync(sql, parameters);

    public ValueTask<DbQueryResult<T>> StoredProcedureSingleAsync<T>(string procedureName, Func<DbDataReader, T> reader, params DbParameter[] parameters)
        => _db.StoredProcedureSingleAsync(procedureName, reader, parameters);

    public ValueTask<DbQueryResult<T[]>> StoredProcedureManyAsync<T>(string procedureName, Func<DbDataReader, T> reader, params DbParameter[] parameters)
        => _db.StoredProcedureManyAsync(procedureName, reader, parameters);

    public ValueTask<DbExecResult> StoredProcedureExecuteAsync(string procedureName, params DbParameter[] parameters)
        => _db.StoredProcedureExecuteAsync(procedureName, parameters);
}
