using System.Data.Common;
using System.Transactions;

namespace SharpDb.Exceptions;

public sealed class TransactionTransientException : TransactionException
{
    public TransactionTransientException(Exception e) : this(e.Message, e) { }
    public TransactionTransientException(string originalErrorMessage, Exception e) : base(originalErrorMessage, e)
    {
        DbException? dbException = e.GetTransientDbError()
            ?? throw new ArgumentException("No transient database error found in the exception chain.", nameof(e));
        DbException = dbException;
    }

    public DbException DbException { get; }
}
