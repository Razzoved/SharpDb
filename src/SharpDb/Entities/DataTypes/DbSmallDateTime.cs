namespace SharpDb.Entities.DataTypes;

/// <summary>
/// This class represents a smalldatetime data type whose range corresponds to MSSQL.
/// </summary>
public readonly struct DbSmallDateTime : IComparable, IComparable<DbSmallDateTime>, IEquatable<DbSmallDateTime>, IComparable<DateTimeOffset>, IEquatable<DateTimeOffset>
{
    public readonly DateTimeOffset Value;

    public DbSmallDateTime(DateTimeOffset value) => Value = Clamp(value);
    public DbSmallDateTime(DateTime value) => Value = Clamp(new(value));

    public static implicit operator DbSmallDateTime(in DateTimeOffset dateTimeOffset) => new(dateTimeOffset.DateTime);
    public static implicit operator DbSmallDateTime(in DateTime dateTime) => new(dateTime);
    public static implicit operator DateTimeOffset(in DbSmallDateTime dbDateTime) => dbDateTime.Value;
    public static implicit operator DateTime(in DbSmallDateTime dbDateTime) => dbDateTime.Value.DateTime;

    public static bool operator ==(DbSmallDateTime left, DbSmallDateTime right) => left.Equals(right);
    public static bool operator !=(DbSmallDateTime left, DbSmallDateTime right) => !(left == right);
    public static bool operator <(DbSmallDateTime left, DbSmallDateTime right) => left.CompareTo(right) < 0;
    public static bool operator <=(DbSmallDateTime left, DbSmallDateTime right) => left.CompareTo(right) <= 0;
    public static bool operator >(DbSmallDateTime left, DbSmallDateTime right) => left.CompareTo(right) > 0;
    public static bool operator >=(DbSmallDateTime left, DbSmallDateTime right) => left.CompareTo(right) >= 0;

    public int CompareTo(DbSmallDateTime other) => CompareTo(other.Value);
    public int CompareTo(DateTimeOffset other) => Value.CompareTo(other);
    public int CompareTo(object? obj) => ((IComparable)Value).CompareTo(obj);
    public bool Equals(DbSmallDateTime other) => Equals(other.Value);
    public bool Equals(DateTimeOffset other) => Value.Equals(other);
    public override bool Equals(object? obj) => Value.Equals(obj);
    public override int GetHashCode() => Value.GetHashCode();

    private static DateTimeOffset Clamp(DateTimeOffset value) => value switch
    {
        { Year: < 1900 } => new DateTimeOffset(1900, 1, 1, 0, 0, 0, value.Offset),
        { Year: > 2079 } => new DateTimeOffset(2079, 06, 06, 23, 59, 59, value.Offset),
        { Year: 2079, Month: > 6 } => new DateTimeOffset(2079, 06, 06, 23, 59, 59, value.Offset),
        { Year: 2079, Month: 6, Day: > 6 } => new DateTimeOffset(2079, 06, 06, 23, 59, 59, value.Offset),
        _ => value,
    };
}
