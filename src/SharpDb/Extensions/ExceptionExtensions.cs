using System.Data.Common;

namespace SharpDb.Extensions;

public static class ExceptionExtensions
{
    public static bool HasDbError(this Exception exception, byte searchDepth = byte.MaxValue)
    {
        Exception? currentException = exception;
        while (currentException is not null && searchDepth-- > 0)
        {
            if (currentException is DbException)
            {
                return true;
            }
            currentException = exception.InnerException;
        }
        return false;
    }

    public static bool HasTransientDbError(this Exception exception, byte searchDepth = byte.MaxValue)
    {
        Exception? currentException = exception;
        while (currentException is not null && searchDepth-- > 0)
        {
            if (currentException is DbException { IsTransient: true })
            {
                return true;
            }
            currentException = exception.InnerException;
        }
        return false;
    }

    public static TResult ThrowIfFailed<TResult>(this TResult result) where TResult : IDbResult
    {
        if (!result.IsSuccess)
        {
            switch (result.Error)
            {
                case ExceptionDbError error:
                    throw error.Exception;
                case StringDbError error:
                    throw new Exception(error.Message);
                default:
                    throw new Exception(result.Error.ToString());
            }
        }
        return result;
    }

    public static Task<TResult> ThrowIfFailed<TResult>(this Task<TResult> result) where TResult : IDbResult
    {
        return result.ContinueWith(task =>
        {
            if (task.IsCompletedSuccessfully)
                return task.Result.ThrowIfFailed();
            else if (task.IsFaulted && task.Exception is not null)
                throw task.Exception;
            else
                throw new Exception("An error occurred during asynchronous operation.");
        }, TaskContinuationOptions.ExecuteSynchronously);
    }

    public static Task<TResult> ThrowIfFailed<TResult>(this ValueTask<TResult> result) where TResult : IDbResult
    {
        return result.AsTask().ThrowIfFailed();
    }
}
