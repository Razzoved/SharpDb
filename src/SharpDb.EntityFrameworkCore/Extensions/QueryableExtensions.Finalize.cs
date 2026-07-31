using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace SharpDb.EntityFrameworkCore;

public static partial class QueryableExtensions
{
    #region Single(OrDefault)

    public static DbQueryResult<T> SingleResult<T>(this IQueryable<T> query)
        => SingleResultCore(query, null);

    public static DbQueryResult<T> SingleResult<T>(this IQueryable<T> query, Expression<Func<T, bool>> predicate)
        => SingleResultCore(query, predicate);

    private static DbQueryResult<T> SingleResultCore<T>(this IQueryable<T> query, Expression<Func<T, bool>>? predicate = null)
    {
        try
        {
            return DbQueryResult<T>.Success(predicate is null
                ? query.Single()
                : query.Single(predicate));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<T>.Failure(new ExceptionDbError(e));
        }
    }

    public static DbQueryResult<T?> SingleOrDefaultResult<T>(this IQueryable<T> query)
        => SingleOrDefaultResultCore(query, null);

    public static DbQueryResult<T?> SingleOrDefaultResult<T>(this IQueryable<T> query, Expression<Func<T, bool>> predicate)
        => SingleOrDefaultResultCore(query, predicate);

    private static DbQueryResult<T?> SingleOrDefaultResultCore<T>(this IQueryable<T> query, Expression<Func<T, bool>>? predicate = null)
    {
        try
        {
            return DbQueryResult<T?>.Success(predicate is null
                ? query.SingleOrDefault()
                : query.SingleOrDefault(predicate));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<T?>.Failure(new ExceptionDbError(e));
        }
    }

    #endregion

    #region First(OrDefault)

    public static DbQueryResult<T> FirstResult<T>(this IQueryable<T> query)
        => FirstResultCore(query, null);

    public static DbQueryResult<T> FirstResult<T>(this IQueryable<T> query, Expression<Func<T, bool>> predicate)
        => FirstResultCore(query, predicate);

    private static DbQueryResult<T> FirstResultCore<T>(this IQueryable<T> query, Expression<Func<T, bool>>? predicate = null)
    {
        try
        {
            return DbQueryResult<T>.Success(predicate is null
                ? query.First()
                : query.First(predicate));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<T>.Failure(new ExceptionDbError(e));
        }
    }

    public static DbQueryResult<T?> FirstOrDefaultResult<T>(this IQueryable<T> query)
        => FirstOrDefaultResultCore(query, null);

    public static DbQueryResult<T?> FirstOrDefaultResult<T>(this IQueryable<T> query, Expression<Func<T, bool>> predicate)
        => FirstOrDefaultResultCore(query, predicate);

    private static DbQueryResult<T?> FirstOrDefaultResultCore<T>(this IQueryable<T> query, Expression<Func<T, bool>>? predicate = null)
    {
        try
        {
            return DbQueryResult<T?>.Success(predicate is null
                ? query.FirstOrDefault()
                : query.FirstOrDefault(predicate));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<T?>.Failure(new ExceptionDbError(e));
        }
    }

    #endregion

    #region Last(OrDefault)

    public static DbQueryResult<T> LastResult<T>(this IQueryable<T> query)
        => LastResultCore(query, null);

    public static DbQueryResult<T> LastResult<T>(this IQueryable<T> query, Expression<Func<T, bool>> predicate)
        => LastResultCore(query, predicate);

    private static DbQueryResult<T> LastResultCore<T>(this IQueryable<T> query, Expression<Func<T, bool>>? predicate = null)
    {
        try
        {
            return DbQueryResult<T>.Success(predicate is null
                ? query.Last()
                : query.Last(predicate));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<T>.Failure(new ExceptionDbError(e));
        }
    }

    public static DbQueryResult<T?> LastOrDefaultResult<T>(this IQueryable<T> query)
        => LastOrDefaultResultCore(query, null);

    public static DbQueryResult<T?> LastOrDefaultResult<T>(this IQueryable<T> query, Expression<Func<T, bool>> predicate)
        => LastOrDefaultResultCore(query, predicate);

    private static DbQueryResult<T?> LastOrDefaultResultCore<T>(this IQueryable<T> query, Expression<Func<T, bool>>? predicate = null)
    {
        try
        {
            return DbQueryResult<T?>.Success(predicate is null
                ? query.LastOrDefault()
                : query.LastOrDefault(predicate));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<T?>.Failure(new ExceptionDbError(e));
        }
    }

    #endregion

