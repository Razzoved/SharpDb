using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using SharpDb.Exceptions;

namespace SharpDb.EntityFrameworkCore;

public static class DatabaseFacadeExtensions
{
    public static int SqlQueryCommandTimeout { get; set; } = 120;
    public static int SqlExecuteCommandTimeout { get; set; } = 120;
    public static int SqlStoredProcedureTimeout { get; set; } = 120;

    public static ValueTask<DbQueryResult<T>> SqlSingleAsync<T>(this DatabaseFacade database, FormattableString sql, Func<DbDataReader, T> reader, CancellationToken cancellation = default)
        => database.RawSqlSingleAsync(sql.GetSqlCommandText(), reader, cancellation, sql.GetSqlCommandParameters());

    public static ValueTask<DbQueryResult<T?>> SqlFirstOrDefaultAsync<T>(this DatabaseFacade database, FormattableString sql, Func<DbDataReader, T?> reader, CancellationToken cancellation = default)
        => database.RawSqlFirstOrDefaultAsync(sql.GetSqlCommandText(), reader, cancellation, sql.GetSqlCommandParameters());

    public static ValueTask<DbQueryResult<IReadOnlyList<T>>> SqlManyAsync<T>(this DatabaseFacade database, FormattableString sql, Func<DbDataReader, T> reader, CancellationToken cancellation = default)
        => database.RawSqlManyAsync(sql.GetSqlCommandText(), reader, cancellation, sql.GetSqlCommandParameters());

    public static ValueTask<DbExecResult> SqlExecuteAsync(this DatabaseFacade database, FormattableString sql, CancellationToken cancellation = default)
        => database.RawSqlExecuteAsync(sql.GetSqlCommandText(), cancellation, sql.GetSqlCommandParameters());

    public static ValueTask<DbQueryResult<T>> RawSqlSingleAsync<T>(this DatabaseFacade database, string sql, Func<DbDataReader, T> reader, params DbParameter[] parameters)
        => database.RawSqlSingleAsync(sql, reader, CancellationToken.None, parameters);

    public static async ValueTask<DbQueryResult<T>> RawSqlSingleAsync<T>(this DatabaseFacade database, string sql, Func<DbDataReader, T> reader, CancellationToken cancellation, params DbParameter[] parameters)
    {
        try
        {
            return await database.RunCommandAsync((sql, reader, parameters), async args =>
            {
                args.cmd.CommandType = System.Data.CommandType.Text;
                args.cmd.CommandTimeout = SqlQueryCommandTimeout;
                args.cmd.AddSqlCommandParameters(args.state.parameters);
                args.cmd.CommandText = args.state.sql;

                await TryConnectAsync(args.cmd, cancellation);

                await using var dbReader = await args.cmd.ExecuteReaderAsync(cancellation);
                if (await dbReader.ReadAsync(cancellation))
                {
                    var entity = args.state.reader(dbReader);
                    if (!await dbReader.ReadAsync(cancellation))
                    {
                        if (dbReader.RecordsAffected > 0 && TransactionContext.GetCurrent(args.db) is { } transactionContext)
                        {
                            transactionContext.AddAffectedRows((uint)dbReader.RecordsAffected);
                        }
                        return DbQueryResult<T>.Success(entity);
                    }
                    return DbQueryResult<T>.Failure(new StringDbError(Resources.Text_Error_Sql_MoreThanOneRow));
                }
                return DbQueryResult<T>.Failure(new StringDbError(Resources.Text_Error_Sql_NoRows));
            });
        }
        catch (TransactionTransientException)
        {
            throw;
        }
        catch (Exception e)
        {
            return DbQueryResult<T>.Failure(new ExceptionDbError(e));
        }
    }

    public static ValueTask<DbQueryResult<T?>> RawSqlFirstOrDefaultAsync<T>(this DatabaseFacade database, string sql, Func<DbDataReader, T> reader, params DbParameter[] parameters)
        => database.RawSqlFirstOrDefaultAsync(sql, reader, CancellationToken.None, parameters);

