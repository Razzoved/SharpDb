namespace SharpDb.Entities.DataTypes;

/// <summary>
/// This class represents a smallmoney data type whose range corresponds to MSSQL.
/// </summary>
public readonly struct DbSmallMoney : IComparable, IComparable<DbSmallMoney>, IEquatable<DbSmallMoney>, IComparable<decimal>, IEquatable<decimal>
{
    private const decimal MinValue = -214748.3648m;
    private const decimal MaxValue = 214748.3647m;

    public readonly decimal Value;

    public DbSmallMoney(decimal value)
    {
        if (value < MinValue || value > MaxValue)
            throw new ArgumentOutOfRangeException(nameof(value), string.Format(Resources.Text_Error_DataType_ValueOutOfRange, MinValue, MaxValue, nameof(DbSmallMoney)));
        Value = value;
    }

    public static implicit operator DbSmallMoney(in long value) => new(value);
    public static implicit operator DbSmallMoney(in double value) => new((decimal)value);
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
    public int CompareTo(object? obj) => Value.CompareTo(obj);
    public bool Equals(DbSmallMoney other) => Equals(other.Value);
    public bool Equals(decimal other) => Value.Equals(other);
    public override bool Equals(object? obj) => Value.Equals(obj);
    public override int GetHashCode() => Value.GetHashCode();
}