    public static DbQueryResult<T[]> ToArrayResult<T>(this IQueryable<T> query)
    {
        try
        {
            return DbQueryResult<T[]>.Success([.. query]);
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<T[]>.Failure(new ExceptionDbError(e));
        }
    }

    public static DbQueryResult<List<T>> ToListResult<T>(this IQueryable<T> query)
    {
        try
        {
            return DbQueryResult<List<T>>.Success([.. query]);
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<List<T>>.Failure(new ExceptionDbError(e));
        }
    }

    #region ToHashSet

    public static DbQueryResult<HashSet<T>> ToHashSetResult<T>(this IQueryable<T> query)
        => ToHashSetResultCore(query, null);

    public static DbQueryResult<HashSet<T>> ToHashSetResult<T>(this IQueryable<T> query, IEqualityComparer<T> comparer)
        => ToHashSetResultCore(query, comparer);

    private static DbQueryResult<HashSet<T>> ToHashSetResultCore<T>(this IQueryable<T> query, IEqualityComparer<T>? comparer = null)
    {
        try
        {
            return DbQueryResult<HashSet<T>>.Success(comparer is null ? [.. query] : new HashSet<T>(query, comparer));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<HashSet<T>>.Failure(new ExceptionDbError(e));
        }
    }

    #endregion

    #region ToDictionary

    public static Task<DbQueryResult<Dictionary<TKey, T>>> ToDictionaryResult<T, TKey>(IQueryable<T> query, Func<T, TKey> keySelector) where TKey : notnull
        => ToDictionaryResultCore(query, keySelector, null, null);

    public static Task<DbQueryResult<Dictionary<TKey, T>>> ToDictionaryResult<T, TKey>(IQueryable<T> query, Func<T, TKey> keySelector, Func<T, T> elementSelector) where TKey : notnull
        => ToDictionaryResultCore(query, keySelector, elementSelector, null);

    public static Task<DbQueryResult<Dictionary<TKey, T>>> ToDictionaryResult<T, TKey>(IQueryable<T> query, Func<T, TKey> keySelector, IEqualityComparer<TKey> comparer) where TKey : notnull
        => ToDictionaryResultCore(query, keySelector, null, comparer);

    public static Task<DbQueryResult<Dictionary<TKey, T>>> ToDictionaryResult<T, TKey>(IQueryable<T> query, Func<T, TKey> keySelector, Func<T, T> elementSelector, IEqualityComparer<TKey> comparer) where TKey : notnull
        => ToDictionaryResultCore(query, keySelector, elementSelector, comparer);

    private static async Task<DbQueryResult<Dictionary<TKey, T>>> ToDictionaryResultCore<T, TKey>(
        this IQueryable<T> query,
        Func<T, TKey> keySelector,
        Func<T, T>? elementSelector = null,
        IEqualityComparer<TKey>? comparer = null) where TKey : notnull
    {
        try
        {
            return DbQueryResult<Dictionary<TKey, T>>.Success((elementSelector, comparer) switch
            {
                (null, null) => query.ToDictionary(keySelector),
                (null, not null) => query.ToDictionary(keySelector, comparer),
                (not null, null) => query.ToDictionary(keySelector, elementSelector),
                (not null, not null) => query.ToDictionary(keySelector, elementSelector, comparer)
            });
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<Dictionary<TKey, T>>.Failure(new ExceptionDbError(e));
        }
    }

    #endregion

    #region Count

    public static DbQueryResult<int> CountResult<T>(this IQueryable<T> query)
        => CountResultCore(query, null);

    public static DbQueryResult<int> CountResult<T>(this IQueryable<T> query, Expression<Func<T, bool>> predicate)
        => CountResultCore(query, predicate);

    private static DbQueryResult<int> CountResultCore<T>(this IQueryable<T> query, Expression<Func<T, bool>>? predicate = null)
    {
        try
        {
            return DbQueryResult<int>.Success(predicate is null ? query.Count() : query.Count(predicate));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<int>.Failure(new ExceptionDbError(e));
        }
    }

    public static DbQueryResult<long> LongCountResult<T>(this IQueryable<T> query)
        => LongCountResultCore(query, null);

    public static DbQueryResult<long> LongCountResult<T>(this IQueryable<T> query, Expression<Func<T, bool>> predicate)
        => LongCountResultCore(query, predicate);

    private static DbQueryResult<long> LongCountResultCore<T>(this IQueryable<T> query, Expression<Func<T, bool>>? predicate = null)
    {
        try
        {
            return DbQueryResult<long>.Success(predicate is null ? query.LongCount() : query.LongCount(predicate));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<long>.Failure(new ExceptionDbError(e));
        }
    }

