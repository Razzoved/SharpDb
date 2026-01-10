using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage;

namespace SharpDb.EntityFrameworkCore;

public static class DatabaseFacadeExtensions
{
    public static int SqlQueryCommandTimeout { get; set; } = 600;
    public static int SqlExecuteCommandTimeout { get; set; } = 600;
    public static int SqlStoredProcedureTimeout { get; set; } = 600;

    public static ValueTask<DbQueryResult<T>> SqlSingleAsync<T>(this DatabaseFacade database, FormattableString sql, Func<DbDataReader, T> reader)
        => RawSqlSingleAsync(database, sql.GetSqlCommandText(), reader, sql.GetSqlCommandParameters());

    public static ValueTask<DbQueryResult<T[]>> SqlManyAsync<T>(this DatabaseFacade database, FormattableString sql, Func<DbDataReader, T> reader)
        => RawSqlManyAsync(database, sql.GetSqlCommandText(), reader, sql.GetSqlCommandParameters());

    public static ValueTask<DbExecResult> SqlExecuteAsync(this DatabaseFacade database, FormattableString sql)
        => RawSqlExecuteAsync(database, sql.GetSqlCommandText(), sql.GetSqlCommandParameters());

    public static async ValueTask<DbQueryResult<T>> RawSqlSingleAsync<T>(this DatabaseFacade database, string sql, Func<DbDataReader, T> reader, params DbParameter[] parameters)
    {
        DbQueryResult<T> result;
        try
        {
            result = await database.RunCommandAsync(async dbCommand =>
            {
                dbCommand.CommandType = System.Data.CommandType.Text;
                dbCommand.CommandTimeout = SqlQueryCommandTimeout;
                dbCommand.AddSqlCommandParameters(parameters);
                dbCommand.CommandText = sql;

                await TryConnect(dbCommand);

                await using var dbReader = await dbCommand.ExecuteReaderAsync();
                if (await dbReader.ReadAsync())
                {
                    var entity = reader(dbReader);
                    if (!await dbReader.ReadAsync())
                    {
                        if (TransactionContext.Transaction is not null && dbReader.RecordsAffected > 0)
                        {
                            TransactionContext.AddAffectedRows((uint)dbReader.RecordsAffected);
                        }
                        return DbQueryResult<T>.Success(entity);
                    }
                    return DbQueryResult<T>.Failure(new StringDbError(Resources.Text_Error_Sql_MoreThanOneRow));
                }
                return DbQueryResult<T>.Failure(new StringDbError(Resources.Text_Error_Sql_NoRows));
            });
        }
        catch (Exception e)
        {
            result = DbQueryResult<T>.Failure(new ExceptionDbError(e));
        }
        return result;
    }

    public static async ValueTask<DbQueryResult<T[]>> RawSqlManyAsync<T>(this DatabaseFacade database, string sql, Func<DbDataReader, T> reader, params DbParameter[] parameters)
    {
        DbQueryResult<T[]> result;
        try
        {
            result = await database.RunCommandAsync(async dbCommand =>
            {
                dbCommand.CommandType = System.Data.CommandType.Text;
                dbCommand.CommandTimeout = SqlQueryCommandTimeout;
                dbCommand.AddSqlCommandParameters(parameters);
                dbCommand.CommandText = sql;

                await TryConnect(dbCommand);

                await using var dbReader = await dbCommand.ExecuteReaderAsync();
                List<T> events = new(128);
                while (await dbReader.ReadAsync())
                {
                    var entity = reader(dbReader);
                    events.Add(entity);
                }
                if (TransactionContext.Transaction is not null && dbReader.RecordsAffected > 0)
                {
                    TransactionContext.AddAffectedRows((uint)dbReader.RecordsAffected);
                }

                return DbQueryResult<T[]>.Success([.. events]);
            });
        }
        catch (Exception e)
        {
            result = DbQueryResult<T[]>.Failure(new ExceptionDbError(e));
        }
        return result;
    }

    public static async ValueTask<DbExecResult> RawSqlExecuteAsync(this DatabaseFacade database, string sql, params DbParameter[] parameters)
    {
        DbExecResult result;
        try
        {
            result = await database.RunCommandAsync(async dbCommand =>
            {
                dbCommand.CommandType = System.Data.CommandType.Text;
                dbCommand.CommandTimeout = SqlExecuteCommandTimeout;
                dbCommand.AddSqlCommandParameters(parameters);
                dbCommand.CommandText = sql;

                await TryConnect(dbCommand);

                int affectedRows = await dbCommand.ExecuteNonQueryAsync();
                if (TransactionContext.Transaction is not null && affectedRows > 0)
                {
                    TransactionContext.AddAffectedRows((uint)affectedRows);
                }

                return DbExecResult.Success(affectedRows);
            });
        }
        catch (Exception e)
        {
            result = DbExecResult.Failure(new ExceptionDbError(e));
        }
        return result;
    }

