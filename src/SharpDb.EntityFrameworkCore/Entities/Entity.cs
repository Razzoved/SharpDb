using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace SharpDb.EntityFrameworkCore.Entities;

/// <summary>
/// Abstract class that can be used as a base class for any entity.
/// </summary>
public abstract class Entity
{
    public event PropertyChangingEventHandler? PropertyChanging;
    public event PropertyChangedEventHandler? PropertyChanged;

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

    /// <summary>
    /// Updates the field and raises PropertyChanging and PropertyChanged events if the value has changed.
    /// </summary>
    /// <typeparam name="T">Property type</typeparam>
    /// <param name="property">Reference to the property</param>
    /// <param name="value">Value to be set</param>
    /// <param name="propertyName">Name of the property (if called in set method)</param>
    protected void SetProperty<T>(ref T property, T value, [CallerMemberName] string? propertyName = null)
    {
        if (!EqualityComparer<T>.Default.Equals(property, value))
        {
            PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
            property = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

/// <summary>
/// Abstract class that can be used as a base class for any entity with a strongly-typed primary key.
/// Whenever possible, prefer using this class over the non-generic <see cref="Entity"/>.
/// </summary>
/// <typeparam name="TKey">Type of key</typeparam>
public abstract class Entity<TKey> : Entity where TKey : struct, IEquatable<TKey>
{
    [Key]
    public virtual TKey Id { get; set; }
}
