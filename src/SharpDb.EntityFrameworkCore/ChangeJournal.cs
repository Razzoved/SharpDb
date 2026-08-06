using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace SharpDb.EntityFrameworkCore;

internal sealed class ChangeJournal : IChangeJournal
{
    private const int InitialCapacity = 32;

    private readonly DbContext _db;
    private readonly HashSet<object> _tracked = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<object, Dictionary<string, object?>> _entitySnapshots = new(InitialCapacity / 2, ReferenceEqualityComparer.Instance);
    private readonly Stack<Operation> _ops = new(InitialCapacity);

    private bool _stopped = true;
    private bool _restored;

    public ChangeJournal(DbContext db)
    {
        ArgumentNullException.ThrowIfNull(db);
        _db = db;

        // Capture existing tracked entities, otherwise they would be missed
        // I might improve this later on, but for now this should be sufficient.
        foreach (var entry in _db.ChangeTracker.Entries())
        {
            if (_tracked.Add(entry.Entity))
            {
                CaptureValues(entry);
            }
        }
    }

    public void Start()
    {
        _stopped = false;
        _db.ChangeTracker.Tracked += OnEntityTracked;
        _db.ChangeTracker.StateChanged += OnEntityStateChanged;
    }

    public void Stop()
    {
        if (!_stopped)
        {
            _stopped = true;
            _db.ChangeTracker.StateChanged -= OnEntityStateChanged;
            _db.ChangeTracker.Tracked -= OnEntityTracked;
        }
    }

    public void Restore()
    {
        if (!_restored)
        {
            Stop();
            while (_ops.Count > 0)
            {
                Undo(_ops.Pop());
            }
            _tracked.Clear();
            _restored = true;
        }
    }

    private void OnEntityTracked(object? sender, EntityTrackedEventArgs e)
    {
        var entry = e.Entry;
        if (_tracked.Add(entry.Entity))
        {
            _ops.Push(Operation.Detach(entry));
            CaptureValues(entry);
        }
    }

    private void OnEntityStateChanged(object? sender, EntityStateChangedEventArgs e)
    {
        var entry = e.Entry;
        _ops.Push(Operation.StateRestore(entry, e.OldState));
        if (_tracked.Add(entry.Entity))
        {
            CaptureValues(entry);
        }
    }

    private void OnEntityPropertyChanging(object? sender, PropertyChangingEventArgs e)
    {
        if (sender is not null && e.PropertyName is not null)
        {
            // Create snapshot dictionary if needed
            if (!_entitySnapshots.TryGetValue(sender, out var dict))
            {
                dict = new Dictionary<string, object?>(4);
                _entitySnapshots.Add(sender, dict);
            }
            // Take snapshot on FIRST change of property only
            if (!dict.ContainsKey(e.PropertyName))
            {
                object? currentValue = _db.Entry(sender).CurrentValues[e.PropertyName];
                dict.Add(e.PropertyName, currentValue);
            }
        }
    }

    private void CaptureValues(in EntityEntry entry)
    {
        if (entry.Entity is INotifyPropertyChanging notifier)
        {
            notifier.PropertyChanging += OnEntityPropertyChanging;
            _ops.Push(Operation.PropertyRestore(entry));
        }
        else
        {
            _ops.Push(Operation.SnapshotRestore(entry, entry.CurrentValues.Clone()));
        }
    }

    private void Undo(in Operation op)
    {
        switch (op.Type)
        {
            case OperationType.Detach:
                {
                    var entry = (EntityEntry)op.Data1;
                    entry.State = EntityState.Detached;
                }
                break;
            case OperationType.PropertyRestore:
                {
                    var entry = (EntityEntry)op.Data1;
                    ((INotifyPropertyChanging)entry.Entity).PropertyChanging -= OnEntityPropertyChanging;
                    if (_entitySnapshots.TryGetValue(entry.Entity, out var dict))
                    {
                        foreach (var d in dict)
                        {
                            entry.CurrentValues[d.Key] = d.Value;
                        }
                        _entitySnapshots.Remove(entry);
                    }
                }
                break;
            case OperationType.StateRestore:
                {
                    var entry = (EntityEntry)op.Data1;
                    entry.State = (EntityState)op.Data2;
                }
                break;
            case OperationType.SnapshotRestore:
                {
                    var entry = (EntityEntry)op.Data1;
                    entry.CurrentValues.SetValues((PropertyValues)op.Data2);
                }
                break;
        }
    }

    private enum OperationType { Detach, StateRestore, SnapshotRestore, PropertyRestore }

    private readonly struct Operation(OperationType type)
    {
        public readonly OperationType Type = type;
        public object Data1 { get; private init; } = null!;
        public object Data2 { get; private init; } = null!;

        public static Operation Detach(EntityEntry entry)
            => new(OperationType.Detach) { Data1 = entry };

        public static Operation StateRestore(EntityEntry entry, EntityState previousState)
            => new(OperationType.StateRestore) { Data1 = entry, Data2 = previousState };

        public static Operation SnapshotRestore(EntityEntry entry, PropertyValues snapshot)
            => new(OperationType.SnapshotRestore) { Data1 = entry, Data2 = snapshot };

        public static Operation PropertyRestore(EntityEntry entry)
            => new(OperationType.PropertyRestore) { Data1 = entry };
    }
}
