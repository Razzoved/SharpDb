using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace SharpDb.EntityFrameworkCore;

public static partial class QueryableExtensions
{
    #region Single(OrDefault)

    public static Task<DbQueryResult<T>> SingleAsyncResult<T>(this IQueryable<T> query, CancellationToken cancellation = default)
        => SingleAsyncResultCore(query, null, cancellation);

    public static Task<DbQueryResult<T>> SingleAsyncResult<T>(this IQueryable<T> query, Expression<Func<T, bool>> predicate, CancellationToken cancellation = default)
        => SingleAsyncResultCore(query, predicate, cancellation);

    private static async Task<DbQueryResult<T>> SingleAsyncResultCore<T>(this IQueryable<T> query, Expression<Func<T, bool>>? predicate = null, CancellationToken cancellation = default)
    {
        try
        {
            var q = predicate is null
                ? query.SingleAsync(cancellation)
                : query.SingleAsync(predicate, cancellation);
            return DbQueryResult<T>.Success(await q.ConfigureAwait(false));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<T>.Failure(new ExceptionDbError(e));
        }
    }

    public static Task<DbQueryResult<T?>> SingleOrDefaultAsyncResult<T>(this IQueryable<T> query, CancellationToken cancellation = default)
        => SingleOrDefaultAsyncResultCore(query, null, cancellation);

    public static Task<DbQueryResult<T?>> SingleOrDefaultAsyncResult<T>(this IQueryable<T> query, Expression<Func<T, bool>> predicate, CancellationToken cancellation = default)
        => SingleOrDefaultAsyncResultCore(query, predicate, cancellation);

    private static async Task<DbQueryResult<T?>> SingleOrDefaultAsyncResultCore<T>(this IQueryable<T> query, Expression<Func<T, bool>>? predicate = null, CancellationToken cancellation = default)
    {
        try
        {
            var q = predicate is null
                ? query.SingleOrDefaultAsync(cancellation)
                : query.SingleOrDefaultAsync(predicate, cancellation);
            return DbQueryResult<T?>.Success(await q.ConfigureAwait(false));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<T?>.Failure(new ExceptionDbError(e));
        }
    }

    #endregion

    #region First(OrDefault)

    public static Task<DbQueryResult<T>> FirstAsyncResult<T>(this IQueryable<T> query, CancellationToken cancellation = default)
        => FirstAsyncResultCore(query, null, cancellation);

    public static Task<DbQueryResult<T>> FirstAsyncResult<T>(this IQueryable<T> query, Expression<Func<T, bool>> predicate, CancellationToken cancellation = default)
        => FirstAsyncResultCore(query, predicate, cancellation);

    private static async Task<DbQueryResult<T>> FirstAsyncResultCore<T>(this IQueryable<T> query, Expression<Func<T, bool>>? predicate = null, CancellationToken cancellation = default)
    {
        try
        {
            var q = predicate is null
                ? query.FirstAsync(cancellation)
                : query.FirstAsync(predicate, cancellation);
            return DbQueryResult<T>.Success(await q.ConfigureAwait(false));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<T>.Failure(new ExceptionDbError(e));
        }
    }

    public static Task<DbQueryResult<T?>> FirstOrDefaultAsyncResult<T>(this IQueryable<T> query, CancellationToken cancellation = default)
        => FirstOrDefaultAsyncResultCore(query, null, cancellation);

    public static Task<DbQueryResult<T?>> FirstOrDefaultAsyncResult<T>(this IQueryable<T> query, Expression<Func<T, bool>> predicate, CancellationToken cancellation = default)
        => FirstOrDefaultAsyncResultCore(query, predicate, cancellation);

    private static async Task<DbQueryResult<T?>> FirstOrDefaultAsyncResultCore<T>(this IQueryable<T> query, Expression<Func<T, bool>>? predicate = null, CancellationToken cancellation = default)
    {
        try
        {
            var q = predicate is null
                ? query.FirstOrDefaultAsync(cancellation)
                : query.FirstOrDefaultAsync(predicate, cancellation);
            return DbQueryResult<T?>.Success(await q.ConfigureAwait(false));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<T?>.Failure(new ExceptionDbError(e));
        }
    }

