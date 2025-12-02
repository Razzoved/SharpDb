using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace SharpDb.EntityFrameworkCore;

internal sealed class TransactionContext : IDisposable
{
    private static readonly AsyncLocal<IDbContextTransaction?> s_transaction = new();
    private static readonly AsyncLocal<ChangeJournal?> s_changeJournal = new();
    private static readonly AsyncLocal<UIntBox> s_affectedRows = new();
    private readonly IDbContextTransaction? _savedTransaction;
    private readonly ChangeJournal? _savedChangeJournal;
    private readonly uint _savedAffectedRows;
    private bool _disposed;

    public TransactionContext(DbContext context)
    {
        ArgumentNullException.ThrowIfNull(context, nameof(context));
        ArgumentNullException.ThrowIfNull(context.Database.CurrentTransaction, nameof(context));
        s_affectedRows.Value ??= new UIntBox();
        _savedTransaction = s_transaction.Value;
        _savedChangeJournal = s_changeJournal.Value;
        _savedAffectedRows = s_affectedRows.Value.BoxedValue;
        s_changeJournal.Value?.Stop();
        s_changeJournal.Value = new ChangeJournal(context);
        s_changeJournal.Value.Start();
        s_transaction.Value = context.Database.CurrentTransaction;
    }

    public static IDbContextTransaction? Transaction => s_transaction.Value;
    public static ChangeJournal? ChangeJournal => s_changeJournal.Value;
    public static uint AffectedRows => s_affectedRows.Value?.BoxedValue ?? 0;

    public static void AddAffectedRows(uint count)
    {
        if (s_affectedRows.Value is not null)
            s_affectedRows.Value.BoxedValue += count;
    }

    public void Rollback()
    {
        s_changeJournal.Value?.Restore();
        if (s_affectedRows.Value is not null)
            s_affectedRows.Value.BoxedValue = _savedAffectedRows;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            s_transaction.Value = _savedTransaction;
            s_changeJournal.Value?.Stop();
            s_changeJournal.Value = _savedChangeJournal;
            s_changeJournal.Value?.Start();
        }
    }

    private sealed class UIntBox { public uint BoxedValue; }
}
