using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace SharpDb.EntityFrameworkCore;

/// <summary>
/// Part of an abstraction layer over the database implementation.
/// Use this to hide DB implementation details from users, and to
/// faciliate transactional operations even between repositories
/// or database commands.
/// </summary>
/// <param name="dbContext">Database context to be wrapped</param>
public abstract class UnitOfWork<TContext>(IDbContextFactory<TContext> dbContextFactory) : IUnitOfWork where TContext : DbContext
{
    private readonly TContext _dbContext = dbContextFactory.CreateDbContext();
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
    public SqlRunner Sql => _sql ??= new(_dbContext.Database);

    public int SaveChanges()
    {
        int affectedRows = _dbContext.SaveChanges();
        if (affectedRows > 0 && TransactionContext.Transaction is not null)
        {
            TransactionContext.AddAffectedRows((uint)affectedRows);
        }
        return affectedRows;
    }

    public async ValueTask<int> SaveChangesAsync()
    {
        int affectedRows = await _dbContext.SaveChangesAsync();
        if (affectedRows > 0 && TransactionContext.Transaction is not null)
        {
            TransactionContext.AddAffectedRows((uint)affectedRows);
        }
        return affectedRows;
    }

    public void DiscardChanges()
    {
        if (TransactionContext.ChangeJournal is ChangeJournal journal)
        {
            journal.Restore();
        }
        else
        {
            _dbContext.ChangeTracker.Clear();
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
        if (_dbContext.Database.CurrentTransaction is IDbContextTransaction transaction)
        {
            if (transaction.SupportsSavepoints)
            {
                string savepoint = Guid.NewGuid().ToString("N");
                transaction.CreateSavepoint(savepoint);
                using var transactionContext = new TransactionContext(_dbContext);
                try
                {
                    action();
                    return DbTransactionResult.Success(TransactionContext.AffectedRows);
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
                action();
                return DbTransactionResult.Success(TransactionContext.AffectedRows);
            }
        }
        else
        {
            var strategy = _dbContext.Database.CreateExecutionStrategy();
            try
            {
                uint affectedRows = 0;
                strategy.Execute(() =>
                {
                    using var transaction = _dbContext.Database.BeginTransaction();
                    using var transactionContext = new TransactionContext(_dbContext);
                    try
                    {
                        action();
                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        transactionContext.Rollback();
                        throw;
                    }
                    finally
                    {
                        affectedRows = TransactionContext.AffectedRows;
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
    public async ValueTask<DbTransactionResult> InTransactionAsync(Func<Task> action)
    {
        if (_dbContext.Database.CurrentTransaction is IDbContextTransaction transaction)
        {
            if (transaction.SupportsSavepoints)
            {
                string savepoint = Guid.NewGuid().ToString("N");
                await transaction.CreateSavepointAsync(savepoint);
                using var transactionContext = new TransactionContext(_dbContext);
                try
                {
                    await action();
                    return DbTransactionResult.Success(TransactionContext.AffectedRows);
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
                await action();
                return DbTransactionResult.Success(TransactionContext.AffectedRows);
            }
        }
        else
        {
            var strategy = _dbContext.Database.CreateExecutionStrategy();
            try
            {
                uint affectedRows = 0;
                await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = _dbContext.Database.BeginTransaction();
                    using var transactionContext = new TransactionContext(_dbContext);
                    try
                    {
                        await action();
                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        transactionContext.Rollback();
                        throw;
                    }
                    finally
                    {
                        affectedRows = TransactionContext.AffectedRows;
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
                _dbContext.Dispose();
            }
            _disposed = true;
        }
    }

    /// <summary>
    /// Fetches the underlying database context.
    /// </summary>
    /// <returns></returns>
    protected TContext GetContext() => _dbContext;

    /// <summary>
    /// Fetches a (possibly cached) instance of repository.
    /// </summary>
    /// <typeparam name="TEntity">Type of target entity</typeparam>
    /// <typeparam name="TRepository">Type of repository that targets entity</typeparam>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    protected TRepository GetRepository<TRepository>(Func<TContext, TRepository> createRepository) where TRepository : IRepository
    {
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
            if (createRepository is null || createRepository(_dbContext) is not TRepository repository)
                throw new InvalidOperationException(string.Format(Resources.Text_Error_TypeInstantiationFailed, typeof(TRepository).Name));
            _loadedRepositories[key] = repository;
            return repository;
        }
    }
}