    #endregion

    #region Last(OrDefault)

    public static Task<DbQueryResult<T>> LastAsyncResult<T>(this IQueryable<T> query, CancellationToken cancellation = default)
        => LastAsyncResultCore(query, null, cancellation);

    public static Task<DbQueryResult<T>> LastAsyncResult<T>(this IQueryable<T> query, Expression<Func<T, bool>> predicate, CancellationToken cancellation = default)
        => LastAsyncResultCore(query, predicate, cancellation);

    private static async Task<DbQueryResult<T>> LastAsyncResultCore<T>(this IQueryable<T> query, Expression<Func<T, bool>>? predicate = null, CancellationToken cancellation = default)
    {
        try
        {
            var q = predicate is null
                ? query.LastAsync(cancellation)
                : query.LastAsync(predicate, cancellation);
            return DbQueryResult<T>.Success(await q.ConfigureAwait(false));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<T>.Failure(new ExceptionDbError(e));
        }
    }

    public static Task<DbQueryResult<T?>> LastOrDefaultAsyncResult<T>(this IQueryable<T> query, CancellationToken cancellation = default)
        => LastOrDefaultAsyncResultCore(query, null, cancellation);

    public static Task<DbQueryResult<T?>> LastOrDefaultAsyncResult<T>(this IQueryable<T> query, Expression<Func<T, bool>> predicate, CancellationToken cancellation = default)
        => LastOrDefaultAsyncResultCore(query, predicate, cancellation);

    private static async Task<DbQueryResult<T?>> LastOrDefaultAsyncResultCore<T>(this IQueryable<T> query, Expression<Func<T, bool>>? predicate = null, CancellationToken cancellation = default)
    {
        try
        {
            var q = predicate is null
                ? query.LastOrDefaultAsync(cancellation)
                : query.LastOrDefaultAsync(predicate, cancellation);
            return DbQueryResult<T?>.Success(await q.ConfigureAwait(false));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<T?>.Failure(new ExceptionDbError(e));
        }
    }

    #endregion

    public static async Task<DbQueryResult<T[]>> ToArrayAsyncResult<T>(this IQueryable<T> query, CancellationToken cancellation = default)
    {
        try
        {
            return DbQueryResult<T[]>.Success(await query.ToArrayAsync(cancellation).ConfigureAwait(false));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<T[]>.Failure(new ExceptionDbError(e));
        }
    }

    public static async Task<DbQueryResult<List<T>>> ToListAsyncResult<T>(this IQueryable<T> query, CancellationToken cancellation = default)
    {
        try
        {
            return DbQueryResult<List<T>>.Success(await query.ToListAsync(cancellation).ConfigureAwait(false));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<List<T>>.Failure(new ExceptionDbError(e));
        }
    }

    #region ToDictionary

    public static Task<DbQueryResult<Dictionary<TKey, T>>> ToDictionaryAsyncResult<T, TKey>(IQueryable<T> query, Func<T, TKey> keySelector, CancellationToken cancellation = default) where TKey : notnull
        => ToDictionaryAsyncResultCore(query, keySelector, null, null, cancellation);

    public static Task<DbQueryResult<Dictionary<TKey, T>>> ToDictionaryAsyncResult<T, TKey>(IQueryable<T> query, Func<T, TKey> keySelector, Func<T, T> elementSelector, CancellationToken cancellation = default) where TKey : notnull
        => ToDictionaryAsyncResultCore(query, keySelector, elementSelector, null, cancellation);

    public static Task<DbQueryResult<Dictionary<TKey, T>>> ToDictionaryAsyncResult<T, TKey>(IQueryable<T> query, Func<T, TKey> keySelector, IEqualityComparer<TKey> comparer, CancellationToken cancellation = default) where TKey : notnull
        => ToDictionaryAsyncResultCore(query, keySelector, null, comparer, cancellation);

