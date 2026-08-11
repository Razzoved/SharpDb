using System.Collections.Frozen;
using System.Data.Common;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using SharpDb.Exceptions;

namespace SharpDb.EntityFrameworkCore.Queries;

internal static class EfcMethods
{
    public static async ValueTask<DbQueryResult<T>> RawSqlSingleAsync<T>(DatabaseFacade database, string sql, Func<DbDataReader, T> reader, CancellationToken cancellation, int timeout = 0, params DbParameter[] parameters)
    {
        try
        {
            CommandHandle handle = new(database);
            (_, T data) = await handle.QueryAsync(
                commandText: sql,
                commandParameters: parameters,
                commandResultReader: reader,
                resultProcessor: static async enumerator =>
                {
                    if (!await enumerator.MoveNextAsync())
                        throw new InvalidDataException(Resources.Text_Error_Sql_NoRows);
                    var entity = enumerator.Current;
                    if (await enumerator.MoveNextAsync())
                        throw new InvalidDataException(Resources.Text_Error_Sql_MoreThanOneRow);
                    return entity;
                },
                commandTimeout: TimeSpan.FromSeconds(timeout),
                cancellation: cancellation);
            return DbQueryResult<T>.Success(data);
        }
        catch (TransactionTransientException)
        {
            throw;
        }
        catch (InvalidDataException e)
        {
            return DbQueryResult<T>.Failure(new StringDbError(e.Message));
        }
        catch (Exception e)
        {
            return DbQueryResult<T>.Failure(new ExceptionDbError(e));
        }
    }

