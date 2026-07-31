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
    public delegate IQueryable<TEntity> Apply(IQueryable<TEntity> query);
    public delegate IQueryable<TProjection> Apply<out TProjection>(IQueryable<TEntity> query);

    public Task<DbQueryResult<TEntity?>> GetAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellation = default)
        => GetAsync(apply: query => query.Where(expression), cancellation: cancellation);

    public Task<DbQueryResult<TEntity?>> GetAsync(Apply apply, CancellationToken cancellation = default)
        => GetAsync(project: Passthrough, applyBeforeProjection: apply, cancellation: cancellation);

    public Task<DbQueryResult<T?>> GetAsync<T>(Apply<T> project, Apply? applyBeforeProjection = null, CancellationToken cancellation = default)
    {
        IQueryable<TEntity> query = Set.AsQueryable();
        if (applyBeforeProjection is not null)
            query = applyBeforeProjection(query);
        if (query is not IOrderedQueryable<TEntity>)
            query = query.OrderByDefault(Set);
        return project(query).FirstOrDefaultAsyncResult(cancellation);
    }

    public Task<DbQueryResult<TEntity[]>> GetArrayAsync(Expression<Func<TEntity, bool>>? expression = null, CancellationToken cancellation = default)
        => GetArrayAsync(apply: expression is null ? Passthrough : query => query.Where(expression), cancellation: cancellation);

    public Task<DbQueryResult<TEntity[]>> GetArrayAsync(Apply apply, CancellationToken cancellation = default)
        => GetArrayAsync(project: Passthrough, applyBeforeProjection: apply, cancellation: cancellation);

    public Task<DbQueryResult<T[]>> GetArrayAsync<T>(Apply<T> project, Apply? applyBeforeProjection = null, CancellationToken cancellation = default)
    {
        IQueryable<TEntity> query = Set.AsQueryable();
        if (applyBeforeProjection is not null)
            query = applyBeforeProjection(query);
        if (query is not IOrderedQueryable<TEntity>)
            query = query.OrderByDefault(Set);
        return project(query).ToArrayAsyncResult(cancellation);
    }

    public Task<DbQueryResult<List<TEntity>>> GetListAsync(Expression<Func<TEntity, bool>>? expression = null, CancellationToken cancellation = default)
        => GetListAsync(apply: expression is null ? Passthrough : query => query.Where(expression), cancellation: cancellation);

    public Task<DbQueryResult<List<TEntity>>> GetListAsync(Apply apply, CancellationToken cancellation = default)
        => GetListAsync(project: Passthrough, applyBeforeProjection: apply, cancellation: cancellation);

    public Task<DbQueryResult<List<T>>> GetListAsync<T>(Apply<T> project, Apply? applyBeforeProjection = null, CancellationToken cancellation = default)
    {
        IQueryable<TEntity> query = Set.AsQueryable();
        if (applyBeforeProjection is not null)
            query = applyBeforeProjection(query);
        if (query is not IOrderedQueryable<TEntity>)
            query = query.OrderByDefault(Set);
        return project(query).ToListAsyncResult(cancellation);
    }

    public Task<DbQueryResult<int>> CountAsync(Expression<Func<TEntity, bool>>? expression = null, CancellationToken cancellation = default)
    {
        IQueryable<TEntity> query = Set.AsNoTracking();
        if (expression is not null)
            query = query.Where(expression);
        return query.CountAsyncResult(cancellation);
    }

    [Obsolete("This method will be removed in future versions. Use CountAsync instead.")]
    public Task<DbQueryResult<int>> GetCountAsync(Expression<Func<TEntity, bool>>? expression = null, CancellationToken cancellation = default)
        => CountAsync(expression, cancellation);

    public Task<DbQueryResult<bool>> ExistsAsync(Expression<Func<TEntity, bool>>? expression = null, CancellationToken cancellation = default)
    {
        IQueryable<TEntity> query = Set.AsNoTracking();
        if (expression is not null)
            query = query.Where(expression);
        return query.AnyAsyncResult(cancellation);
    }

    [Obsolete("This method will be removed in future versions. Use ExistsAsync instead.")]
    public Task<DbQueryResult<bool>> GetExistsAsync(Expression<Func<TEntity, bool>>? expression = null, CancellationToken cancellation = default)
        => ExistsAsync(expression ?? (static _ => true), cancellation);

    private static IQueryable<TEntity> Passthrough(IQueryable<TEntity> query) => query;
}
