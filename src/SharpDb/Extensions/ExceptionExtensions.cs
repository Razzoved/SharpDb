using System.Data.Common;

namespace SharpDb;

public static class ExceptionExtensions
{
    public static bool HasTransientDbError(this Exception exception, byte searchDepth = byte.MaxValue)
    {
        Exception? currentException = exception;
        while (currentException is not null && searchDepth-- > 0)
        {
            if (currentException is DbException dbException && IsTransient(dbException))
                return true;
            currentException = currentException.InnerException;
        }
        return false;
    }

    public static DbException? GetTransientDbError(this Exception exception, byte searchDepth = byte.MaxValue)
    {
        Exception? currentException = exception;
        while (currentException is not null && searchDepth-- > 0)
        {
            if (currentException is DbException dbException && IsTransient(dbException))
                return dbException;
            currentException = currentException.InnerException;
        }
        return null;
    }

    public static TResult ThrowIfFailed<TResult>(this TResult result) where TResult : IDbResult
        => result.IsSuccess ? result : throw result.Error.ToException();

    public static TResult ThrowIfFailed<TResult>(this TResult result, Func<IDbError, IDbError> apply) where TResult : IDbResult
        => result.IsSuccess || apply(result.Error) is NoDbError ? result : throw apply(result.Error).ToException();

    public static Task<TResult> ThrowIfFailed<TResult>(this Task<TResult> result) where TResult : IDbResult
    {
        return result.ContinueWith(task =>
        {
            if (task.IsCompletedSuccessfully)
                return task.Result.ThrowIfFailed();
            if (task is { IsFaulted: true, Exception: not null })
                throw task.Exception.Flatten().GetBaseException();
            throw new Exception("An error occurred during asynchronous operation.");
        }, TaskContinuationOptions.ExecuteSynchronously);
    }

    public static Task<TResult> ThrowIfFailed<TResult>(this Task<TResult> result, Func<IDbError, IDbError> apply) where TResult : IDbResult
    {
        return result.ContinueWith(task =>
        {
            if (task.IsCompletedSuccessfully)
                return task.Result.ThrowIfFailed(apply);
            if (task is { IsFaulted: true, Exception: not null })
                throw task.Exception.Flatten().GetBaseException();
            throw new Exception("An error occurred during asynchronous operation.");
        }, TaskContinuationOptions.ExecuteSynchronously);
    }

    public static Task<TResult> ThrowIfFailed<TResult>(this ValueTask<TResult> result) where TResult : IDbResult
        => result.AsTask().ThrowIfFailed();

    public static Task<TResult> ThrowIfFailed<TResult>(this ValueTask<TResult> result, Func<IDbError, IDbError> apply) where TResult : IDbResult
        => result.AsTask().ThrowIfFailed(apply);

    private static bool IsTransient(DbException dbException)
    {
        if (dbException.IsTransient)
            return true;
        if (dbException.ErrorCode == -2146232060 || dbException.Data["HelpLink.EvtID"] is "1205") // SQL Server deadlock
            return true;
        return false;
    }
}