    #endregion

    #region Any

    public static DbQueryResult<bool> AnyResult<T>(this IQueryable<T> query)
        => AnyResultCore(query, null);

    public static DbQueryResult<bool> AnyResult<T>(this IQueryable<T> query, Expression<Func<T, bool>> predicate)
        => AnyResultCore(query, predicate);

    private static DbQueryResult<bool> AnyResultCore<T>(this IQueryable<T> query, Expression<Func<T, bool>>? predicate = null)
    {
        try
        {
            return DbQueryResult<bool>.Success(predicate is null ? query.Any() : query.Any(predicate));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<bool>.Failure(new ExceptionDbError(e));
        }
    }

    #endregion

    public static DbQueryResult<bool> AllResult<T>(this IQueryable<T> query, Expression<Func<T, bool>> predicate)
    {
        try
        {
            return DbQueryResult<bool>.Success(query.All(predicate));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<bool>.Failure(new ExceptionDbError(e));
        }
    }

    public static DbQueryResult<bool> ContainsResult<T>(this IQueryable<T> query, T item)
    {
        try
        {
            return DbQueryResult<bool>.Success(query.Contains(item));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<bool>.Failure(new ExceptionDbError(e));
        }
    }

    #region Sum

    public static DbQueryResult<int> SumResult<T>(this IQueryable<T> query, Expression<Func<T, int>> selector)
    {
        try
        {
            return DbQueryResult<int>.Success(query.Sum(selector));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<int>.Failure(new ExceptionDbError(e));
        }
    }

    public static DbQueryResult<int?> SumResult<T>(this IQueryable<T> query, Expression<Func<T, int?>> selector)
    {
        try
        {
            return DbQueryResult<int?>.Success(query.Sum(selector));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<int?>.Failure(new ExceptionDbError(e));
        }
    }

    public static DbQueryResult<long> SumResult<T>(this IQueryable<T> query, Expression<Func<T, long>> selector)
    {
        try
        {
            return DbQueryResult<long>.Success(query.Sum(selector));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<long>.Failure(new ExceptionDbError(e));
        }
    }

    public static DbQueryResult<long?> SumResult<T>(this IQueryable<T> query, Expression<Func<T, long?>> selector)
    {
        try
        {
            return DbQueryResult<long?>.Success(query.Sum(selector));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<long?>.Failure(new ExceptionDbError(e));
        }
    }

    public static DbQueryResult<float> SumResult<T>(this IQueryable<T> query, Expression<Func<T, float>> selector)
    {
        try
        {
            return DbQueryResult<float>.Success(query.Sum(selector));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<float>.Failure(new ExceptionDbError(e));
        }
    }

    public static DbQueryResult<float?> SumResult<T>(this IQueryable<T> query, Expression<Func<T, float?>> selector)
    {
        try
        {
            return DbQueryResult<float?>.Success(query.Sum(selector));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<float?>.Failure(new ExceptionDbError(e));
        }
    }

    public static DbQueryResult<double> SumResult<T>(this IQueryable<T> query, Expression<Func<T, double>> selector)
    {
        try
        {
            return DbQueryResult<double>.Success(query.Sum(selector));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<double>.Failure(new ExceptionDbError(e));
        }
    }

    public static DbQueryResult<double?> SumResult<T>(this IQueryable<T> query, Expression<Func<T, double?>> selector)
    {
        try
        {
            return DbQueryResult<double?>.Success(query.Sum(selector));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<double?>.Failure(new ExceptionDbError(e));
        }
    }

    public static DbQueryResult<decimal> SumResult<T>(this IQueryable<T> query, Expression<Func<T, decimal>> selector)
    {
        try
        {
            return DbQueryResult<decimal>.Success(query.Sum(selector));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<decimal>.Failure(new ExceptionDbError(e));
        }
    }

    public static DbQueryResult<decimal?> SumResult<T>(this IQueryable<T> query, Expression<Func<T, decimal?>> selector)
    {
        try
        {
            return DbQueryResult<decimal?>.Success(query.Sum(selector));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<decimal?>.Failure(new ExceptionDbError(e));
        }
    }

    #endregion

    #region Average

    public static DbQueryResult<double> AverageResult<T>(this IQueryable<T> query, Expression<Func<T, int>> selector)
    {
        try
        {
            return DbQueryResult<double>.Success(query.Average(selector));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<double>.Failure(new ExceptionDbError(e));
        }
    }