    public static async ValueTask<DbQueryResult<T?>> RawSqlFirstOrDefaultAsync<T>(this DatabaseFacade database, string sql, Func<DbDataReader, T> reader, CancellationToken cancellation, params DbParameter[] parameters)
    {
        try
        {
            return await database.RunCommandAsync((sql, reader, parameters), async args =>
            {
                args.cmd.CommandType = System.Data.CommandType.Text;
                args.cmd.CommandTimeout = SqlQueryCommandTimeout;
                args.cmd.AddSqlCommandParameters(args.state.parameters);
                args.cmd.CommandText = args.state.sql;

                await TryConnectAsync(args.cmd, cancellation);

                await using var dbReader = await args.cmd.ExecuteReaderAsync(cancellation);
                if (await dbReader.ReadAsync(cancellation))
                {
                    var entity = args.state.reader(dbReader);
                    if (dbReader.RecordsAffected > 0 && TransactionContext.GetCurrent(args.db) is { } transactionContext)
                    {
                        transactionContext.AddAffectedRows((uint)dbReader.RecordsAffected);
                    }
                    return DbQueryResult<T?>.Success(entity);
                }
                return DbQueryResult<T?>.Success(default);
            });
        }
        catch (TransactionTransientException)
        {
            throw;
        }
        catch (Exception e)
        {
            return DbQueryResult<T?>.Failure(new ExceptionDbError(e));
        }
    }

    public static ValueTask<DbQueryResult<IReadOnlyList<T>>> RawSqlManyAsync<T>(this DatabaseFacade database, string sql, Func<DbDataReader, T> reader, params DbParameter[] parameters)
        => database.RawSqlManyAsync(sql, reader, CancellationToken.None, parameters);

    public static async ValueTask<DbQueryResult<IReadOnlyList<T>>> RawSqlManyAsync<T>(this DatabaseFacade database, string sql, Func<DbDataReader, T> reader, CancellationToken cancellation, params DbParameter[] parameters)
    {
        try
        {
            return await database.RunCommandAsync((sql, reader, parameters), async args =>
            {
                args.cmd.CommandType = System.Data.CommandType.Text;
                args.cmd.CommandTimeout = SqlQueryCommandTimeout;
                args.cmd.AddSqlCommandParameters(args.state.parameters);
                args.cmd.CommandText = args.state.sql;

                await TryConnectAsync(args.cmd, cancellation);

                await using var dbReader = await args.cmd.ExecuteReaderAsync(cancellation);
                List<T> entities = new(128);
                while (await dbReader.ReadAsync(cancellation))
                {
                    var entity = args.state.reader(dbReader);
                    entities.Add(entity);
                }
                if (dbReader.RecordsAffected > 0 && TransactionContext.GetCurrent(args.db) is { } transactionContext)
                {
                    transactionContext.AddAffectedRows((uint)dbReader.RecordsAffected);
                }
                return DbQueryResult<IReadOnlyList<T>>.Success(entities);
            });
        }
        catch (TransactionTransientException)
        {
            throw;
        }
        catch (Exception e)
        {
            return DbQueryResult<IReadOnlyList<T>>.Failure(new ExceptionDbError(e));
        }
    }

    public static ValueTask<DbExecResult> RawSqlExecuteAsync(this DatabaseFacade database, string sql, params DbParameter[] parameters)
        => database.RawSqlExecuteAsync(sql, CancellationToken.None, parameters);

    public static async ValueTask<DbExecResult> RawSqlExecuteAsync(this DatabaseFacade database, string sql, CancellationToken cancellation = default, params DbParameter[] parameters)
    {
        try
        {
            return await database.RunCommandAsync((sql, parameters), async args =>
            {
                args.cmd.CommandType = System.Data.CommandType.Text;
                args.cmd.CommandTimeout = SqlExecuteCommandTimeout;
                args.cmd.AddSqlCommandParameters(args.state.parameters);
                args.cmd.CommandText = args.state.sql;

                await TryConnectAsync(args.cmd, cancellation);

                int affectedRows = await args.cmd.ExecuteNonQueryAsync(cancellation);
                if (affectedRows > 0 && TransactionContext.GetCurrent(args.db) is { } transactionContext)
                {
                    transactionContext.AddAffectedRows((uint)affectedRows);
                }
                return DbExecResult.Success(affectedRows);
            });
        }
        catch (TransactionTransientException)
        {
            throw;
        }
        catch (Exception e)
        {
            return DbExecResult.Failure(new ExceptionDbError(e));
        }
    }

