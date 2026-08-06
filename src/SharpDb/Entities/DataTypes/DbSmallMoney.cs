using System.Globalization;

namespace SharpDb.Entities.DataTypes;

/// <summary>
/// This class represents a <b>smallmoney</b> data type whose range corresponds to MSSQL.
/// </summary>
public readonly struct DbSmallMoney :
    IComparable,
    IComparable<DbSmallMoney>, IComparable<decimal>,
    IEquatable<DbSmallMoney>, IEquatable<decimal>
{
    public const decimal MinValue = -214748.3648m;
    public const decimal MaxValue = 214748.3647m;

    public readonly decimal Value;

    public DbSmallMoney(long value) : this((decimal)value) { }
    public DbSmallMoney(double value) : this((decimal)value) { }
    public DbSmallMoney(decimal value)
    {
        if (value is < MinValue or > MaxValue)
            throw new ArgumentOutOfRangeException(nameof(value), string.Format(Resources.Text_Error_DataType_ValueOutOfRange, MinValue, MaxValue, nameof(DbSmallMoney)));
        Value = value;
    }

    public static implicit operator DbSmallMoney(in long value) => new(value);
    public static implicit operator DbSmallMoney(in double value) => new(value);
    public static implicit operator DbSmallMoney(in decimal value) => new(value);
    public static implicit operator decimal(in DbSmallMoney money) => money.Value;

    public static bool operator ==(DbSmallMoney left, DbSmallMoney right) => left.Equals(right);
    public static bool operator !=(DbSmallMoney left, DbSmallMoney right) => !(left == right);
    public static bool operator <(DbSmallMoney left, DbSmallMoney right) => left.CompareTo(right) < 0;
    public static bool operator <=(DbSmallMoney left, DbSmallMoney right) => left.CompareTo(right) <= 0;
    public static bool operator >(DbSmallMoney left, DbSmallMoney right) => left.CompareTo(right) > 0;
    public static bool operator >=(DbSmallMoney left, DbSmallMoney right) => left.CompareTo(right) >= 0;

    public int CompareTo(DbSmallMoney other) => CompareTo(other.Value);
    public int CompareTo(decimal other) => Value.CompareTo(other);
    public int CompareTo(object? obj) => obj switch
    {
        DbSmallMoney other => CompareTo(other),
        decimal dec => CompareTo(dec),
        _ => throw new ArgumentException(string.Format(Resources.Text_Error_Comparison_IncompatibleTypes, nameof(DbSmallMoney), obj?.GetType().Name ?? "null"), nameof(obj))
    };
    public bool Equals(DbSmallMoney other) => Equals(other.Value);
    public bool Equals(decimal other) => Value.Equals(other);
    public override bool Equals(object? obj) => obj switch
    {
        DbSmallMoney other => Equals(other),
        decimal dec => Equals(dec),
        _ => false
    };
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