    public static DbQueryResult<double?> AverageResult<T>(this IQueryable<T> query, Expression<Func<T, int?>> selector)
    {
        try
        {
            return DbQueryResult<double?>.Success(query.Average(selector));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<double?>.Failure(new ExceptionDbError(e));
        }
    }

    public static DbQueryResult<double> AverageResult<T>(this IQueryable<T> query, Expression<Func<T, long>> selector)
    {
        try
        {
            return DbQueryResult<double>.Success(query.Average(selector));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<double>.Failure(new ExceptionDbError(e));
        }
    }

    public static DbQueryResult<double?> AverageResult<T>(this IQueryable<T> query, Expression<Func<T, long?>> selector)
    {
        try
        {
            return DbQueryResult<double?>.Success(query.Average(selector));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<double?>.Failure(new ExceptionDbError(e));
        }
    }

    public static DbQueryResult<float> AverageResult<T>(this IQueryable<T> query, Expression<Func<T, float>> selector)
    {
        try
        {
            return DbQueryResult<float>.Success(query.Average(selector));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<float>.Failure(new ExceptionDbError(e));
        }
    }

    public static DbQueryResult<float?> AverageResult<T>(this IQueryable<T> query, Expression<Func<T, float?>> selector)
    {
        try
        {
            return DbQueryResult<float?>.Success(query.Average(selector));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<float?>.Failure(new ExceptionDbError(e));
        }
    }

    public static DbQueryResult<double> AverageResult<T>(this IQueryable<T> query, Expression<Func<T, double>> selector)
    {
        try
        {
            return DbQueryResult<double>.Success(query.Average(selector));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<double>.Failure(new ExceptionDbError(e));
        }
    }

    public static DbQueryResult<double?> AverageResult<T>(this IQueryable<T> query, Expression<Func<T, double?>> selector)
    {
        try
        {
            return DbQueryResult<double?>.Success(query.Average(selector));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<double?>.Failure(new ExceptionDbError(e));
        }
    }

    public static DbQueryResult<decimal> AverageResult<T>(this IQueryable<T> query, Expression<Func<T, decimal>> selector)
    {
        try
        {
            return DbQueryResult<decimal>.Success(query.Average(selector));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<decimal>.Failure(new ExceptionDbError(e));
        }
    }

    public static DbQueryResult<decimal?> AverageResult<T>(this IQueryable<T> query, Expression<Func<T, decimal?>> selector)
    {
        try
        {
            return DbQueryResult<decimal?>.Success(query.Average(selector));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<decimal?>.Failure(new ExceptionDbError(e));
        }
    }

    #endregion

    #region Min

    public static DbQueryResult<T?> MinResult<T>(this IQueryable<T> query)
    {
        try
        {
            return DbQueryResult<T?>.Success(query.Min());
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<T?>.Failure(new ExceptionDbError(e));
        }
    }

    public static DbQueryResult<TResult?> MinResult<T, TResult>(this IQueryable<T> query, Expression<Func<T, TResult>> selector)
    {
        try
        {
            return DbQueryResult<TResult?>.Success(query.Min(selector));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<TResult?>.Failure(new ExceptionDbError(e));
        }
    }

    #endregion

    #region Max

    public static DbQueryResult<T?> MaxResult<T>(this IQueryable<T> query)
    {
        try
        {
            return DbQueryResult<T?>.Success(query.Max());
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<T?>.Failure(new ExceptionDbError(e));
        }
    }

    public static DbQueryResult<TResult?> MaxResult<T, TResult>(this IQueryable<T> query, Expression<Func<T, TResult>> selector)
    {
        try
        {
            return DbQueryResult<TResult?>.Success(query.Max(selector));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbQueryResult<TResult?>.Failure(new ExceptionDbError(e));
        }
    }

    #endregion

    public static DbExecResult ExecuteUpdateResult<T>(this IQueryable<T> query, Expression<Func<SetPropertyCalls<T>, SetPropertyCalls<T>>> setPropertyCalls)
    {
        try
        {
            return DbExecResult.Success(query.ExecuteUpdate(setPropertyCalls));
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbExecResult.Failure(new ExceptionDbError(e));
        }
    }

    public static DbExecResult ExecuteDeleteResult<T>(this IQueryable<T> query)
    {
        try
        {
            return DbExecResult.Success(query.ExecuteDelete());
        }
        catch (Exception e)
        {
            ThrowIfTransient(query, e);
            return DbExecResult.Failure(new ExceptionDbError(e));
        }
    }
}