    public static ValueTask<DbQueryResult<T>> StoredProcedureSingleAsync<T>(this DatabaseFacade database, string procedureName, Func<DbDataReader, T> reader, params DbParameter[] parameters)
        => database.StoredProcedureSingleAsync(procedureName, reader, CancellationToken.None, parameters);

    public static async ValueTask<DbQueryResult<T>> StoredProcedureSingleAsync<T>(this DatabaseFacade database, string procedureName, Func<DbDataReader, T> reader, CancellationToken cancellation, params DbParameter[] parameters)
    {
        try
        {
            return await database.RunCommandAsync((procedureName, reader, parameters), async args =>
            {
                args.cmd.CommandType = System.Data.CommandType.StoredProcedure;
                args.cmd.CommandTimeout = SqlStoredProcedureTimeout;
                args.cmd.AddSqlCommandParameters(args.state.parameters);
                args.cmd.CommandText = args.state.procedureName;

                await TryConnectAsync(args.cmd, cancellation);

                await using var dbReader = await args.cmd.ExecuteReaderAsync(cancellation);
                if (await dbReader.ReadAsync(cancellation))
                {
                    T entity = args.state.reader(dbReader);
                    if (!await dbReader.ReadAsync(cancellation))
                    {
                        if (dbReader.RecordsAffected > 0 && TransactionContext.GetCurrent(args.db) is { } transactionContext)
                        {
                            transactionContext.AddAffectedRows((uint)dbReader.RecordsAffected);
                        }
                        return DbQueryResult<T>.Success(entity);
                    }
                    return DbQueryResult<T>.Failure(new StringDbError(Resources.Text_Error_Sql_MoreThanOneRow));
                }
                return DbQueryResult<T>.Failure(new StringDbError(Resources.Text_Error_Sql_NoRows));
            });
        }
        catch (TransactionTransientException)
        {
            throw;
        }
        catch (Exception e)
        {
            return DbQueryResult<T>.Failure(new ExceptionDbError(e));
        }
    }

    public static ValueTask<DbQueryResult<T?>> StoredProcedureFirstOrDefaultAsync<T>(this DatabaseFacade database, string procedureName, Func<DbDataReader, T> reader, params DbParameter[] parameters)
        => database.StoredProcedureFirstOrDefaultAsync(procedureName, reader, CancellationToken.None, parameters);

    public static async ValueTask<DbQueryResult<T?>> StoredProcedureFirstOrDefaultAsync<T>(this DatabaseFacade database, string procedureName, Func<DbDataReader, T> reader, CancellationToken cancellation, params DbParameter[] parameters)
    {
        try
        {
            return await database.RunCommandAsync((procedureName, reader, parameters), async args =>
            {
                args.cmd.CommandType = System.Data.CommandType.StoredProcedure;
                args.cmd.CommandTimeout = SqlStoredProcedureTimeout;
                args.cmd.AddSqlCommandParameters(args.state.parameters);
                args.cmd.CommandText = args.state.procedureName;

                await TryConnectAsync(args.cmd, cancellation);

                await using var dbReader = await args.cmd.ExecuteReaderAsync(cancellation);
                if (await dbReader.ReadAsync(cancellation))
                {
                    var entity = args.state.reader(dbReader);
                    if (dbReader.RecordsAffected > 0 && TransactionContext.GetCurrent(args.db) is { } transactionContext)
                    {
                        transactionContext.AddAffectedRows((uint)dbReader.RecordsAffected);
                    }
                    return DbQueryResult<T?>.Success(entity);
                }
                return DbQueryResult<T?>.Success(default);
            });
        }
        catch (TransactionTransientException)
        {
            throw;
        }
        catch (Exception e)
        {
            return DbQueryResult<T?>.Failure(new ExceptionDbError(e));
        }
    }