    public static Task<DbQueryResult<Dictionary<TKey, T>>> ToDictionaryAsyncResult<T, TKey>(IQueryable<T> query, Func<T, TKey> keySelector, Func<T, T> elementSelector, IEqualityComparer<TKey> comparer, CancellationToken cancellation = default) where TKey : notnull
        => ToDictionaryAsyncResultCore(query, keySelector, elementSelector, comparer, cancellation);

    private static async Task<DbQueryResult<Dictionary<TKey, T>>> ToDictionaryAsyncResultCore<T, TKey>(
        this IQueryable<T> query,
        Func<T, TKey> keySelector,
        Func<T, T>? elementSelector = null,
        IEqualityComparer<TKey>? comparer = null,
        CancellationToken cancellation = default) where TKey : notnull
    {
        try
        {
            var q = (elementSelector, comparer) switch
            {
                (null, null) => query.ToDictionaryAsync(keySelector, cancellation),
                (null, not null) => query.ToDictionaryAsync(keySelector, comparer, cancellation),
                (not null, null) => query.ToDictionaryAsync(keySelector, elementSelector, cancellation),
                (not null, not null) => query.ToDictionaryAsync(keySelector, elementSelector, comparer, cancellation)
            };
            return DbQueryResult<Dictionary<TKey, T>>.Success(await q.ConfigureAwait(false));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<Dictionary<TKey, T>>.Failure(new ExceptionDbError(e));
        }
    }

    #endregion

    #region Count

    public static Task<DbQueryResult<int>> CountAsyncResult<T>(this IQueryable<T> query, CancellationToken cancellation = default)
        => CountAsyncResultCore(query, null, cancellation);

    public static Task<DbQueryResult<int>> CountAsyncResult<T>(this IQueryable<T> query, Expression<Func<T, bool>> predicate, CancellationToken cancellation = default)
        => CountAsyncResultCore(query, predicate, cancellation);

    private static async Task<DbQueryResult<int>> CountAsyncResultCore<T>(this IQueryable<T> query, Expression<Func<T, bool>>? predicate = null, CancellationToken cancellation = default)
    {
        try
        {
            var q = predicate is null
                ? query.CountAsync(cancellation)
                : query.CountAsync(predicate, cancellation);
            return DbQueryResult<int>.Success(await q.ConfigureAwait(false));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<int>.Failure(new ExceptionDbError(e));
        }
    }

    public static Task<DbQueryResult<long>> LongCountAsyncResult<T>(this IQueryable<T> query, CancellationToken cancellation = default)
        => LongCountAsyncResultCore(query, null, cancellation);

    public static Task<DbQueryResult<long>> LongCountAsyncResult<T>(this IQueryable<T> query, Expression<Func<T, bool>> predicate, CancellationToken cancellation = default)
        => LongCountAsyncResultCore(query, predicate, cancellation);

    private static async Task<DbQueryResult<long>> LongCountAsyncResultCore<T>(this IQueryable<T> query, Expression<Func<T, bool>>? predicate = null, CancellationToken cancellation = default)
    {
        try
        {
            var q = predicate is null
                ? query.LongCountAsync(cancellation)
                : query.LongCountAsync(predicate, cancellation);
            return DbQueryResult<long>.Success(await q.ConfigureAwait(false));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<long>.Failure(new ExceptionDbError(e));
        }
    }

    #endregion

    #region Any

    public static Task<DbQueryResult<bool>> AnyAsyncResult<T>(this IQueryable<T> query, CancellationToken cancellation = default)
        => AnyAsyncResultCore(query, null, cancellation);

    public static Task<DbQueryResult<bool>> AnyAsyncResult<T>(this IQueryable<T> query, Expression<Func<T, bool>> predicate, CancellationToken cancellation = default)
        => AnyAsyncResultCore(query, predicate, cancellation);

