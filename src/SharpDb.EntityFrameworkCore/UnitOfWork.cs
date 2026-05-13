using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.EntityFrameworkCore;
using SharpDb.Exceptions;
using SharpDb.Extensions;

namespace SharpDb.EntityFrameworkCore;

/// <summary>
/// Part of an abstraction layer over the database implementation.
/// Use this to hide DB implementation details from users, and to
/// facilitate transactional operations even between repositories
/// or database commands.
/// </summary>
/// <param name="dbContextFactory">Database context factory used to create a new owned context</param>
public abstract class UnitOfWork<TContext>(IDbContextFactory<TContext> dbContextFactory) : IUnitOfWork where TContext : DbContext
{
    private readonly Dictionary<int, object> _loadedRepositories = new(3);
    private readonly object _loadedRepositoriesLock = new();
    private SqlRunner? _sql;
    private bool _disposed;

    ~UnitOfWork()
    {
        Dispose(disposing: false);
    }

    /// <summary>
    /// This property can be used to execute SQL operations directly
    /// on the underlying database.
    /// </summary>
    public SqlRunner Sql => _sql ??= new SqlRunner(DbContext.Database);

    /// <summary>
    /// This property can be used to access the underlying database context.
    /// </summary>
    protected TContext DbContext { get; } = dbContextFactory.CreateDbContext();

    public DbTransactionResult SaveChanges()
    {
        TransactionContext? transactionContext = TransactionContext.GetCurrent(DbContext.Database);
        try
        {
            int affectedRows = DbContext.SaveChanges();
            if (affectedRows > 0 && transactionContext is not null)
                transactionContext.AddAffectedRows((uint)affectedRows);
            return DbTransactionResult.Success(affectedRows);
        }
        catch (Exception e) when (transactionContext is not null)
        {
            if (e is DbUpdateException { InnerException: { } innerEx })
                e = innerEx;
            if (e.HasTransientDbError())
                throw e;
            return DbTransactionResult.Failure(new ExceptionDbError(e));
        }
        catch (Exception e)
        {
            if (e is DbUpdateException { InnerException: { } innerEx })
                e = innerEx;
            return DbTransactionResult.Failure(new ExceptionDbError(e));
        }
    }

    public async ValueTask<DbTransactionResult> SaveChangesAsync()
    {
        TransactionContext? transactionContext = TransactionContext.GetCurrent(DbContext.Database);
        try
        {
            int affectedRows = await DbContext.SaveChangesAsync();
            if (affectedRows > 0 && transactionContext is not null)
                transactionContext.AddAffectedRows((uint)affectedRows);
            return DbTransactionResult.Success(affectedRows);
        }
        catch (Exception e) when (transactionContext is not null)
        {
            if (e is DbUpdateException { InnerException: { } innerEx })
                e = innerEx;
            if (e.HasTransientDbError())
                throw e;
            return DbTransactionResult.Failure(new ExceptionDbError(e));
        }
        catch (Exception e)
        {
            if (e is DbUpdateException { InnerException: { } innerEx })
                e = innerEx;
            return DbTransactionResult.Failure(new ExceptionDbError(e));
        }
    }