    public static async ValueTask<DbQueryResult<T>> StoredProcedureSingleAsync<T>(this DatabaseFacade database, string procedureName, Func<DbDataReader, T> reader, params DbParameter[] parameters)
    {
        DbQueryResult<T> result;
        try
        {
            result = await database.RunCommandAsync(async dbCommand =>
            {
                dbCommand.CommandType = System.Data.CommandType.StoredProcedure;
                dbCommand.CommandTimeout = SqlStoredProcedureTimeout;
                dbCommand.AddSqlCommandParameters(parameters);
                dbCommand.CommandText = procedureName;

                await TryConnect(dbCommand);

                await using var dbReader = await dbCommand.ExecuteReaderAsync();
                if (await dbReader.ReadAsync())
                {
                    var entity = reader(dbReader);
                    if (!await dbReader.ReadAsync())
                    {
                        if (TransactionContext.Transaction is not null && dbReader.RecordsAffected > 0)
                        {
                            TransactionContext.AddAffectedRows((uint)dbReader.RecordsAffected);
                        }
                        return DbQueryResult<T>.Success(entity);
                    }
                    return DbQueryResult<T>.Failure(new StringDbError(Resources.Text_Error_Sql_MoreThanOneRow));
                }
                return DbQueryResult<T>.Failure(new StringDbError(Resources.Text_Error_Sql_NoRows));
            });
        }
        catch (Exception e)
        {
            result = DbQueryResult<T>.Failure(new ExceptionDbError(e));
        }
        return result;
    }

    public static async ValueTask<DbQueryResult<T[]>> StoredProcedureManyAsync<T>(this DatabaseFacade database, string procedureName, Func<DbDataReader, T> reader, params DbParameter[] parameters)
    {
        DbQueryResult<T[]> result;
        try
        {
            result = await database.RunCommandAsync(async dbCommand =>
            {
                dbCommand.CommandType = System.Data.CommandType.StoredProcedure;
                dbCommand.CommandTimeout = SqlStoredProcedureTimeout;
                dbCommand.AddSqlCommandParameters(parameters);
                dbCommand.CommandText = procedureName;

                await TryConnect(dbCommand);

                await using var dbReader = await dbCommand.ExecuteReaderAsync();
                List<T> events = new(128);
                while (await dbReader.ReadAsync())
                {
                    var entity = reader(dbReader);
                    events.Add(entity);
                }
                if (TransactionContext.Transaction is not null && dbReader.RecordsAffected > 0)
                {
                    TransactionContext.AddAffectedRows((uint)dbReader.RecordsAffected);
                }

                return DbQueryResult<T[]>.Success([.. events]);
            });
        }
        catch (Exception e)
        {
            result = DbQueryResult<T[]>.Failure(new ExceptionDbError(e));
        }
        return result;
    }

    public static async ValueTask<DbExecResult> StoredProcedureExecuteAsync(this DatabaseFacade database, string procedureName, params DbParameter[] parameters)
    {
        DbExecResult result;
        try
        {
            result = await database.RunCommandAsync(async dbCommand =>
            {
                dbCommand.CommandType = System.Data.CommandType.StoredProcedure;
                dbCommand.CommandTimeout = SqlStoredProcedureTimeout;
                dbCommand.AddSqlCommandParameters(parameters);
                dbCommand.CommandText = procedureName;

                await TryConnect(dbCommand);

                int affectedRows = await dbCommand.ExecuteNonQueryAsync();
                if (TransactionContext.Transaction is not null && affectedRows > 0)
                {
                    TransactionContext.AddAffectedRows((uint)affectedRows);
                }

                return DbExecResult.Success(affectedRows);
            });
        }
        catch (Exception e)
        {
            result = DbExecResult.Failure(new ExceptionDbError(e));
        }
        return result;
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
    /// <returns>Awaitable task</returns>
    /// <exception cref="InvalidOperationException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static async Task TryConnect(DbCommand command)
    {
        if (command.Connection is null)
            throw new InvalidOperationException(Resources.Text_Error_Command_MissingConnection);
        if (command.Connection.State != System.Data.ConnectionState.Open)
            await command.Connection.OpenAsync();
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

    internal static string GetSqlCommandText(this FormattableString sql)
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

            // Format parameter (or just add string if its not parameter)
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

    internal static DbParameter[] GetSqlCommandParameters(this FormattableString sql)
    {
        object?[] args = sql.GetArguments();
        DbParameter[] parameters = new DbParameter[args.Length];
        for (int i = 0; i < args.Length; i++)
        {
            parameters[i] = new($"@p{i}", args[i] ?? DBNull.Value);
        }
        return parameters;
    }

    private static async Task<TResult> RunCommandAsync<TResult>(this DatabaseFacade database, Func<DbCommand, Task<TResult>> commandAction)
    {
        await using var command = CreateCommand(database);
        if (database.CurrentTransaction is not null)
        {
            return await commandAction(command);
        }
        else
        {
            return await database.CreateExecutionStrategy().ExecuteAsync(command, commandAction);
        }
    }
}
