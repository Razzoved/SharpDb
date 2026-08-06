using System.Globalization;

namespace SharpDb.Entities.DataTypes;

/// <summary>
/// This type represents a <b>datetime</b> data type whose range corresponds to MSSQL.
/// </summary>
public readonly struct DbDateTime :
    IComparable,
    IComparable<DbDateTime>, IComparable<DateTimeOffset>,
    IEquatable<DbDateTime>, IEquatable<DateTimeOffset>
{
    public static readonly DateTimeOffset MinValue = new(1753, 1, 1, 0, 0, 0, TimeSpan.Zero);
    public static readonly DateTimeOffset MaxValue = new(9999, 12, 31, 23, 59, 59, TimeSpan.Zero);

    public readonly DateTimeOffset Value;

    public DbDateTime(DateTimeOffset value) => Value = Clamp(value);
    public DbDateTime(DateTime value) => Value = Clamp(new DateTimeOffset(value));

    public static implicit operator DbDateTime(in DateTimeOffset dateTimeOffset) => new(dateTimeOffset.DateTime);
    public static implicit operator DbDateTime(in DateTime dateTime) => new(dateTime);
    public static implicit operator DateTimeOffset(in DbDateTime dbDateTime) => dbDateTime.Value;
    public static implicit operator DateTime(in DbDateTime dbDateTime) => dbDateTime.Value.DateTime;

    public static bool operator ==(DbDateTime left, DbDateTime right) => left.Equals(right);
    public static bool operator !=(DbDateTime left, DbDateTime right) => !(left == right);
    public static bool operator <(DbDateTime left, DbDateTime right) => left.CompareTo(right) < 0;
    public static bool operator <=(DbDateTime left, DbDateTime right) => left.CompareTo(right) <= 0;
    public static bool operator >(DbDateTime left, DbDateTime right) => left.CompareTo(right) > 0;
    public static bool operator >=(DbDateTime left, DbDateTime right) => left.CompareTo(right) >= 0;

    public int CompareTo(DbDateTime other) => CompareTo(other.Value);
    public int CompareTo(DateTimeOffset other) => Value.CompareTo(other);
    public int CompareTo(object? obj) => obj switch
    {
        DbDateTime other => CompareTo(other),
        DateTimeOffset otherDateTimeOffset => CompareTo(otherDateTimeOffset),
        _ => throw new ArgumentException(string.Format(Resources.Text_Error_Comparison_IncompatibleTypes, nameof(DbDateTime), obj?.GetType().Name ?? "null"), nameof(obj))
    };
    public bool Equals(DbDateTime other) => Equals(other.Value);
    public bool Equals(DateTimeOffset other) => Value.Equals(other);
    public override bool Equals(object? obj) => obj switch
    {
        DbDateTime other => Equals(other),
        DateTimeOffset otherDateTimeOffset => Equals(otherDateTimeOffset),
        _ => false
    };
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    private static DateTimeOffset Clamp(DateTimeOffset value) => value switch
    {
        { Year: < 1753 } => new DateTimeOffset(MinValue.UtcDateTime, value.Offset),
        { Year: > 9999 } => new DateTimeOffset(MaxValue.UtcDateTime, value.Offset),
        { Year: 9999, Month: 12, Day: > 31 } => new DateTimeOffset(MaxValue.UtcDateTime, value.Offset),
        { Year: 9999, Month: 12, Day: 31, Hour: 23, Minute: > 59 } => new DateTimeOffset(MaxValue.UtcDateTime, value.Offset),
        _ => value,
    };
}
