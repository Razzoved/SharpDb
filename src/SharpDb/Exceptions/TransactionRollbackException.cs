using System.Transactions;

namespace SharpDb.Exceptions;

public class TransactionRollbackException : TransactionException
{
    public TransactionRollbackException(IDbError error, Exception? rollbackException) : base(error.Message, error is ExceptionDbError exError ? exError.Exception : null)
    {
        DbError = error;
        RollbackException = rollbackException;
    }

    public IDbError DbError { get; }
    public Exception? RollbackException { get; }
    public override string Message => DbError.Message;
}