    public static ValueTask<DbQueryResult<IReadOnlyList<T>>> StoredProcedureManyAsync<T>(this DatabaseFacade database, string procedureName, Func<DbDataReader, T> reader, params DbParameter[] parameters)
        => database.StoredProcedureManyAsync(procedureName, reader, CancellationToken.None, parameters);

    public static async ValueTask<DbQueryResult<IReadOnlyList<T>>> StoredProcedureManyAsync<T>(this DatabaseFacade database, string procedureName, Func<DbDataReader, T> reader, CancellationToken cancellation, params DbParameter[] parameters)
    {
        DbQueryResult<IReadOnlyList<T>> result;
        try
        {
            result = await database.RunCommandAsync((procedureName, reader, parameters), async args =>
            {
                args.cmd.CommandType = System.Data.CommandType.StoredProcedure;
                args.cmd.CommandTimeout = SqlStoredProcedureTimeout;
                args.cmd.AddSqlCommandParameters(args.state.parameters);
                args.cmd.CommandText = args.state.procedureName;

                await TryConnectAsync(args.cmd, cancellation);

                await using var dbReader = await args.cmd.ExecuteReaderAsync(cancellation);
                List<T> entities = new(128);
                while (await dbReader.ReadAsync(cancellation))
                {
                    T entity = args.state.reader(dbReader);
                    entities.Add(entity);
                }
                if (dbReader.RecordsAffected > 0 && TransactionContext.GetCurrent(args.db) is { } transactionContext)
                {
                    transactionContext.AddAffectedRows((uint)dbReader.RecordsAffected);
                }
                return DbQueryResult<IReadOnlyList<T>>.Success(entities);
            });
        }
        catch (TransactionTransientException)
        {
            throw;
        }
        catch (Exception e)
        {
            result = DbQueryResult<IReadOnlyList<T>>.Failure(new ExceptionDbError(e));
        }
        return result;
    }

    public static ValueTask<DbExecResult> StoredProcedureExecuteAsync(this DatabaseFacade database, string procedureName, params DbParameter[] parameters)
        => database.StoredProcedureExecuteAsync(procedureName, CancellationToken.None, parameters);

    public static async ValueTask<DbExecResult> StoredProcedureExecuteAsync(this DatabaseFacade database, string procedureName, CancellationToken cancellation, params DbParameter[] parameters)
    {
        try
        {
            return await database.RunCommandAsync((procedureName, parameters), async args =>
            {
                args.cmd.CommandType = System.Data.CommandType.StoredProcedure;
                args.cmd.CommandTimeout = SqlStoredProcedureTimeout;
                args.cmd.AddSqlCommandParameters(args.state.parameters);
                args.cmd.CommandText = args.state.procedureName;

                await TryConnectAsync(args.cmd, cancellation);

                int affectedRows = await args.cmd.ExecuteNonQueryAsync(cancellation);
                if (affectedRows > 0 && TransactionContext.GetCurrent(args.db) is { } transactionContext)
                {
                    transactionContext.AddAffectedRows((uint)affectedRows);
                }
                return DbExecResult.Success(affectedRows);
            });
        }
        catch (TransactionTransientException)
        {
            throw;
        }
        catch (Exception e)
        {
            return DbExecResult.Failure(new ExceptionDbError(e));
        }
    }

    public static string GetSqlCommandText(this FormattableString sql)
    {
        object?[] args = sql.GetArguments();
        if (args.Length == 0) return sql.Format;

        ReadOnlySpan<char> sqlSpan = sql.Format.AsSpan();
        StringBuilder sqlBuilder = new();
        int parameterIndex = 0;

        while (!sqlSpan.IsEmpty)
        {
            int index = sqlSpan.IndexOf('{');

            // Add everything before the next '{'
            if (index < 0)
            {
                sqlBuilder.Append(sqlSpan);
                sqlSpan = [];
                continue;
            }
            sqlBuilder.Append(sqlSpan[0..index]);
            sqlSpan = sqlSpan[index..];

            // Format parameter (or just add string if it's not parameter)
            if (!sqlSpan.StartsWith('{' + parameterIndex.ToString() + '}'))
            {
                sqlBuilder.Append('{');
                sqlSpan = sqlSpan[1..];
                continue;
            }
            sqlBuilder.Append($"@p{parameterIndex}");
            sqlSpan = sqlSpan[(2 + parameterIndex.ToString().Length)..];
            parameterIndex++;
        }

        return sqlBuilder.ToString();
    }

