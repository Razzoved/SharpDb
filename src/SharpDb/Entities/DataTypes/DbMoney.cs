namespace SharpDb.Entities.DataTypes;

/// <summary>
/// This class represents a money data type similar to MSSQL.
/// </summary>
public readonly struct DbMoney : IComparable, IComparable<DbMoney>, IEquatable<DbMoney>, IComparable<decimal>, IEquatable<decimal>
{
    public readonly decimal Value;

    public DbMoney(decimal value) => Value = value;

    public static implicit operator DbMoney(in long value) => new(value);
    public static implicit operator DbMoney(in double value) => new((decimal)value);
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
    public int CompareTo(object? obj) => Value.CompareTo(obj);
    public bool Equals(DbMoney other) => Equals(other.Value);
    public bool Equals(decimal other) => Value.Equals(other);
    public override bool Equals(object? obj) => Value.Equals(obj);
    public override int GetHashCode() => Value.GetHashCode();
}
