using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace SharpDb.EntityFrameworkCore;

internal sealed class ChangeJournal : IChangeJournal
{
    private const int InitialCapacity = 32;

    private readonly DbContext _db;
    private readonly Stack<Operation> _ops = new(InitialCapacity);
    private readonly HashSet<PropertyChange> _propertyChanges = new(InitialCapacity);

    private readonly EventHandler<EntityTrackedEventArgs> _onEntityTracked;
    private readonly EventHandler<EntityStateChangedEventArgs> _onEntityStateChanged;
    private readonly PropertyChangingEventHandler _onPropertyChanging;

    private int _currentSavepointId;

    public ChangeJournal(DbContext db)
    {
        ArgumentNullException.ThrowIfNull(db);
        _db = db;
        _onEntityTracked = OnEntityTracked;
        _onEntityStateChanged = OnEntityStateChanged;
        _onPropertyChanging = OnEntityPropertyChanging;
    }

    public void Start()
    {
        if (_currentSavepointId == 0)
        {
            _db.ChangeTracker.Tracked += _onEntityTracked;
            _db.ChangeTracker.StateChanged += _onEntityStateChanged;
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
        _db.ChangeTracker.Tracked -= _onEntityTracked;
        _db.ChangeTracker.StateChanged -= _onEntityStateChanged;

        // Release savepoint
        while (_ops.TryPop(out var op))
        {
            if (op.Type == OperationType.SavePoint)
            {
                _currentSavepointId = (int)op.Data!;
                break;
            }
            ReleaseOperation(op);
        }

        // Reduce capacity to avoid memory overhead
#if NET10_0_OR_GREATER
        _propertyChanges.TrimExcess(InitialCapacity);
        _ops.TrimExcess(InitialCapacity * 2);
#else
        _propertyChanges.TrimExcess();
        _ops.TrimExcess();
#endif

        if (_currentSavepointId > 0)
        {
            _db.ChangeTracker.Tracked += _onEntityTracked;
            _db.ChangeTracker.StateChanged += _onEntityStateChanged;
        }
    }

    public void Restore()
    {
        if (_ops.TryPeek(out var op) && op.Type != OperationType.SavePoint)
        {
            _db.ChangeTracker.Tracked -= _onEntityTracked;
            _db.ChangeTracker.StateChanged -= _onEntityStateChanged;

            while (_ops.TryPop(out op))
            {
                if (op.Type == OperationType.SavePoint)
                {
                    _ops.Push(op);
                    break;
                }
                UndoOperation(op);
            }

            _db.ChangeTracker.Tracked += _onEntityTracked;
            _db.ChangeTracker.StateChanged += _onEntityStateChanged;
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
        if (sender is null || e.PropertyName is null) return;

        // Take snapshot on FIRST change of property only
        PropertyChange key = new(sender, _currentSavepointId, e.PropertyName);
        if (_propertyChanges.Add(key))
        {
            var entry = _db.Entry(sender);

            // Ignore properties that are not part of model. This may be changed in the future.
            var prop = entry.Metadata.FindProperty(e.PropertyName);
            if (prop is not null)
            {
                object? currentValue = entry.CurrentValues[e.PropertyName];
                _ops.Push(Operation.PropertyRestore(entry, e.PropertyName, currentValue));
            }
        }
    }

    private void CaptureValues(in EntityEntry entry)
    {
        if (entry.Entity is INotifyPropertyChanging notifier)
        {
            if (_propertyChanges.Add(PropertyChange.Marker(notifier)))
            {
                notifier.PropertyChanging += _onPropertyChanging;
                _ops.Push(Operation.UnregisterPropertyTracking(entry));
            }
        }
        else
        {
            _ops.Push(Operation.SnapshotRestore(entry, entry.CurrentValues.Clone()));
        }
    }

    private void ReleaseOperation(in Operation op)
    {
        switch (op.Type)
        {
            case OperationType.UnregisterPropertyTracking:
                ((INotifyPropertyChanging)op.Entry.Entity).PropertyChanging -= _onPropertyChanging;
                _propertyChanges.Remove(PropertyChange.Marker(op.Entry.Entity));
                break;
            case OperationType.PropertyRestore:
                {
                    (string propertyName, _) = ((string, object?))op.Data!;
                    _propertyChanges.Remove(new PropertyChange(op.Entry.Entity, _currentSavepointId, propertyName));
                }
                break;
        }
    }

    private void UndoOperation(in Operation op)
    {
        switch (op.Type)
        {
            case OperationType.Detach:
                op.Entry.State = EntityState.Detached;
                break;
            case OperationType.StateRestore:
                op.Entry.State = op.State;
                break;
            case OperationType.UnregisterPropertyTracking:
                ((INotifyPropertyChanging)op.Entry.Entity).PropertyChanging -= _onPropertyChanging;
                _propertyChanges.Remove(PropertyChange.Marker(op.Entry.Entity));
                break;
            case OperationType.PropertyRestore:
                {
                    var notifier = (INotifyPropertyChanging)op.Entry.Entity;
                    var (propertyName, value) = ((string, object?))op.Data!;
                    notifier.PropertyChanging -= _onPropertyChanging;
                    if (_propertyChanges.Remove(new(notifier, _currentSavepointId, propertyName)))
                        op.Entry.CurrentValues[propertyName] = value;
                    if (_propertyChanges.Contains(PropertyChange.Marker(notifier)))
                        notifier.PropertyChanging += _onPropertyChanging;
                }
                break;
            case OperationType.SnapshotRestore:
                op.Entry.CurrentValues.SetValues((PropertyValues)op.Data!);
                break;
        }
    }

    private enum OperationType { SavePoint, Detach, StateRestore, SnapshotRestore, UnregisterPropertyTracking, PropertyRestore }

    private readonly struct Operation(OperationType type)
    {
        public readonly OperationType Type = type;
        public EntityEntry Entry { get; private init; } = null!;
        public EntityState State { get; private init; }
        public object? Data { get; private init; }

        public static Operation SavePoint(ref int savePointId)
            => new(OperationType.SavePoint) { Data = Interlocked.Increment(ref savePointId) };

        public static Operation Detach(EntityEntry entry)
            => new(OperationType.Detach) { Entry = entry };

        public static Operation StateRestore(EntityEntry entry, EntityState previousState)
            => new(OperationType.StateRestore) { Entry = entry, State = previousState };

        public static Operation SnapshotRestore(EntityEntry entry, PropertyValues snapshot)
            => new(OperationType.SnapshotRestore) { Entry = entry, Data = snapshot };

        public static Operation UnregisterPropertyTracking(EntityEntry entry)
            => new(OperationType.UnregisterPropertyTracking) { Entry = entry };

        public static Operation PropertyRestore(EntityEntry entry, string propertyName, object? currentValue)
            => new(OperationType.PropertyRestore) { Entry = entry, Data = (propertyName, currentValue) };
    }

    private readonly record struct PropertyChange(object Entity, int SavePointId, string PropertyName)
    {
        public static PropertyChange Marker(object entity) => new(entity, 0, "");
    }
}