    public static async ValueTask<DbQueryResult<T?>> RawSqlFirstOrDefaultAsync<T>(DatabaseFacade database, string sql, Func<DbDataReader, T> reader, CancellationToken cancellation, int timeout = 0, params DbParameter[] parameters)
    {
        try
        {
            CommandHandle handle = new(database);
            (_, T? data) = await handle.QueryAsync(
                commandText: sql,
                commandParameters: parameters,
                commandResultReader: reader,
                resultProcessor: static async enumerator => await enumerator.MoveNextAsync() ? enumerator.Current : default!,
                commandTimeout: TimeSpan.FromSeconds(timeout),
                cancellation: cancellation);
            return DbQueryResult<T?>.Success(data);
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

    public static async ValueTask<DbQueryResult<IReadOnlyList<T>>> RawSqlManyAsync<T>(DatabaseFacade database, string sql, Func<DbDataReader, T> reader, CancellationToken cancellation, int timeout = 0, params DbParameter[] parameters)
    {
        try
        {
            CommandHandle handle = new(database);
            (_, List<T> data) = await handle.QueryAsync(
                commandText: sql,
                commandParameters: parameters,
                commandResultReader: reader,
                resultProcessor: static async enumerator =>
                {
                    List<T> entities = new(8);
                    while (await enumerator.MoveNextAsync())
                        entities.Add(enumerator.Current);
                    return entities;
                },
                commandTimeout: TimeSpan.FromSeconds(timeout),
                cancellation: cancellation);
            return DbQueryResult<IReadOnlyList<T>>.Success(data);
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

    public static async ValueTask<DbExecResult> RawSqlExecuteAsync(DatabaseFacade database, string sql, CancellationToken cancellation, int timeout = 0, params DbParameter[] parameters)
    {
        try
        {
            CommandHandle handle = new(database);
            int affectedRows = await handle.ExecuteAsync(
                commandText: sql,
                commandParameters: parameters,
                commandTimeout: TimeSpan.FromSeconds(timeout),
                cancellation: cancellation);
            return DbExecResult.Success(affectedRows);
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

    public static async ValueTask<DbQueryResult<T>> StoredProcedureSingleAsync<T>(DatabaseFacade database, string procedureName, Func<DbDataReader, T> reader, CancellationToken cancellation, int timeout = 0, params DbParameter[] parameters)
    {
        try
        {
            string sql = CreateStoredProcedureSql(procedureName, parameters);
            return await RawSqlSingleAsync(database, sql, reader, cancellation, timeout, parameters);
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

    public static async ValueTask<DbQueryResult<T?>> StoredProcedureFirstOrDefaultAsync<T>(DatabaseFacade database, string procedureName, Func<DbDataReader, T> reader, CancellationToken cancellation, int timeout = 0, params DbParameter[] parameters)
    {
        try
        {
            string sql = CreateStoredProcedureSql(procedureName, parameters);
            return await RawSqlFirstOrDefaultAsync(database, sql, reader, cancellation, timeout, parameters);
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

    public static async ValueTask<DbQueryResult<IReadOnlyList<T>>> StoredProcedureManyAsync<T>(DatabaseFacade database, string procedureName, Func<DbDataReader, T> reader, CancellationToken cancellation, int timeout = 0, params DbParameter[] parameters)
    {
        try
        {
            string sql = CreateStoredProcedureSql(procedureName, parameters);
            return await RawSqlManyAsync(database, sql, reader, cancellation, timeout, parameters);
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

    public static async ValueTask<DbExecResult> StoredProcedureExecuteAsync(DatabaseFacade database, string procedureName, CancellationToken cancellation, int timeout = 0, params DbParameter[] parameters)
    {
        try
        {
            string sql = CreateStoredProcedureSql(procedureName, parameters);
            return await RawSqlExecuteAsync(database, sql, cancellation, timeout, parameters);
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

    private static string CreateStoredProcedureSql(string procedureName, DbParameter[] parameters)
    {
        StringBuilder sqlBuilder = new(procedureName.Length + parameters.Sum(x => x.Name.Length + 2));
        sqlBuilder.Append("exec");
        sqlBuilder.Append(' ').Append(procedureName);
        for (int i = 0; i < parameters.Length; i++)
        {
            if (i > 0) sqlBuilder.Append(',');
            sqlBuilder.Append(' ').Append('{').Append(parameters[i].Name).Append('}');
        }
        return sqlBuilder.Append(';').ToString();
    }

    private readonly record struct CommandHandle(DatabaseFacade Facade)
    {
        // AN: Later on we could implement variants with command renting when using a cached command.
        // See IRelationalConnection.RentCommand, IRelationalCommand.PopulateFrom, IRelationalCommandTemplate

        private readonly IRelationalConnection _connection =
            Facade.GetInfrastructure().GetRequiredService<IRelationalConnection>();
        private readonly DbContext? _context =
            Facade.GetInfrastructure().GetService<ICurrentDbContext>()?.Context;
        private readonly IRelationalCommandDiagnosticsLogger? _logger =
            Facade.GetInfrastructure().GetService<IRelationalCommandDiagnosticsLogger>();
        private readonly bool _detailedErrorsEnabled =
            Facade.GetInfrastructure().GetService<ICoreSingletonOptions>()?.AreDetailedErrorsEnabled ?? false;

        public async ValueTask<(int, TResult)> QueryAsync<TRead, TResult>(
            string commandText,
            IEnumerable<DbParameter>? commandParameters,
            Func<DbDataReader, TRead> commandResultReader,
            Func<IAsyncEnumerator<TRead>, ValueTask<TResult>> resultProcessor,
            TimeSpan commandTimeout = default,
            CancellationToken cancellation = default)
        {
            var commandParams = CreateCommandParameters(commandParameters, CommandSource.FromSqlQuery);
            var state = (
                command: CreateCommandTemplate(commandText, commandParams.ParameterValues),
                commandParams,
                timeout: commandTimeout,
                reader: commandResultReader,
                resultReader: resultProcessor);
            return await RunAsync(Facade, state, static async (s, ct) =>
            {
                // Prepare the command timeout
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                if (s.timeout > TimeSpan.Zero) cts.CancelAfter(s.timeout);
                // Read the result set
                await using var relationalReader = await s.command.ExecuteReaderAsync(s.commandParams, cts.Token);
                await using var readerEnumerator = CreateReaderAsyncEnumerator(relationalReader, s.reader, cts.Token);
                return (relationalReader.DbDataReader.RecordsAffected, await s.resultReader(readerEnumerator));
            }, cancellation).ConfigureAwait(false);
        }

        public async ValueTask<TResult?> ScalarAsync<TResult>(
            string commandText,
            IEnumerable<DbParameter>? commandParameters,
            TimeSpan commandTimeout = default,
            CancellationToken cancellation = default)
        {
            var commandParams = CreateCommandParameters(commandParameters, CommandSource.FromSqlQuery);
            var state = (
                command: CreateCommandTemplate(commandText, commandParams.ParameterValues),
                commandParams,
                timeout: commandTimeout);
            (_, TResult? result) = await RunAsync(Facade, state, static async (s, ct) =>
            {
                // Prepare the command timeout
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                if (s.timeout > TimeSpan.Zero) cts.CancelAfter(s.timeout);
                // Get the scalar value
                object? scalar = await s.command.ExecuteScalarAsync(s.commandParams, cts.Token);
                TResult? typedScalar = scalar is TResult result ? result : default;
                return (-1, typedScalar);
            }, cancellation).ConfigureAwait(false);
            return result;
        }

        public async ValueTask<int> ExecuteAsync(
            string commandText,
            IEnumerable<DbParameter>? commandParameters,
            TimeSpan commandTimeout = default,
            CancellationToken cancellation = default)
        {
            var commandParams = CreateCommandParameters(commandParameters, CommandSource.ExecuteSqlRaw);
            var state = (
                command: CreateCommandTemplate(commandText, commandParams.ParameterValues),
                commandParams,
                timeout: commandTimeout);
            (int affectedRows, _) = await RunAsync(Facade, state, static async (s, ct) =>
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                if (s.timeout > TimeSpan.Zero) cts.CancelAfter(s.timeout);
                return (await s.command.ExecuteNonQueryAsync(s.commandParams, cts.Token), (byte)0);
            }, cancellation).ConfigureAwait(false);
            return affectedRows;
        }

        private RelationalCommandParameterObject CreateCommandParameters(IEnumerable<DbParameter>? parameters, CommandSource commandSource)
        {
            return new RelationalCommandParameterObject(
                connection: _connection,
                parameterValues: parameters?.ToFrozenDictionary(p => p.Name, p => p.Value),
                readerColumns: null,
                context: _context,
                logger: _logger,
                detailedErrorsEnabled: _detailedErrorsEnabled,
                commandSource: commandSource);
        }

        private IRelationalCommand CreateCommandTemplate(string commandText, IReadOnlyDictionary<string, object?>? parameterValues)
        {
            var commandBuilder = Facade.GetInfrastructure().GetRequiredService<IRelationalCommandBuilderFactory>().Create().Append(commandText);
            if (parameterValues?.Count > 0)
            {
                var parameterNameGenerator = Facade.GetInfrastructure().GetRequiredService<IParameterNameGeneratorFactory>().Create();
                var typeMappingSource = Facade.GetInfrastructure().GetRequiredService<IRelationalTypeMappingSource>();
                foreach ((string invariantName, object? value) in parameterValues)
                {
                    commandBuilder.AddParameter(
                        invariantName: invariantName,
                        name: parameterNameGenerator.GenerateNext(),
                        nullable: value is null || value == DBNull.Value,
                        relationalTypeMapping: typeMappingSource.GetMappingForValue(value));
                }
            }
            return commandBuilder.Build();
        }

        private static async IAsyncEnumerator<TResult> CreateReaderAsyncEnumerator<TResult>(
            RelationalDataReader reader, Func<DbDataReader, TResult> read, CancellationToken cancellation = default)
        {
            while (await reader.ReadAsync(cancellation)) yield return read(reader.DbDataReader);
        }

        private static async ValueTask<(int, T)> RunAsync<T, TState>(
            DatabaseFacade facade, TState state, Func<TState, CancellationToken, Task<(int, T)>> operation, CancellationToken cancellation = default)
        {
            if (TransactionContext.GetCurrent(facade) is { } transactionContext)
            {
                try
                {
                    (int affectedRows, T result) = await operation(state, cancellation).ConfigureAwait(false);
                    if (affectedRows > 0) transactionContext.AddAffectedRows((uint)affectedRows);
                    return (affectedRows, result);
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
            var strategy = facade.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(state, operation, cancellation).ConfigureAwait(false);
        }
    }
}
