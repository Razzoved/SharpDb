using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace SharpDb.EntityFrameworkCore;

public static partial class QueryableExtensions
{
    #region Single(OrDefault)

    public static DbQueryResult<T> SingleResult<T>(this IQueryable<T> query)
        => query.SingleResultCore(null);

    public static DbQueryResult<T> SingleResult<T>(this IQueryable<T> query, Expression<Func<T, bool>> predicate)
        => query.SingleResultCore(predicate);

    private static DbQueryResult<T> SingleResultCore<T>(this IQueryable<T> query, Expression<Func<T, bool>>? predicate)
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
        => query.SingleOrDefaultResultCore(null);

    public static DbQueryResult<T?> SingleOrDefaultResult<T>(this IQueryable<T> query, Expression<Func<T, bool>> predicate)
        => query.SingleOrDefaultResultCore(predicate);

    private static DbQueryResult<T?> SingleOrDefaultResultCore<T>(this IQueryable<T> query, Expression<Func<T, bool>>? predicate)
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
        => query.FirstResultCore(null);

    public static DbQueryResult<T> FirstResult<T>(this IQueryable<T> query, Expression<Func<T, bool>> predicate)
        => query.FirstResultCore(predicate);

    private static DbQueryResult<T> FirstResultCore<T>(this IQueryable<T> query, Expression<Func<T, bool>>? predicate)
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
        => query.FirstOrDefaultResultCore(null);

    public static DbQueryResult<T?> FirstOrDefaultResult<T>(this IQueryable<T> query, Expression<Func<T, bool>> predicate)
        => query.FirstOrDefaultResultCore(predicate);

    private static DbQueryResult<T?> FirstOrDefaultResultCore<T>(this IQueryable<T> query, Expression<Func<T, bool>>? predicate)
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
        => query.LastResultCore(null);

    public static DbQueryResult<T> LastResult<T>(this IQueryable<T> query, Expression<Func<T, bool>> predicate)
        => query.LastResultCore(predicate);

    private static DbQueryResult<T> LastResultCore<T>(this IQueryable<T> query, Expression<Func<T, bool>>? predicate)
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
        => query.LastOrDefaultResultCore(null);

    public static DbQueryResult<T?> LastOrDefaultResult<T>(this IQueryable<T> query, Expression<Func<T, bool>> predicate)
        => query.LastOrDefaultResultCore(predicate);

    private static DbQueryResult<T?> LastOrDefaultResultCore<T>(this IQueryable<T> query, Expression<Func<T, bool>>? predicate)
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
        => query.ToHashSetResultCore(null);

    public static DbQueryResult<HashSet<T>> ToHashSetResult<T>(this IQueryable<T> query, IEqualityComparer<T> comparer)
        => query.ToHashSetResultCore(comparer);

    private static DbQueryResult<HashSet<T>> ToHashSetResultCore<T>(this IQueryable<T> query, IEqualityComparer<T>? comparer)
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

    public static DbQueryResult<Dictionary<TKey, T>> ToDictionaryResult<T, TKey>(IQueryable<T> query, Func<T, TKey> keySelector) where TKey : notnull
        => query.ToDictionaryResultCore(keySelector, null, null);

    public static DbQueryResult<Dictionary<TKey, T>> ToDictionaryResult<T, TKey>(IQueryable<T> query, Func<T, TKey> keySelector, Func<T, T> elementSelector) where TKey : notnull
        => query.ToDictionaryResultCore(keySelector, elementSelector, null);

    public static DbQueryResult<Dictionary<TKey, T>> ToDictionaryResult<T, TKey>(IQueryable<T> query, Func<T, TKey> keySelector, IEqualityComparer<TKey> comparer) where TKey : notnull
        => query.ToDictionaryResultCore(keySelector, null, comparer);

    public static DbQueryResult<Dictionary<TKey, T>> ToDictionaryResult<T, TKey>(IQueryable<T> query, Func<T, TKey> keySelector, Func<T, T> elementSelector, IEqualityComparer<TKey> comparer) where TKey : notnull
        => query.ToDictionaryResultCore(keySelector, elementSelector, comparer);

    private static DbQueryResult<Dictionary<TKey, T>> ToDictionaryResultCore<T, TKey>(
        this IQueryable<T> query,
        Func<T, TKey> keySelector,
        Func<T, T>? elementSelector,
        IEqualityComparer<TKey>? comparer) where TKey : notnull
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
        => query.CountResultCore(null);

    public static DbQueryResult<int> CountResult<T>(this IQueryable<T> query, Expression<Func<T, bool>> predicate)
        => query.CountResultCore(predicate);

    private static DbQueryResult<int> CountResultCore<T>(this IQueryable<T> query, Expression<Func<T, bool>>? predicate)
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
        => query.LongCountResultCore(null);

    public static DbQueryResult<long> LongCountResult<T>(this IQueryable<T> query, Expression<Func<T, bool>> predicate)
        => query.LongCountResultCore(predicate);

    private static DbQueryResult<long> LongCountResultCore<T>(this IQueryable<T> query, Expression<Func<T, bool>>? predicate)
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
        => query.AnyResultCore(null);

    public static DbQueryResult<bool> AnyResult<T>(this IQueryable<T> query, Expression<Func<T, bool>> predicate)
        => query.AnyResultCore(predicate);

    private static DbQueryResult<bool> AnyResultCore<T>(this IQueryable<T> query, Expression<Func<T, bool>>? predicate)
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
