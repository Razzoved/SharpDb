using System.Data.Common;
using Microsoft.EntityFrameworkCore.Infrastructure;
using SharpDb.EntityFrameworkCore.Queries;

namespace SharpDb.EntityFrameworkCore;

public static class DatabaseFacadeExtensions
{
    public static int SqlQueryCommandTimeout { get; set; } = 120;
    public static int SqlExecuteCommandTimeout { get; set; } = 120;
    public static int SqlStoredProcedureTimeout { get; set; } = 120;

    public static ValueTask<DbQueryResult<IReadOnlyList<T>>> SqlManyAsync<T>(this DatabaseFacade database, FormattableString sql, Func<DbDataReader, T> reader, CancellationToken cancellation = default)
        => AdoNetMethods.RawSqlManyAsync(database, sql.GetSqlCommandText(), reader, cancellation, SqlQueryCommandTimeout, sql.GetSqlCommandParameters());

    public static ValueTask<DbExecResult> SqlExecuteAsync(this DatabaseFacade database, FormattableString sql, CancellationToken cancellation = default)
        => AdoNetMethods.RawSqlExecuteAsync(database, sql.GetSqlCommandText(), cancellation, SqlExecuteCommandTimeout, sql.GetSqlCommandParameters());

    public static ValueTask<DbQueryResult<T>> RawSqlSingleAsync<T>(this DatabaseFacade database, string sql, Func<DbDataReader, T> reader, params DbParameter[] parameters)
        => AdoNetMethods.RawSqlSingleAsync(database, sql, reader, CancellationToken.None, SqlQueryCommandTimeout, parameters);

    public static ValueTask<DbQueryResult<T>> SqlSingleAsync<T>(this DatabaseFacade database, FormattableString sql, Func<DbDataReader, T> reader, CancellationToken cancellation = default)
        => AdoNetMethods.RawSqlSingleAsync(database, sql.GetSqlCommandText(), reader, cancellation, SqlQueryCommandTimeout, sql.GetSqlCommandParameters());

    public static ValueTask<DbQueryResult<T>> RawSqlSingleAsync<T>(this DatabaseFacade database, string sql, Func<DbDataReader, T> reader, CancellationToken cancellation, params DbParameter[] parameters)
        => AdoNetMethods.RawSqlSingleAsync(database, sql, reader, cancellation, SqlQueryCommandTimeout, parameters);

    public static ValueTask<DbQueryResult<T?>> SqlFirstOrDefaultAsync<T>(this DatabaseFacade database, FormattableString sql, Func<DbDataReader, T?> reader, CancellationToken cancellation = default)
        => AdoNetMethods.RawSqlFirstOrDefaultAsync(database, sql.GetSqlCommandText(), reader, cancellation, SqlQueryCommandTimeout, sql.GetSqlCommandParameters());

    public static ValueTask<DbQueryResult<T?>> RawSqlFirstOrDefaultAsync<T>(this DatabaseFacade database, string sql, Func<DbDataReader, T> reader, params DbParameter[] parameters)
        => AdoNetMethods.RawSqlFirstOrDefaultAsync(database, sql, reader, CancellationToken.None, SqlQueryCommandTimeout, parameters);

    public static ValueTask<DbQueryResult<T?>> RawSqlFirstOrDefaultAsync<T>(this DatabaseFacade database, string sql, Func<DbDataReader, T> reader, CancellationToken cancellation, params DbParameter[] parameters)
        => AdoNetMethods.RawSqlFirstOrDefaultAsync(database, sql, reader, cancellation, SqlQueryCommandTimeout, parameters);

    public static ValueTask<DbQueryResult<IReadOnlyList<T>>> RawSqlManyAsync<T>(this DatabaseFacade database, string sql, Func<DbDataReader, T> reader, params DbParameter[] parameters)
        => AdoNetMethods.RawSqlManyAsync(database, sql, reader, CancellationToken.None, SqlQueryCommandTimeout, parameters);