    public void DiscardChanges()
    {
        if (TransactionContext.GetCurrent(DbContext.Database) is { } transactionContext)
        {
            transactionContext.ChangeJournal.Restore();
        }
        else
        {
            DbContext.ChangeTracker.Clear();
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Take care when using any non-efc logic, such as adding to lists, since the operations might be
    /// repeated several times when the transaction is retried. Throws an exception if the call
    /// is nested inside an active transaction and the exception itself is caused by transient error.
    /// </remarks>
    /// <exception cref="DbException">When transient error occurs in a nested call</exception>
    public DbTransactionResult InTransaction(Func<ActionState> action)
    {
        if (DbContext.Database.CurrentTransaction is not { } transaction)
        {
            var strategy = DbContext.Database.CreateExecutionStrategy();
            try
            {
                uint affectedRows = strategy.Execute((DbContext, action), actionContext =>
                {
                    using var newTransaction = actionContext.DbContext.Database.BeginTransaction();
                    using var newTransactionContext = new TransactionContext(actionContext.DbContext);

                    ActionState state;
                    try
                    {
                        state = actionContext.action();
                        if (!state.IsAborted)
                        {
                            newTransaction.Commit();
                            return newTransactionContext.AffectedRows;
                        }
                    }
                    catch (Exception e)
                    {
                        state = ActionState.Abort(new ExceptionDbError(e));
                    }

                    try { newTransaction.Rollback(); }
                    catch (Exception e) { throw new DbRollbackException(state.Error.Message, e); }
                    finally { newTransactionContext.Rollback(); }

                    if (state.Error is ExceptionDbError exError)
                        throw exError.Exception;
                    throw new InvalidOperationException(state.Error.Message);
                });
                return DbTransactionResult.Success(affectedRows);
            }
            catch (Exception e)
            {
                return DbTransactionResult.Failure(new ExceptionDbError(e));
            }
        }
        else if (transaction.SupportsSavepoints)
        {
            using var transactionContext = new TransactionContext(DbContext);
            string savepoint = Guid.NewGuid().ToString("N");
            transaction.CreateSavepoint(savepoint);
            try
            {
                ActionState state;
                try
                {
                    state = action();
                    if (!state.IsAborted)
                    {
                        uint affectedRows = transactionContext.AffectedRows;
                        return DbTransactionResult.Success(affectedRows);
                    }
                }
                catch (Exception e)
                {
                    state = ActionState.Abort(new ExceptionDbError(e));
                }

                try { transaction.RollbackToSavepoint(savepoint); }
                catch (Exception e) { throw new DbRollbackException(state.Error.Message, e); }
                finally { transactionContext.Rollback(); }

                if (state.Error.IsTransient)
                    throw new DbTransientException(state.Error);
                return DbTransactionResult.Failure(state.Error);
            }
            finally
            {
                try { transaction.ReleaseSavepoint(savepoint); } catch { }
            }
        }
        else
        {
            ActionState state = action();
            if (state.IsAborted)
            {
                if (state.Error.IsTransient)
                    throw new DbTransientException(state.Error);
                return DbTransactionResult.Failure(state.Error);
            }
            uint affectedRows = TransactionContext.GetCurrent(DbContext.Database)?.AffectedRows ?? 0;
            return DbTransactionResult.Success(affectedRows);
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Take care when using any non-efc logic, such as adding to lists, since the operations might be
    /// repeated several times when the transaction is retried. Throws an exception if the call
    /// is nested inside an active transaction and the exception itself is caused by transient error.
    /// </remarks>
    /// <exception cref="DbException">When transient error occurs in a nested call</exception>
    public DbTransactionResult InTransaction(Action action)
    {
        return InTransaction(WrappedAction);

        ActionState WrappedAction()
        {
            action();
            return ActionState.Complete();
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Take care when using any non-efc logic, such as adding to lists, since the operations might be
    /// repeated several times when the transaction is retried. Throws an exception if the call
    /// is nested inside an active transaction and the exception itself is caused by transient error.
    /// </remarks>
    /// <exception cref="DbException">When transient error occurs in a nested call</exception>
    public async ValueTask<DbTransactionResult> InTransactionAsync(Func<Task<ActionState>> asyncAction)
    {
        if (DbContext.Database.CurrentTransaction is not { } transaction)
        {
            var strategy = DbContext.Database.CreateExecutionStrategy();
            try
            {
                uint affectedRows = await strategy.ExecuteAsync((DbContext, asyncAction), async actionContext =>
                {
                    await using var newTransaction = await actionContext.DbContext.Database.BeginTransactionAsync();
                    using var newTransactionContext = new TransactionContext(actionContext.DbContext);

                    ActionState state;
                    try
                    {
                        state = await actionContext.asyncAction();
                        if (!state.IsAborted)
                        {
                            await newTransaction.CommitAsync();
                            return newTransactionContext.AffectedRows;
                        }
                    }
                    catch (Exception e)
                    {
                        state = ActionState.Abort(new ExceptionDbError(e));
                    }

                    try { await newTransaction.RollbackAsync(); }
                    catch (Exception e) { throw new DbRollbackException(state.Error.Message, e); }
                    finally { newTransactionContext.Rollback(); }

                    if (state.Error is ExceptionDbError exError)
                        throw exError.Exception;
                    throw new InvalidOperationException(state.Error.Message);
                });
                return DbTransactionResult.Success(affectedRows);
            }
            catch (Exception e)
            {
                return DbTransactionResult.Failure(new ExceptionDbError(e));
            }
        }
        else if (transaction.SupportsSavepoints)
        {
            using var transactionContext = new TransactionContext(DbContext);
            string savepoint = Guid.NewGuid().ToString("N");
            await transaction.CreateSavepointAsync(savepoint);
            try
            {
                ActionState state;
                try
                {
                    state = await asyncAction();
                    if (!state.IsAborted)
                    {
                        uint affectedRows = transactionContext.AffectedRows;
                        return DbTransactionResult.Success(affectedRows);
                    }
                }
                catch (Exception e)
                {
                    state = ActionState.Abort(new ExceptionDbError(e));
                }

                try { await transaction.RollbackToSavepointAsync(savepoint); }
                catch (Exception e) { throw new DbRollbackException(state.Error.Message, e); }
                finally { transactionContext.Rollback(); }

                if (state.Error.IsTransient)
                    throw new DbTransientException(state.Error);
                return DbTransactionResult.Failure(state.Error);
            }
            finally
            {
                try { await transaction.ReleaseSavepointAsync(savepoint); } catch { }
            }
        }
        else
        {
            ActionState state = await asyncAction();
            if (state.IsAborted)
            {
                if (state.Error.IsTransient)
                    throw new DbTransientException(state.Error);
                return DbTransactionResult.Failure(state.Error);
            }
            uint affectedRows = TransactionContext.GetCurrent(DbContext.Database)?.AffectedRows ?? 0;
            return DbTransactionResult.Success(affectedRows);
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Take care when using any non-efc logic, such as adding to lists, since the operations might be
    /// repeated several times when the transaction is retried. Throws an exception if the call
    /// is nested inside an active transaction and the exception itself is caused by transient error.
    /// </remarks>
    /// <exception cref="DbException">When transient error occurs in a nested call</exception>
    public ValueTask<DbTransactionResult> InTransactionAsync(Func<Task> asyncAction)
    {
        return InTransactionAsync(WrappedAction);

        async Task<ActionState> WrappedAction()
        {
            await asyncAction();
            return ActionState.Complete();
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                DbContext.Dispose();
            }
            _disposed = true;
        }
    }

    /// <summary>
    /// Fetches a (possibly cached) instance of repository.
    /// </summary>
    /// <typeparam name="TRepository">Type of repository that targets entity</typeparam>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    protected TRepository GetRepository<TRepository>(Func<TContext, TRepository> createRepository) where TRepository : IRepository
    {
        ArgumentNullException.ThrowIfNull(createRepository);

        int key = typeof(TRepository).GetHashCode();

        // First try to get the repository without locking
        ref object value = ref CollectionsMarshal.GetValueRefOrNullRef(_loadedRepositories, key);
        if (!Unsafe.IsNullRef(ref value))
            return (TRepository)value;

        // If not found, lock and try again (double-checked locking), else insert
        lock (_loadedRepositoriesLock)
        {
            ref object lockedValue = ref CollectionsMarshal.GetValueRefOrNullRef(_loadedRepositories, key);
            if (!Unsafe.IsNullRef(ref lockedValue))
                return (TRepository)lockedValue;
            if (createRepository(DbContext) is not { } repository)
                throw new InvalidOperationException(string.Format(Resources.Text_Error_TypeInstantiationFailed, typeof(TRepository).Name));
            _loadedRepositories[key] = repository;
            return repository;
        }
    }
}
