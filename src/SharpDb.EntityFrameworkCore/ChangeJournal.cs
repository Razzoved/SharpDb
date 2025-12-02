using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace SharpDb.EntityFrameworkCore;

internal sealed class ChangeJournal : IChangeJournal
{
    private readonly DbContext _db;
    private readonly HashSet<object> _tracked = [];
    private readonly Stack<IOperation> _ops = [];

    private bool _stopped = true;
    private bool _restored;

    public ChangeJournal(DbContext db)
    {
        ArgumentNullException.ThrowIfNull(db, nameof(db));
        _db = db;

        // Capture existing tracked entities, otherwise they would be missed
        // I might improve this later on, but for now this should be sufficient.
        foreach (var entry in _db.ChangeTracker.Entries())
        {
            if (_tracked.Add(entry.Entity))
            {
                _ops.Push(entry.Entity is INotifyPropertyChanging and INotifyPropertyChanged
                    ? new PropertyRestoreOperation(entry)
                    : new SnapshotRestoreOperation(entry));
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
                var op = _ops.Pop();
                op.Undo(_db);
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
            _ops.Push(new DetachOperation(entry));
            _ops.Push(entry.Entity is INotifyPropertyChanging and INotifyPropertyChanged
                ? new PropertyRestoreOperation(entry)
                : new SnapshotRestoreOperation(entry));
        }
    }

    private void OnEntityStateChanged(object? sender, EntityStateChangedEventArgs e)
    {
        var entry = e.Entry;
        _ops.Push(new StateRestoreOperation(entry, e.OldState));
        if (_tracked.Add(entry.Entity))
        {
            _ops.Push(entry.Entity is INotifyPropertyChanging and INotifyPropertyChanged
                ? new PropertyRestoreOperation(entry)
                : new SnapshotRestoreOperation(entry));
        }
    }

    /// <summary>
    /// Operations that can be undone.
    /// </summary>
    private interface IOperation
    {
        void Undo(DbContext context);
    }

    private sealed class DetachOperation(EntityEntry entry) : IOperation
    {
        public void Undo(DbContext context)
        {
            entry.State = EntityState.Detached;
        }
    }

    private sealed class StateRestoreOperation(EntityEntry entry, EntityState previousState) : IOperation
    {
        public void Undo(DbContext context)
        {
            entry.State = previousState;
        }
    }

    private sealed class SnapshotRestoreOperation : IOperation
    {
        private readonly PropertyValues _currentValues;
        private readonly PropertyValues _snapshot;

        public SnapshotRestoreOperation(EntityEntry entry)
        {
            _currentValues = entry.CurrentValues;
            _snapshot = entry.CurrentValues.Clone();
        }

        public void Undo(DbContext context)
        {
            _currentValues.SetValues(_snapshot);
        }
    }

    private sealed class PropertyRestoreOperation : IOperation
    {
        private readonly object _source;
        private readonly PropertyValues _currentValues;
        private readonly Dictionary<string, (bool IsChanged, object? Value)> _changedValues;

        public PropertyRestoreOperation(EntityEntry entry)
        {
            _source = entry.Entity;
            _currentValues = entry.CurrentValues;
            _changedValues = [];
            ((INotifyPropertyChanging)_source).PropertyChanging += OnPropertyChanging;
            ((INotifyPropertyChanged)_source).PropertyChanged += OnPropertyChanged;
        }

        public void Undo(DbContext context)
        {
            ((INotifyPropertyChanged)_source).PropertyChanged -= OnPropertyChanged;
            ((INotifyPropertyChanging)_source).PropertyChanging -= OnPropertyChanging;
            foreach (var (propertyName, data) in _changedValues)
            {
                if (data.IsChanged)
                {
                    _currentValues[propertyName] = data.Value;
                }
            }
        }

        private void OnPropertyChanging(object? sender, PropertyChangingEventArgs e)
        {
            string? propertyName = e.PropertyName;
            if (!string.IsNullOrWhiteSpace(propertyName))
            {
                ref var changedValue = ref CollectionsMarshal.GetValueRefOrNullRef(_changedValues, propertyName);
                if (Unsafe.IsNullRef(ref changedValue))
                {
                    changedValue = (false, _currentValues[propertyName]);
                }
            }
        }

        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            string? propertyName = e.PropertyName;
            if (!string.IsNullOrWhiteSpace(propertyName))
            {
                ref var changedValue = ref CollectionsMarshal.GetValueRefOrNullRef(_changedValues, propertyName);
                if (!Unsafe.IsNullRef(ref changedValue))
                {
                    changedValue = (true, changedValue.Value);
                }
            }
        }
    }
}