    private static async Task<DbQueryResult<bool>> AnyAsyncResultCore<T>(this IQueryable<T> query, Expression<Func<T, bool>>? predicate = null, CancellationToken cancellation = default)
    {
        try
        {
            var q = predicate is null
                ? query.AnyAsync(cancellation)
                : query.AnyAsync(predicate, cancellation);
            return DbQueryResult<bool>.Success(await q.ConfigureAwait(false));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<bool>.Failure(new ExceptionDbError(e));
        }
    }

    #endregion

    public static async Task<DbQueryResult<bool>> AllAsyncResult<T>(this IQueryable<T> query, Expression<Func<T, bool>> predicate, CancellationToken cancellation = default)
    {
        try
        {
            return DbQueryResult<bool>.Success(await query.AllAsync(predicate, cancellation).ConfigureAwait(false));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<bool>.Failure(new ExceptionDbError(e));
        }
    }

    public static async Task<DbQueryResult<bool>> ContainsAsyncResult<T>(this IQueryable<T> query, T item, CancellationToken cancellation = default)
    {
        try
        {
            return DbQueryResult<bool>.Success(await query.ContainsAsync(item, cancellation).ConfigureAwait(false));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<bool>.Failure(new ExceptionDbError(e));
        }
    }

    #region Sum

    public static async Task<DbQueryResult<int>> SumAsyncResult<T>(this IQueryable<T> query, Expression<Func<T, int>> selector, CancellationToken cancellation = default)
    {
        try
        {
            return DbQueryResult<int>.Success(await query.SumAsync(selector, cancellation).ConfigureAwait(false));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<int>.Failure(new ExceptionDbError(e));
        }
    }

    public static async Task<DbQueryResult<int?>> SumAsyncResult<T>(this IQueryable<T> query, Expression<Func<T, int?>> selector, CancellationToken cancellation = default)
    {
        try
        {
            return DbQueryResult<int?>.Success(await query.SumAsync(selector, cancellation).ConfigureAwait(false));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<int?>.Failure(new ExceptionDbError(e));
        }
    }

    public static async Task<DbQueryResult<long>> SumAsyncResult<T>(this IQueryable<T> query, Expression<Func<T, long>> selector, CancellationToken cancellation = default)
    {
        try
        {
            return DbQueryResult<long>.Success(await query.SumAsync(selector, cancellation).ConfigureAwait(false));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<long>.Failure(new ExceptionDbError(e));
        }
    }

    public static async Task<DbQueryResult<long?>> SumAsyncResult<T>(this IQueryable<T> query, Expression<Func<T, long?>> selector, CancellationToken cancellation = default)
    {
        try
        {
            return DbQueryResult<long?>.Success(await query.SumAsync(selector, cancellation).ConfigureAwait(false));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<long?>.Failure(new ExceptionDbError(e));
        }
    }

    public static async Task<DbQueryResult<float>> SumAsyncResult<T>(this IQueryable<T> query, Expression<Func<T, float>> selector, CancellationToken cancellation = default)
    {
        try
        {
            return DbQueryResult<float>.Success(await query.SumAsync(selector, cancellation).ConfigureAwait(false));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<float>.Failure(new ExceptionDbError(e));
        }
    }

    public static async Task<DbQueryResult<float?>> SumAsyncResult<T>(this IQueryable<T> query, Expression<Func<T, float?>> selector, CancellationToken cancellation = default)
    {
        try
        {
            return DbQueryResult<float?>.Success(await query.SumAsync(selector, cancellation).ConfigureAwait(false));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<float?>.Failure(new ExceptionDbError(e));
        }
    }

    public static async Task<DbQueryResult<double>> SumAsyncResult<T>(this IQueryable<T> query, Expression<Func<T, double>> selector, CancellationToken cancellation = default)
    {
        try
        {
            return DbQueryResult<double>.Success(await query.SumAsync(selector, cancellation).ConfigureAwait(false));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<double>.Failure(new ExceptionDbError(e));
        }
    }

