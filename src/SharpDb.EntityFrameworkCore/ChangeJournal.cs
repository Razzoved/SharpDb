using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace SharpDb.EntityFrameworkCore;

internal sealed class ChangeJournal : IChangeJournal
{
    private const int InitialCapacity = 32;

    private readonly DbContext _db;
    private readonly Dictionary<INotifyPropertyChanging, int> _trackingChanges = new(InitialCapacity, ReferenceEqualityComparer.Instance);
    private readonly Dictionary<INotifyPropertyChanging, Dictionary<PropertyKey, object?>> _changed = new(InitialCapacity, ReferenceEqualityComparer.Instance);
    private readonly Stack<Operation> _ops = new(InitialCapacity * 2);

    private int _currentSavepointId;

    public ChangeJournal(DbContext db)
    {
        ArgumentNullException.ThrowIfNull(db);
        _db = db;
    }

    public void Start()
    {
        if (_currentSavepointId == 0)
        {
            _db.ChangeTracker.Tracked += OnEntityTracked;
            _db.ChangeTracker.StateChanged += OnEntityStateChanged;
        }
        _ops.Push(Operation.SavePoint(ref _currentSavepointId));
        foreach (var entry in _db.ChangeTracker.Entries())
        {
            _ops.Push(Operation.StateRestore(entry, entry.State));
            CaptureValues(entry);
        }
    }

    public void Stop()
    {
        _db.ChangeTracker.Tracked -= OnEntityTracked;
        _db.ChangeTracker.StateChanged -= OnEntityStateChanged;

        // Release savepoint
        while (_ops.TryPop(out var op))
        {
            if (op.Type == OperationType.SavePoint)
            {
                _currentSavepointId = (int)op.Data1;
                break;
            }
            ReleaseOperation(op);
        }

        // Reduce capacity to avoid memory overhead
        _trackingChanges.TrimExcess(InitialCapacity);
        _changed.TrimExcess(InitialCapacity);
#if NET10_0_OR_GREATER
        _ops.TrimExcess(InitialCapacity * 2);
#else
        _ops.TrimExcess();
#endif

        if (_currentSavepointId > 0)
        {
            _db.ChangeTracker.Tracked += OnEntityTracked;
            _db.ChangeTracker.StateChanged += OnEntityStateChanged;
        }
    }

    public void Restore()
    {
        if (_ops.TryPeek(out var op) && op.Type != OperationType.SavePoint)
        {
            _db.ChangeTracker.Tracked -= OnEntityTracked;
            _db.ChangeTracker.StateChanged -= OnEntityStateChanged;

            while (_ops.TryPop(out op))
            {
                if (op.Type == OperationType.SavePoint)
                {
                    _ops.Push(op);
                    break;
                }
                UndoOperation(op);
            }

            _db.ChangeTracker.Tracked += OnEntityTracked;
            _db.ChangeTracker.StateChanged += OnEntityStateChanged;
        }
    }

    private void OnEntityTracked(object? sender, EntityTrackedEventArgs e)
    {
        _ops.Push(Operation.Detach(e.Entry));
        CaptureValues(e.Entry);
    }

    private void OnEntityStateChanged(object? sender, EntityStateChangedEventArgs e)
    {
        _ops.Push(Operation.StateRestore(e.Entry, e.OldState));
    }

    private void OnEntityPropertyChanging(object? sender, PropertyChangingEventArgs e)
    {
        if (sender is not INotifyPropertyChanging notifier || e.PropertyName is null) return;

        // Create snapshot dictionary if needed
        if (!_changed.TryGetValue(notifier, out var dict))
        {
            dict = new Dictionary<PropertyKey, object?>(4);
            _changed.Add(notifier, dict);
        }

        // Take snapshot on FIRST change of property only
        PropertyKey propertyKey = new(_currentSavepointId, e.PropertyName);
        if (!dict.ContainsKey(propertyKey))
        {
            object? currentValue = _db.Entry(sender).CurrentValues[propertyKey.PropertyName];
            dict.Add(propertyKey, currentValue);
        }
    }