    public static ValueTask<DbQueryResult<IReadOnlyList<T>>> RawSqlManyAsync<T>(this DatabaseFacade database, string sql, Func<DbDataReader, T> reader, CancellationToken cancellation, params DbParameter[] parameters)
        => AdoNetMethods.RawSqlManyAsync(database, sql, reader, cancellation, SqlQueryCommandTimeout, parameters);

    public static ValueTask<DbExecResult> RawSqlExecuteAsync(this DatabaseFacade database, string sql, params DbParameter[] parameters)
        => AdoNetMethods.RawSqlExecuteAsync(database, sql, CancellationToken.None, SqlExecuteCommandTimeout, parameters);

    public static ValueTask<DbExecResult> RawSqlExecuteAsync(this DatabaseFacade database, string sql, CancellationToken cancellation, params DbParameter[] parameters)
        => AdoNetMethods.RawSqlExecuteAsync(database, sql, cancellation, SqlExecuteCommandTimeout, parameters);

    public static ValueTask<DbQueryResult<T>> StoredProcedureSingleAsync<T>(this DatabaseFacade database, string procedureName, Func<DbDataReader, T> reader, params DbParameter[] parameters)
        => AdoNetMethods.StoredProcedureSingleAsync(database, procedureName, reader, CancellationToken.None, SqlStoredProcedureTimeout, parameters);

    public static ValueTask<DbQueryResult<T>> StoredProcedureSingleAsync<T>(this DatabaseFacade database, string procedureName, Func<DbDataReader, T> reader, CancellationToken cancellation, params DbParameter[] parameters)
        => AdoNetMethods.StoredProcedureSingleAsync(database, procedureName, reader, cancellation, SqlStoredProcedureTimeout, parameters);

    public static ValueTask<DbQueryResult<T?>> StoredProcedureFirstOrDefaultAsync<T>(this DatabaseFacade database, string procedureName, Func<DbDataReader, T> reader, params DbParameter[] parameters)
        => AdoNetMethods.StoredProcedureFirstOrDefaultAsync(database, procedureName, reader, CancellationToken.None, SqlStoredProcedureTimeout, parameters);

    public static ValueTask<DbQueryResult<T?>> StoredProcedureFirstOrDefaultAsync<T>(this DatabaseFacade database, string procedureName, Func<DbDataReader, T> reader, CancellationToken cancellation, params DbParameter[] parameters)
        => AdoNetMethods.StoredProcedureFirstOrDefaultAsync(database, procedureName, reader, cancellation, SqlStoredProcedureTimeout, parameters);

    public static ValueTask<DbQueryResult<IReadOnlyList<T>>> StoredProcedureManyAsync<T>(this DatabaseFacade database, string procedureName, Func<DbDataReader, T> reader, params DbParameter[] parameters)
        => AdoNetMethods.StoredProcedureManyAsync(database, procedureName, reader, CancellationToken.None, SqlStoredProcedureTimeout, parameters);

    public static ValueTask<DbQueryResult<IReadOnlyList<T>>> StoredProcedureManyAsync<T>(this DatabaseFacade database, string procedureName, Func<DbDataReader, T> reader, CancellationToken cancellation, params DbParameter[] parameters)
        => AdoNetMethods.StoredProcedureManyAsync(database, procedureName, reader, cancellation, SqlStoredProcedureTimeout, parameters);

    public static ValueTask<DbExecResult> StoredProcedureExecuteAsync(this DatabaseFacade database, string procedureName, params DbParameter[] parameters)
        => AdoNetMethods.StoredProcedureExecuteAsync(database, procedureName, CancellationToken.None, SqlStoredProcedureTimeout, parameters);

    public static ValueTask<DbExecResult> StoredProcedureExecuteAsync(this DatabaseFacade database, string procedureName, CancellationToken cancellation, params DbParameter[] parameters)
        => AdoNetMethods.StoredProcedureExecuteAsync(database, procedureName, cancellation, SqlStoredProcedureTimeout, parameters);
}