    public static async Task<DbQueryResult<double?>> SumAsyncResult<T>(this IQueryable<T> query, Expression<Func<T, double?>> selector, CancellationToken cancellation = default)
    {
        try
        {
            return DbQueryResult<double?>.Success(await query.SumAsync(selector, cancellation).ConfigureAwait(false));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<double?>.Failure(new ExceptionDbError(e));
        }
    }

    public static async Task<DbQueryResult<decimal>> SumAsyncResult<T>(this IQueryable<T> query, Expression<Func<T, decimal>> selector, CancellationToken cancellation = default)
    {
        try
        {
            return DbQueryResult<decimal>.Success(await query.SumAsync(selector, cancellation).ConfigureAwait(false));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<decimal>.Failure(new ExceptionDbError(e));
        }
    }

    public static async Task<DbQueryResult<decimal?>> SumAsyncResult<T>(this IQueryable<T> query, Expression<Func<T, decimal?>> selector, CancellationToken cancellation = default)
    {
        try
        {
            return DbQueryResult<decimal?>.Success(await query.SumAsync(selector, cancellation).ConfigureAwait(false));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<decimal?>.Failure(new ExceptionDbError(e));
        }
    }

    #endregion

    #region Average

    public static async Task<DbQueryResult<double>> AverageAsyncResult<T>(this IQueryable<T> query, Expression<Func<T, int>> selector, CancellationToken cancellation = default)
    {
        try
        {
            return DbQueryResult<double>.Success(await query.AverageAsync(selector, cancellation).ConfigureAwait(false));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<double>.Failure(new ExceptionDbError(e));
        }
    }

    public static async Task<DbQueryResult<double?>> AverageAsyncResult<T>(this IQueryable<T> query, Expression<Func<T, int?>> selector, CancellationToken cancellation = default)
    {
        try
        {
            return DbQueryResult<double?>.Success(await query.AverageAsync(selector, cancellation).ConfigureAwait(false));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<double?>.Failure(new ExceptionDbError(e));
        }
    }

    public static async Task<DbQueryResult<double>> AverageAsyncResult<T>(this IQueryable<T> query, Expression<Func<T, long>> selector, CancellationToken cancellation = default)
    {
        try
        {
            return DbQueryResult<double>.Success(await query.AverageAsync(selector, cancellation).ConfigureAwait(false));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<double>.Failure(new ExceptionDbError(e));
        }
    }

    public static async Task<DbQueryResult<double?>> AverageAsyncResult<T>(this IQueryable<T> query, Expression<Func<T, long?>> selector, CancellationToken cancellation = default)
    {
        try
        {
            return DbQueryResult<double?>.Success(await query.AverageAsync(selector, cancellation).ConfigureAwait(false));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<double?>.Failure(new ExceptionDbError(e));
        }
    }

    public static async Task<DbQueryResult<float>> AverageAsyncResult<T>(this IQueryable<T> query, Expression<Func<T, float>> selector, CancellationToken cancellation = default)
    {
        try
        {
            return DbQueryResult<float>.Success(await query.AverageAsync(selector, cancellation).ConfigureAwait(false));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<float>.Failure(new ExceptionDbError(e));
        }
    }

    public static async Task<DbQueryResult<float?>> AverageAsyncResult<T>(this IQueryable<T> query, Expression<Func<T, float?>> selector, CancellationToken cancellation = default)
    {
        try
        {
            return DbQueryResult<float?>.Success(await query.AverageAsync(selector, cancellation).ConfigureAwait(false));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<float?>.Failure(new ExceptionDbError(e));
        }
    }

    public static async Task<DbQueryResult<double>> AverageAsyncResult<T>(this IQueryable<T> query, Expression<Func<T, double>> selector, CancellationToken cancellation = default)
    {
        try
        {
            return DbQueryResult<double>.Success(await query.AverageAsync(selector, cancellation).ConfigureAwait(false));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<double>.Failure(new ExceptionDbError(e));
        }
    }

