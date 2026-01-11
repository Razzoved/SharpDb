using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace SharpDb.EntityFrameworkCore;

internal sealed class TransactionContext : IDisposable
{
    private static readonly ConditionalWeakTable<IDbContextTransaction, AsyncLocal<ModifiableValue>> s_contexts = new();

    private readonly IDbContextTransaction _transaction;
    private readonly TransactionContext? _previous;
    private bool _disposed;

    public TransactionContext(DbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(dbContext.Database.CurrentTransaction, nameof(dbContext));

        _transaction = dbContext.Database.CurrentTransaction;

        var local = s_contexts.GetOrCreateValue(_transaction);
        local.Value ??= new ModifiableValue();

        _previous = local.Value.Context;
        ChangeJournal = new ChangeJournal(dbContext);
        _previous?.ChangeJournal.Stop();
        ChangeJournal.Start();

        local.Value.Context = this;
    }

    public ChangeJournal ChangeJournal { get; }
    public uint AffectedRows { get; private set; }

    public static TransactionContext? GetCurrent(DatabaseFacade facade)
    {
        return facade.CurrentTransaction is null
               || !s_contexts.TryGetValue(facade.CurrentTransaction, out var local)
               || local.Value is null
            ? null
            : local.Value.Context;
    }

    public void AddAffectedRows(uint count)
    {
        AffectedRows += count;
    }

    public void Rollback()
    {
        ChangeJournal.Restore();
        AffectedRows = 0;
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        ChangeJournal.Stop();

        if (s_contexts.TryGetValue(_transaction, out var local) && local.Value is not null)
        {
            if (_previous is not null)
            {
                _previous.ChangeJournal.Start();
                _previous.AffectedRows += AffectedRows;
            }
            local.Value.Context = _previous;
        }
    }

    public override int GetHashCode()
    {
        return _transaction.GetHashCode();
    }

    public override bool Equals(object? obj)
    {
        return obj is TransactionContext other && _transaction.Equals(other._transaction);
    }

    private sealed class ModifiableValue
    {
        public TransactionContext? Context { get; set; }
    }
}
