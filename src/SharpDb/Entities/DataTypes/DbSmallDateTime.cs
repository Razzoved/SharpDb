using System.Globalization;

namespace SharpDb.Entities.DataTypes;

/// <summary>
/// This class represents a <b>smalldatetime</b> data type whose range corresponds to MSSQL.
/// </summary>
public readonly struct DbSmallDateTime :
    IComparable,
    IComparable<DbSmallDateTime>, IComparable<DateTimeOffset>,
    IEquatable<DbSmallDateTime>, IEquatable<DateTimeOffset>
{
    public static readonly DateTimeOffset MinValue = new(1900, 1, 1, 0, 0, 0, TimeSpan.Zero);
    public static readonly DateTimeOffset MaxValue = new(2079, 06, 06, 23, 59, 59, TimeSpan.Zero);

    public readonly DateTimeOffset Value;

    public DbSmallDateTime(DateTimeOffset value) => Value = Clamp(value);
    public DbSmallDateTime(DateTime value) => Value = Clamp(new DateTimeOffset(value));

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
    public int CompareTo(object? obj) => obj switch
    {
        DbSmallDateTime other => CompareTo(other),
        DateTimeOffset dateTimeOffset => CompareTo(dateTimeOffset),
        _ => throw new ArgumentException(string.Format(Resources.Text_Error_Comparison_IncompatibleTypes, nameof(DbSmallDateTime), obj?.GetType().Name ?? "null"), nameof(obj))
    };
    public bool Equals(DbSmallDateTime other) => Equals(other.Value);
    public bool Equals(DateTimeOffset other) => Value.Equals(other);
    public override bool Equals(object? obj) => obj switch
    {
        DbSmallDateTime other => Equals(other),
        DateTimeOffset dateTimeOffset => Equals(dateTimeOffset),
        _ => false
    };
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    private static DateTimeOffset Clamp(DateTimeOffset value) => value switch
    {
        { Year: < 1900 } => new DateTimeOffset(MinValue.UtcDateTime, value.Offset),
        { Year: > 2079 } => new DateTimeOffset(MaxValue.UtcDateTime, value.Offset),
        { Year: 2079, Month: > 6 } => new DateTimeOffset(MaxValue.UtcDateTime, value.Offset),
        { Year: 2079, Month: 6, Day: > 6 } => new DateTimeOffset(MaxValue.UtcDateTime, value.Offset),
        _ => value,
    };
}