    private void CaptureValues(in EntityEntry entry)
    {
        if (entry.Entity is INotifyPropertyChanging notifier)
        {
            if (_trackingChanges.TryAdd(notifier, _currentSavepointId))
                notifier.PropertyChanging += OnEntityPropertyChanging;
            _ops.Push(Operation.PropertyRestore(entry));
        }
        else
        {
            _ops.Push(Operation.SnapshotRestore(entry, entry.CurrentValues.Clone()));
        }
    }

    private void ReleaseOperation(in Operation op)
    {
        if (op.Type == OperationType.PropertyRestore)
        {
            var entry = (EntityEntry)op.Data1;
            var notifier = (INotifyPropertyChanging)entry.Entity;
            if (_trackingChanges.TryGetValue(notifier, out int savePointId))
            {
                if (savePointId >= _currentSavepointId)
                {
                    notifier.PropertyChanging -= OnEntityPropertyChanging;
                    _changed.Remove(notifier);
                    _trackingChanges.Remove(notifier);
                }
                else if (_changed.TryGetValue(notifier, out var dict))
                {
                    foreach (var d in dict.Keys.Where(x => x.SavePointId == savePointId).ToArray())
                    {
                        dict.Remove(d);
                    }
                    if (dict.Count == 0) _changed.Remove(notifier);
                }
            }
        }
    }

    private void UndoOperation(in Operation op)
    {
        switch (op.Type)
        {
            case OperationType.Detach:
                {
                    var entry = (EntityEntry)op.Data1;
                    entry.State = EntityState.Detached;
                }
                break;
            case OperationType.StateRestore:
                {
                    var entry = (EntityEntry)op.Data1;
                    entry.State = (EntityState)op.Data2;
                }
                break;
            case OperationType.PropertyRestore:
                {
                    var entry = (EntityEntry)op.Data1;
                    var notifier = (INotifyPropertyChanging)entry.Entity;
                    if (_trackingChanges.TryGetValue(notifier, out int savePointId))
                    {
                        notifier.PropertyChanging -= OnEntityPropertyChanging;

                        if (_changed.TryGetValue(notifier, out var dict))
                        {
                            List<PropertyKey> toRemove = [];
                            foreach (var d in dict)
                            {
                                if (d.Key.SavePointId == _currentSavepointId)
                                {
                                    var prop = entry.Metadata.FindProperty(d.Key.PropertyName);
                                    if (prop is not null)
                                        entry.CurrentValues[prop] = d.Value;
                                    else
                                        entry.Metadata.ClrType?.GetProperty(d.Key.PropertyName)?.SetValue(entry.Entity, d.Value);
                                    toRemove.Add(d.Key);
                                }
                            }
                            foreach (var key in toRemove) dict.Remove(key);
                            if (dict.Count == 0) _changed.Remove(notifier);
                        }

                        if (savePointId >= _currentSavepointId)
                            _trackingChanges.Remove(notifier);
                        else
                            notifier.PropertyChanging += OnEntityPropertyChanging;
                    }
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

    private enum OperationType { SavePoint, Detach, StateRestore, SnapshotRestore, PropertyRestore }

    private readonly struct Operation(OperationType type)
    {
        public readonly OperationType Type = type;
        public object Data1 { get; private init; } = null!;
        public object Data2 { get; private init; } = null!;

        public static Operation SavePoint(ref int savePointId)
            => new(OperationType.SavePoint) { Data1 = Interlocked.Increment(ref savePointId) };

        public static Operation Detach(EntityEntry entry)
            => new(OperationType.Detach) { Data1 = entry };

        public static Operation StateRestore(EntityEntry entry, EntityState previousState)
            => new(OperationType.StateRestore) { Data1 = entry, Data2 = previousState };

        public static Operation SnapshotRestore(EntityEntry entry, PropertyValues snapshot)
            => new(OperationType.SnapshotRestore) { Data1 = entry, Data2 = snapshot };

        public static Operation PropertyRestore(EntityEntry entry)
            => new(OperationType.PropertyRestore) { Data1 = entry };
    }

    private readonly record struct PropertyKey(int SavePointId, string PropertyName);
}
