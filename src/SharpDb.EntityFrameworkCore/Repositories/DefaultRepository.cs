using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace SharpDb.EntityFrameworkCore.Repositories;

/// <summary>
/// Repository implementation with methods for common data access operations.
/// Use this repository when you don't need a specialized repository for an entity type.
/// </summary>
/// <typeparam name="TEntity">Type of entity</typeparam>
/// <param name="context">Database context used by the repository</param>
public sealed class DefaultRepository<TEntity>(DbContext context) : Repository<TEntity>(context) where TEntity : class
{
    public async Task<DbQueryResult<TEntity?>> GetAsync(Expression<Func<TEntity, bool>> expression, Func<IQueryable<TEntity>, IQueryable<TEntity>>? apply = null)
    {
        try
        {
            IQueryable<TEntity> query = apply is null ? Set : apply(Set);
            if (query is not IOrderedQueryable<TEntity>)
                query = query.Order();
            return DbQueryResult<TEntity?>.Success(await query.FirstOrDefaultAsync(expression));
        }
        catch (Exception e)
        {
            return DbQueryResult<TEntity?>.Failure(new ExceptionDbError(e));
        }
    }

    public async Task<DbQueryResult<TEntity[]>> GetArrayAsync(Expression<Func<TEntity, bool>>? expression = null, Func<IQueryable<TEntity>, IQueryable<TEntity>>? apply = null)
    {
        try
        {
            IQueryable<TEntity> query = apply is null ? Set : apply(Set);
            if (query is not IOrderedQueryable<TEntity>)
                query = query.Order();
            if (expression is not null)
                query = query.Where(expression);
            return DbQueryResult<TEntity[]>.Success(await query.ToArrayAsync());
        }
        catch (Exception e)
        {
            return DbQueryResult<TEntity[]>.Failure(new ExceptionDbError(e));
        }
    }

    public async Task<DbQueryResult<List<TEntity>>> GetListAsync(Expression<Func<TEntity, bool>>? expression = null, Func<IQueryable<TEntity>, IQueryable<TEntity>>? apply = null)
    {
        try
        {
            IQueryable<TEntity> query = apply is null ? Set : apply(Set);
            if (query is not IOrderedQueryable<TEntity>)
                query = query.Order();
            if (expression is not null)
                query = query.Where(expression);
            return DbQueryResult<List<TEntity>>.Success(await query.ToListAsync());
        }
        catch (Exception e)
        {
            return DbQueryResult<List<TEntity>>.Failure(new ExceptionDbError(e));
        }
    }

    public async Task<DbQueryResult<int>> GetCountAsync(Expression<Func<TEntity, bool>>? expression = null)
    {
        try
        {
            IQueryable<TEntity> query = Set.AsNoTracking();
            if (expression is not null)
                query = query.Where(expression);
            return DbQueryResult<int>.Success(await query.CountAsync());
        }
        catch (Exception e)
        {
            return DbQueryResult<int>.Failure(new ExceptionDbError(e));
        }
    }

    public async Task<DbQueryResult<bool>> GetExistsAsync(Expression<Func<TEntity, bool>>? expression = null)
    {
        try
        {
            IQueryable<TEntity> query = Set.AsNoTracking();
            if (expression is not null)
                query = query.Where(expression);
            return DbQueryResult<bool>.Success(await query.AnyAsync());
        }
        catch (Exception e)
        {
            return DbQueryResult<bool>.Failure(new ExceptionDbError(e));
        }
    }
}
