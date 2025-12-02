using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace SharpDb.EntityFrameworkCore.Repositories;

/// <summary>
/// Repository implementation with generic methods for common data access operations.
/// </summary>
/// <typeparam name="TEntity">Type of entity</typeparam>
/// <param name="context">Context to operate on</param>
public sealed class GenericRepository<TEntity>(DbContext context) : Repository<TEntity>(context) where TEntity : class
{
    public Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> expression, Func<IQueryable<TEntity>, IQueryable<TEntity>>? apply = null)
    {
        IQueryable<TEntity> query = apply is null ? Set : apply(Set);
        if (query is not IOrderedQueryable<TEntity>)
            query = query.Order();
        return query.FirstOrDefaultAsync(expression);
    }

    public Task<TEntity[]> GetArrayAsync(Expression<Func<TEntity, bool>>? expression = null, Func<IQueryable<TEntity>, IQueryable<TEntity>>? apply = null)
    {
        IQueryable<TEntity> query = apply is null ? Set : apply(Set);
        if (query is not IOrderedQueryable<TEntity>)
            query = query.Order();
        if (expression is not null)
            query = query.Where(expression);
        return query.ToArrayAsync();
    }

    public Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>>? expression = null, Func<IQueryable<TEntity>, IQueryable<TEntity>>? apply = null)
    {
        IQueryable<TEntity> query = apply is null ? Set : apply(Set);
        if (query is not IOrderedQueryable<TEntity>)
            query = query.Order();
        if (expression is not null)
            query = query.Where(expression);
        return query.ToListAsync();
    }

    public Task<int> GetCountAsync(Expression<Func<TEntity, bool>>? expression = null)
    {
        IQueryable<TEntity> query = Set;
        if (expression is not null)
            query = query.Where(expression);
        return query.AsNoTracking().CountAsync();
    }

    public Task<bool> GetExistsAsync(Expression<Func<TEntity, bool>>? expression = null)
    {
        IQueryable<TEntity> query = Set;
        if (expression is not null)
            query = query.Where(expression);
        return query.AsNoTracking().AnyAsync();
    }

    public override void Add(TEntity entity, bool cascade = true)
        => EntityAdd.Execute(Context, entity, cascade);

    public override void Update(TEntity entity, bool cascade = true)
        => EntityUpdate.Execute(Context, entity, cascade);

    public override void Delete(TEntity entity, bool cascade = true)
        => EntityDelete.Execute(Context, entity, cascade);
}