    public static DbParameter[] GetSqlCommandParameters(this FormattableString sql)
    {
        object?[] args = sql.GetArguments();
        DbParameter[] parameters = new DbParameter[args.Length];
        for (int i = 0; i < args.Length; i++)
        {
            parameters[i] = new DbParameter($"@p{i}", args[i] ?? DBNull.Value);
        }
        return parameters;
    }

    /// <summary>
    /// Creates a DbCommand associated with the given DatabaseFacade, taking into account any current transaction.
    /// The created command should always be disposed of by the caller.
    /// </summary>
    /// <param name="database">Database connector to use</param>
    /// <returns>New database command, possibly assigned to transaction</returns>
    /// <exception cref="InvalidOperationException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static DbCommand CreateCommand(DatabaseFacade database)
    {
        DbCommand command;

        if (database.CurrentTransaction?.GetDbTransaction() is { } transaction)
        {
            if (transaction.Connection is null)
                throw new InvalidOperationException(Resources.Text_Error_Transaction_MissingConnection);
            command = transaction.Connection.CreateCommand();
            command.Transaction = transaction;
        }
        else
        {
            command = database.GetDbConnection().CreateCommand();
        }

        return command;
    }

    /// <summary>
    /// Tries to open the connection associated with the given command, if it is not already open.
    /// </summary>
    /// <param name="command">Command from which the connection is sourced</param>
    /// <param name="cancellation">Cancellation token</param>
    /// <returns>Awaitable task</returns>
    /// <exception cref="InvalidOperationException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async Task TryConnectAsync(DbCommand command, CancellationToken cancellation = default)
    {
        if (command.Connection is null)
            throw new InvalidOperationException(Resources.Text_Error_Command_MissingConnection);
        if (command.Connection.State != System.Data.ConnectionState.Open)
            await command.Connection.OpenAsync(cancellation);
    }

    private static void AddSqlCommandParameters(this DbCommand command, params DbParameter[] parameters)
    {
        foreach (var p in parameters)
        {
            var param = command.CreateParameter();
            param.ParameterName = p.Name.StartsWith('@') ? p.Name : $"@{p.Name}";
            param.Value = p.Value ?? DBNull.Value;
            command.Parameters.Add(param);
        }
    }

    /// <summary>
    /// Executes a database command with the given state and returns the result.
    /// </summary>
    /// <returns>Result of the command execution</returns>
    /// <exception cref="TransactionTransientException">When a transient exception occurred during a transaction</exception>
    /// <exception cref="Exception"></exception>
    private static async Task<TResult> RunCommandAsync<TState, TResult>(
        this DatabaseFacade database,
        TState state,
        Func<(DatabaseFacade db, DbCommand cmd, TState state), Task<TResult>> commandAction)
        where TResult : IDbResult
    {
        if (database.CurrentTransaction is not null)
        {
            await using var command = CreateCommand(database);
            try
            {
                TResult? result = await commandAction((database, command, state));
                if (result is ExceptionDbError { IsTransient: true } err)
                    throw new TransactionTransientException(err.Message, err.Exception);
                return result;
            }
            catch (TransactionTransientException)
            {
                throw;
            }
            catch (Exception e) when (e.HasTransientDbError())
            {
                throw new TransactionTransientException(e);
            }
        }
        else
        {
            var strategy = database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync((database, state, commandAction), static async (args) =>
            {
                await using var command = CreateCommand(args.database);
                return await args.commandAction((args.database, command, args.state));
            });
        }
    }
}
