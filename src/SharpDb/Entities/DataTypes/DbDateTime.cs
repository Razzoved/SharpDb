namespace SharpDb.Entities.DataTypes;

/// <summary>
/// This type represents a datetime data type whose range corresponds to MSSQL.
/// </summary>
public readonly struct DbDateTime : IComparable, IComparable<DbDateTime>, IEquatable<DbDateTime>, IComparable<DateTimeOffset>, IEquatable<DateTimeOffset>
{
    public readonly DateTimeOffset Value;

    public DbDateTime(DateTimeOffset value) => Value = Clamp(value);
    public DbDateTime(DateTime value) => Value = Clamp(new(value));

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
    public int CompareTo(object? obj) => ((IComparable)Value).CompareTo(obj);
    public bool Equals(DbDateTime other) => Equals(other.Value);
    public bool Equals(DateTimeOffset other) => Value.Equals(other);
    public override bool Equals(object? obj) => Value.Equals(obj);
    public override int GetHashCode() => Value.GetHashCode();

    private static DateTimeOffset Clamp(DateTimeOffset value) => value switch
    {
        { Year: < 1753 } => new DateTimeOffset(1753, 1, 1, 0, 0, 0, value.Offset),
        { Year: > 9999 } => new DateTimeOffset(9999, 12, 31, 23, 59, 59, value.Offset),
        { Year: 9999, Month: 12, Day: > 31 } => new DateTimeOffset(9999, 12, 31, 23, 59, 59, value.Offset),
        { Year: 9999, Month: 12, Day: 31, Hour: 23, Minute: > 59 } => new DateTimeOffset(9999, 12, 31, 23, 59, 59, value.Offset),
        _ => value,
    };
}
