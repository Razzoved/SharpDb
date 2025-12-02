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
public abstract class UnitOfWork(DbContext dbContext) : IUnitOfWork
{
    private readonly Dictionary<int, object> _loadedRepositories = new(3);
    private readonly object _loadedRepositoriesLock = new();
    private EfcSqlRunner? _sql;

    /// <summary>
    /// This property can be used to execute SQL operations directly
    /// on the underlying database.
    /// </summary>
    public EfcSqlRunner Sql => _sql ??= new(dbContext.Database);

    public void Attach<TEntity>(TEntity entity) where TEntity : class
    {
        var entry = dbContext.Entry(entity);
        if (entry.State == EntityState.Detached)
            entry.State = EntityState.Unchanged;
    }

    public void Detach<TEntity>(TEntity entity) where TEntity : class
    {
        var entry = dbContext.Entry(entity);
        if (entry.State != EntityState.Detached)
            entry.State = EntityState.Detached;
    }

    public int SaveChanges()
    {
        int affectedRows = dbContext.SaveChanges();
        if (affectedRows > 0 && TransactionContext.Transaction is not null)
        {
            TransactionContext.AddAffectedRows((uint)affectedRows);
        }
        return affectedRows;
    }

    public async ValueTask<int> SaveChangesAsync()
    {
        int affectedRows = await dbContext.SaveChangesAsync();
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
            dbContext.ChangeTracker.Clear();
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Take care when using any non-efc logic, such as adding to lists, since the operations might be
    /// repeated several times when the transaction is retried. Do not wrap this method in a try-catch
    /// block, as it handles exceptions internally.
    /// </remarks>
    public DbTransactionResult InTransaction(Action action)
    {
        if (dbContext.Database.CurrentTransaction is IDbContextTransaction transaction)
        {
            if (transaction.SupportsSavepoints)
            {
                string savepoint = Guid.NewGuid().ToString("N");
                transaction.CreateSavepoint(savepoint);
                using var transactionContext = new TransactionContext(dbContext);
                try
                {
                    action();
                    return DbTransactionResult.Success(TransactionContext.AffectedRows);
                }
                catch (Exception e)
                {
                    transaction.RollbackToSavepoint(savepoint);
                    transactionContext.Rollback();
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
            var strategy = dbContext.Database.CreateExecutionStrategy();
            try
            {
                uint affectedRows = 0;
                strategy.Execute(() =>
                {
                    using var transaction = dbContext.Database.BeginTransaction();
                    using var transactionContext = new TransactionContext(dbContext);
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
    /// repeated several times when the transaction is retried. Do not wrap this method in a try-catch
    /// block, as it handles exceptions internally.
    /// </remarks>
    public async ValueTask<DbTransactionResult> InTransactionAsync(Func<Task> action)
    {
        if (dbContext.Database.CurrentTransaction is IDbContextTransaction transaction)
        {
            if (transaction.SupportsSavepoints)
            {
                string savepoint = Guid.NewGuid().ToString("N");
                await transaction.CreateSavepointAsync(savepoint);
                using var transactionContext = new TransactionContext(dbContext);
                try
                {
                    await action();
                    return DbTransactionResult.Success(TransactionContext.AffectedRows);
                }
                catch
                {
                    await transaction.RollbackToSavepointAsync(savepoint);
                    transactionContext.Rollback();
                    throw;
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
            var strategy = dbContext.Database.CreateExecutionStrategy();
            try
            {
                uint affectedRows = 0;
                await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = dbContext.Database.BeginTransaction();
                    using var transactionContext = new TransactionContext(dbContext);
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

    /// <summary>
    /// Fetches a (possibly cached) instance of repository.
    /// </summary>
    /// <typeparam name="TEntity">Type of target entity</typeparam>
    /// <typeparam name="TRepository">Type of repository that targets entity</typeparam>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    protected TRepository GetRepository<TEntity, TRepository>() where TRepository : IRepository<TEntity> where TEntity : class
    {
        ref object value = ref CollectionsMarshal.GetValueRefOrNullRef(_loadedRepositories, typeof(TRepository).GetHashCode());
        if (Unsafe.IsNullRef(ref value))
        {
            lock (_loadedRepositoriesLock)
            {
                if (Unsafe.IsNullRef(ref value) || value is not TRepository)
                {
                    if (Activator.CreateInstance(typeof(TRepository), dbContext) is not TRepository repository)
                        throw new InvalidOperationException(string.Format(Resources.Text_Error_TypeInstantiationFailed, typeof(TRepository).Name));
                    value = repository;
                }
            }
        }
        return (TRepository)value;
    }
}
