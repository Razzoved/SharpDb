using System.Globalization;

namespace SharpDb.Entities.DataTypes;

/// <summary>
/// This class represents a <b>money</b> data type similar to MSSQL.
/// </summary>
public readonly struct DbMoney :
    IComparable,
    IComparable<DbMoney>, IComparable<decimal>,
    IEquatable<DbMoney>, IEquatable<decimal>
{
    public readonly decimal Value;

    public DbMoney(long value) => Value = value;
    public DbMoney(double value) => Value = (decimal)value;
    public DbMoney(decimal value) => Value = value;

    public static implicit operator DbMoney(in long value) => new(value);
    public static implicit operator DbMoney(in double value) => new(value);
    public static implicit operator DbMoney(in decimal value) => new(value);
    public static implicit operator decimal(in DbMoney money) => money.Value;

    public static bool operator ==(DbMoney left, DbMoney right) => left.Equals(right);
    public static bool operator !=(DbMoney left, DbMoney right) => !(left == right);
    public static bool operator <(DbMoney left, DbMoney right) => left.CompareTo(right) < 0;
    public static bool operator <=(DbMoney left, DbMoney right) => left.CompareTo(right) <= 0;
    public static bool operator >(DbMoney left, DbMoney right) => left.CompareTo(right) > 0;
    public static bool operator >=(DbMoney left, DbMoney right) => left.CompareTo(right) >= 0;

    public int CompareTo(DbMoney other) => CompareTo(other.Value);
    public int CompareTo(decimal other) => Value.CompareTo(other);
    public int CompareTo(object? obj) => obj switch
    {
        DbMoney other => CompareTo(other),
        decimal otherDecimal => CompareTo(otherDecimal),
        _ => throw new ArgumentException(string.Format(Resources.Text_Error_Comparison_IncompatibleTypes, nameof(DbMoney), obj?.GetType().Name ?? "null"), nameof(obj))
    };
    public bool Equals(DbMoney other) => Equals(other.Value);
    public bool Equals(decimal other) => Value.Equals(other);
    public override bool Equals(object? obj) => obj switch
    {
        DbMoney other => Equals(other),
        decimal otherDecimal => Equals(otherDecimal),
        _ => false
    };
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
