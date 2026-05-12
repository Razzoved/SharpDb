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
}
