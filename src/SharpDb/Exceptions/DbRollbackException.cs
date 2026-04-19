using System.Data.Common;

namespace SharpDb.Exceptions;

public class DbRollbackException : DbException
{
    public DbRollbackException(string originalErrorMessage, Exception e) : base(originalErrorMessage, e) { }

    public override bool IsTransient => InnerException is DbException { IsTransient: true };
    public override int ErrorCode => InnerException is DbException e ? e.ErrorCode : default;
}
