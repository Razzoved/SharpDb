using System.Data.Common;
using System.Transactions;

namespace SharpDb.Exceptions;

public sealed class TransactionTransientException : TransactionException
{
    public TransactionTransientException(Exception e) : this(e.Message, e) { }
    public TransactionTransientException(string originalErrorMessage, Exception e) : base(originalErrorMessage, e)
    {
        DbException? dbException = e.GetTransientDbError()
            ?? throw new ArgumentException(Resources.Text_Error_Transaction_NoTransientErrorFound, nameof(e));
        DbException = dbException;
    }

    public DbException DbException { get; }
}
