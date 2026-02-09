using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.EntityFrameworkCore;

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

    public int SaveChanges()
    {
        int affectedRows = DbContext.SaveChanges();
        if (affectedRows > 0 && TransactionContext.GetCurrent(DbContext.Database) is { } transactionContext)
        {
            transactionContext.AddAffectedRows((uint)affectedRows);
        }
        return affectedRows;
    }

    public async ValueTask<int> SaveChangesAsync()
    {
        int affectedRows = await DbContext.SaveChangesAsync();
        if (affectedRows > 0 && TransactionContext.GetCurrent(DbContext.Database) is { } transactionContext)
        {
            transactionContext.AddAffectedRows((uint)affectedRows);
        }
        return affectedRows;
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
        if (DbContext.Database.CurrentTransaction is { } transaction)
        {
            if (transaction.SupportsSavepoints)
            {
                string savepoint = Guid.NewGuid().ToString("N");
                transaction.CreateSavepoint(savepoint);
                using var transactionContext = new TransactionContext(DbContext);
                try
                {
                    var state = action();
                    if (state.IsAborted)
                    {
                        transaction.RollbackToSavepoint(savepoint);
                        transactionContext.Rollback();
                        return DbTransactionResult.Failure(state.Error);
                    }
                    return DbTransactionResult.Success(transactionContext.AffectedRows);
                }
                catch (Exception e)
                {
                    transaction.RollbackToSavepoint(savepoint);
                    transactionContext.Rollback();
                    if (e is DbException { IsTransient: true }) throw;
                    return DbTransactionResult.Failure(new ExceptionDbError(e));
                }
                finally
                {
                    transaction.ReleaseSavepoint(savepoint);
                }
            }
            else
            {
                var state = action();
                if (state.IsAborted)
                {
                    return DbTransactionResult.Failure(state.Error);
                }
                uint affectedRows = TransactionContext.GetCurrent(DbContext.Database)?.AffectedRows ?? 0;
                return DbTransactionResult.Success(affectedRows);
            }
        }
        else
        {
            var strategy = DbContext.Database.CreateExecutionStrategy();
            try
            {
                uint affectedRows = strategy.Execute((DbContext, action), actionContext =>
                {
                    using var newTransaction = actionContext.DbContext.Database.BeginTransaction();
                    using var newTransactionContext = new TransactionContext(actionContext.DbContext);
                    try
                    {
                        var state = actionContext.action();
                        if (state.IsAborted)
                        {
                            var exception = state.Error is ExceptionDbError { Exception: not null } exError
                                ? exError.Exception
                                : new InvalidOperationException(state.Error.Message);
                            throw exception;
                        }
                        newTransaction.Commit();
                        return newTransactionContext.AffectedRows;
                    }
                    catch
                    {
                        newTransaction.Rollback();
                        newTransactionContext.Rollback();
                        throw;
                    }
                });
                return DbTransactionResult.Success(affectedRows);
            }
            catch (Exception e)
            {
                return DbTransactionResult.Failure(new ExceptionDbError(e));
            }
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Take care when using any non-efc logic, such as adding to lists, since the operations might be
    /// repeated several times when the transaction is retried. Throws an exception if the call
    /// is nested inside an active transaction and the exception itself is caused by transient error.
    /// </remarks>
    /// <exception cref="DbException">When transient error occurs in a nested call</exception>
    public DbTransactionResult InTransaction(Action action) => InTransaction(() =>
    {
        action();
        return ActionState.Complete();
    });

    /// <inheritdoc/>
    /// <remarks>
    /// Take care when using any non-efc logic, such as adding to lists, since the operations might be
    /// repeated several times when the transaction is retried. Throws an exception if the call
    /// is nested inside an active transaction and the exception itself is caused by transient error.
    /// </remarks>
    /// <exception cref="DbException">When transient error occurs in a nested call</exception>
    public async ValueTask<DbTransactionResult> InTransactionAsync(Func<Task<ActionState>> asyncAction)
    {
        if (DbContext.Database.CurrentTransaction is { } transaction)
        {
            if (transaction.SupportsSavepoints)
            {
                string savepoint = Guid.NewGuid().ToString("N");
                await transaction.CreateSavepointAsync(savepoint);
                using var transactionContext = new TransactionContext(DbContext);
                try
                {
                    var state = await asyncAction();
                    if (state.IsAborted)
                    {
                        await transaction.RollbackToSavepointAsync(savepoint);
                        transactionContext.Rollback();
                        return DbTransactionResult.Failure(state.Error);
                    }
                    return DbTransactionResult.Success(transactionContext.AffectedRows);
                }
                catch (Exception e)
                {
                    await transaction.RollbackToSavepointAsync(savepoint);
                    transactionContext.Rollback();
                    if (e is DbException { IsTransient: true }) throw;
                    return DbTransactionResult.Failure(new ExceptionDbError(e));
                }
                finally
                {
                    await transaction.ReleaseSavepointAsync(savepoint);
                }
            }
            else
            {
                var state = await asyncAction();
                if (state.IsAborted)
                {
                    return DbTransactionResult.Failure(state.Error);
                }
                uint affectedRows = TransactionContext.GetCurrent(DbContext.Database)?.AffectedRows ?? 0;
                return DbTransactionResult.Success(affectedRows);
            }
        }
        else
        {
            var strategy = DbContext.Database.CreateExecutionStrategy();
            try
            {
                uint affectedRows = await strategy.ExecuteAsync((DbContext, asyncAction), async actionContext =>
                {
                    await using var newTransaction = await actionContext.DbContext.Database.BeginTransactionAsync();
                    using var newTransactionContext = new TransactionContext(actionContext.DbContext);
                    try
                    {
                        var state = await actionContext.asyncAction();
                        if (state.IsAborted)
                        {
                            var exception = state.Error is ExceptionDbError { Exception: not null } exError
                                ? exError.Exception
                                : new InvalidOperationException(state.Error.Message);
                            throw exception;
                        }
                        await newTransaction.CommitAsync();
                        return newTransactionContext.AffectedRows;
                    }
                    catch
                    {
                        await newTransaction.RollbackAsync();
                        newTransactionContext.Rollback();
                        throw;
                    }
                });
                return DbTransactionResult.Success(affectedRows);
            }
            catch (Exception e)
            {
                return DbTransactionResult.Failure(new ExceptionDbError(e));
            }
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Take care when using any non-efc logic, such as adding to lists, since the operations might be
    /// repeated several times when the transaction is retried. Throws an exception if the call
    /// is nested inside an active transaction and the exception itself is caused by transient error.
    /// </remarks>
    /// <exception cref="DbException">When transient error occurs in a nested call</exception>
    public ValueTask<DbTransactionResult> InTransactionAsync(Func<Task> asyncAction) => InTransactionAsync(async () =>
    {
        await asyncAction();
        return ActionState.Complete();
    });

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
