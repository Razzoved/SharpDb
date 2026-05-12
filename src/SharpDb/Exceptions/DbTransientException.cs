using System.Collections;
using System.Data.Common;

namespace SharpDb.Exceptions;

public sealed class DbTransientException : DbException
{
    public DbTransientException(Exception e) : base(e.Message, e) { }
    public DbTransientException(string originalErrorMessage, Exception? e) : base(originalErrorMessage, e) { }
    public DbTransientException(IDbError e) : this(e.Message, e is ExceptionDbError { Exception: Exception ex } ? ex : null) { }

    public override bool IsTransient => true;
    public override int ErrorCode => InnerException is DbException e ? e.ErrorCode : default;
    public override IDictionary Data => InnerException is DbException e ? e.Data : base.Data;

    protected override DbBatchCommand? DbBatchCommand => InnerException is DbException e ? e.BatchCommand : null;
}