    public static async Task<DbQueryResult<double?>> AverageAsyncResult<T>(this IQueryable<T> query, Expression<Func<T, double?>> selector, CancellationToken cancellation = default)
    {
        try
        {
            return DbQueryResult<double?>.Success(await query.AverageAsync(selector, cancellation).ConfigureAwait(false));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<double?>.Failure(new ExceptionDbError(e));
        }
    }

    public static async Task<DbQueryResult<decimal>> AverageAsyncResult<T>(this IQueryable<T> query, Expression<Func<T, decimal>> selector, CancellationToken cancellation = default)
    {
        try
        {
            return DbQueryResult<decimal>.Success(await query.AverageAsync(selector, cancellation).ConfigureAwait(false));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<decimal>.Failure(new ExceptionDbError(e));
        }
    }

    public static async Task<DbQueryResult<decimal?>> AverageAsyncResult<T>(this IQueryable<T> query, Expression<Func<T, decimal?>> selector, CancellationToken cancellation = default)
    {
        try
        {
            return DbQueryResult<decimal?>.Success(await query.AverageAsync(selector, cancellation).ConfigureAwait(false));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<decimal?>.Failure(new ExceptionDbError(e));
        }
    }

    #endregion

    #region Min

    public static async Task<DbQueryResult<T>> MinAsyncResult<T>(this IQueryable<T> query, CancellationToken cancellation = default)
    {
        try
        {
            return DbQueryResult<T>.Success(await query.MinAsync(cancellation).ConfigureAwait(false));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<T>.Failure(new ExceptionDbError(e));
        }
    }

    public static async Task<DbQueryResult<TResult>> MinAsyncResult<T, TResult>(this IQueryable<T> query, Expression<Func<T, TResult>> selector, CancellationToken cancellation = default)
    {
        try
        {
            return DbQueryResult<TResult>.Success(await query.MinAsync(selector, cancellation).ConfigureAwait(false));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<TResult>.Failure(new ExceptionDbError(e));
        }
    }

    #endregion

    #region Max

    public static async Task<DbQueryResult<T>> MaxAsyncResult<T>(this IQueryable<T> query, CancellationToken cancellation = default)
    {
        try
        {
            return DbQueryResult<T>.Success(await query.MaxAsync(cancellation).ConfigureAwait(false));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<T>.Failure(new ExceptionDbError(e));
        }
    }

    public static async Task<DbQueryResult<TResult>> MaxAsyncResult<T, TResult>(this IQueryable<T> query, Expression<Func<T, TResult>> selector, CancellationToken cancellation = default)
    {
        try
        {
            return DbQueryResult<TResult>.Success(await query.MaxAsync(selector, cancellation).ConfigureAwait(false));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<TResult>.Failure(new ExceptionDbError(e));
        }
    }

    #endregion

    public static async Task<DbExecResult> ForEachAsyncResult<T>(this IQueryable<T> query, Action<T> action, CancellationToken cancellation = default)
    {
        try
        {
            int count = 0;
            await query.ForEachAsync(x => { action(x); Interlocked.Increment(ref count); }, cancellation).ConfigureAwait(false);
            return DbExecResult.Success(count);
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbExecResult.Failure(new ExceptionDbError(e));
        }
    }

    public static async Task<DbExecResult> ExecuteUpdateAsyncResult<T>(this IQueryable<T> query, Expression<Func<SetPropertyCalls<T>, SetPropertyCalls<T>>> setPropertyCalls, CancellationToken cancellation = default)
    {
        try
        {
            return DbExecResult.Success(await query.ExecuteUpdateAsync(setPropertyCalls, cancellation).ConfigureAwait(false));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbExecResult.Failure(new ExceptionDbError(e));
        }
    }

    public static async Task<DbExecResult> ExecuteDeleteAsyncResult<T>(this IQueryable<T> query, CancellationToken cancellation = default)
    {
        try
        {
            return DbExecResult.Success(await query.ExecuteDeleteAsync(cancellation).ConfigureAwait(false));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbExecResult.Failure(new ExceptionDbError(e));
        }
    }
}
