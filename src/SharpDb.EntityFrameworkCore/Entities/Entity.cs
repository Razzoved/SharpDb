using System.ComponentModel;

namespace SharpDb.EntityFrameworkCore.Entities;

/// <summary>
/// Abstract class that can be used as a base class for any entity.
/// </summary>
public abstract class Entity
{
#pragma warning disable CS0067
    public event PropertyChangingEventHandler? PropertyChanging;
    public event PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore CS0067

    protected Entity()
    {
        if (this is INotifyPropertyChanging notifyPropertyChanging)
            notifyPropertyChanging.PropertyChanging += OnPropertyChanging;
        if (this is INotifyPropertyChanged notifyPropertyChanged)
            notifyPropertyChanged.PropertyChanged += OnPropertyChanged;
    }

    /// <summary>
    /// Only has effect when the entity implements <see cref="INotifyPropertyChanging"/>.
    /// </summary>
    /// <param name="sender">Expected to be the entity</param>
    /// <param name="e">Info about property that is changing</param>
    protected virtual void OnPropertyChanging(object? sender, PropertyChangingEventArgs e) { }

    /// <summary>
    /// Only has effect when the entity implements <see cref="INotifyPropertyChanged"/>.
    /// </summary>
    /// <param name="sender">Expected to be the entity</param>
    /// <param name="e">Info about property that has been changed</param>
    protected virtual void OnPropertyChanged(object? sender, PropertyChangedEventArgs e) { }
}

/// <summary>
/// Abstract class that can be used as a base class for any entity with a strongly-typed primary key.
/// Whenever possible, prefer using this class over the non-generic <see cref="Entity"/>.
/// </summary>
/// <typeparam name="TKey">Type of key</typeparam>
public abstract class Entity<TKey> : Entity where TKey : struct, IEquatable<TKey>
{
    public virtual TKey Id { get; set; }
}
