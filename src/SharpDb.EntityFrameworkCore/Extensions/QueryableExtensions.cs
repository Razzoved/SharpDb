using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace SharpDb.EntityFrameworkCore.Extensions;

public static class QueryableExtensions
{
    private static readonly ConcurrentDictionary<int, string[]> s_orderingCache = [];

    /// <summary>
    /// Orders the queryable by the primary key or first defined index of the entity type
    /// in ascending manner. Alternatively use <see cref="OrderByDefaultDescending{TEntity}"/>.
    /// </summary>
    /// <typeparam name="TEntity">Entity type (should be part of EFC model)</typeparam>
    /// <param name="source">What to order</param>
    /// <param name="dbSet">Source of model info</param>
    /// <returns>Ordered queryable</returns>
    /// <exception cref="ArgumentException">When no key or index is defined</exception>
    public static IOrderedQueryable<TEntity> OrderByDefault<TEntity>(this IQueryable<TEntity> source, DbSet<TEntity> dbSet) where TEntity : class
    {
        string[] keys = s_orderingCache.GetOrAdd(typeof(TEntity).GetHashCode(), GetDefaultOrderingNames, dbSet.EntityType);
        if (keys.Length == 0)
            throw new ArgumentException("No primary key or index defined.", nameof(dbSet));
        IOrderedQueryable<TEntity>? orderedQuery = source.OrderBy(e => EF.Property<object>(e, keys[0]));
        for (int i = 1; i < keys.Length; i++)
        {
            orderedQuery = orderedQuery.ThenBy(e => EF.Property<object>(e, keys[i]));
        }
        return orderedQuery;
    }

    /// <summary>
    /// Orders the queryable by the primary key or first defined index of the entity type
    /// in descending manner. Alternatively use <see cref="OrderByDefault{TEntity}"/>.
    /// </summary>
    /// <typeparam name="TEntity">Entity type (should be part of EFC model)</typeparam>
    /// <param name="source">What to order</param>
    /// <param name="dbSet">Source of model info</param>
    /// <returns>Ordered queryable</returns>
    /// <exception cref="ArgumentException">When no key or index is defined</exception>
    public static IOrderedQueryable<TEntity> OrderByDefaultDescending<TEntity>(this IQueryable<TEntity> source, DbSet<TEntity> dbSet) where TEntity : class
    {
        string[] keys = s_orderingCache.GetOrAdd(typeof(TEntity).GetHashCode(), GetDefaultOrderingNames, dbSet.EntityType);
        if (keys.Length == 0)
            throw new ArgumentException("No primary key or index defined.", nameof(dbSet));
        IOrderedQueryable<TEntity>? orderedQuery = source.OrderByDescending(e => EF.Property<object>(e, keys[0]));
        for (int i = 1; i < keys.Length; i++)
        {
            orderedQuery = orderedQuery.ThenByDescending(e => EF.Property<object>(e, keys[i]));
        }
        return orderedQuery;
    }

    private static string[] GetDefaultOrderingNames(int cacheKey, IEntityType entityType)
    {
        var primaryKey = entityType.FindPrimaryKey()?.Properties;
        if (primaryKey is not null && primaryKey.Count > 0)
        {
            return [.. primaryKey.Select(x => x.Name)];
        }
        foreach (var index in entityType.GetIndexes())
        {
            if (index is not null && index.Properties.Count > 0)
            {
                return [.. index.Properties.Select(x => x.Name)];
            }
        }
        return [];
    }
}
