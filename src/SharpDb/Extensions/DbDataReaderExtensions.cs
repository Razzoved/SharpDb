using System.Data.Common;
using System.Runtime.CompilerServices;

namespace SharpDb.Extensions;

public static class DbDataReaderExtensions
{
    /// <summary>
    /// Method for getting a value from DataReader, returns the value only if the
    /// column is present and can be converted to the given type. Errors out when
    /// the value is DBNULL and the <see cref="Nullable.GetUnderlyingType(Type)"/> is null.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="this"></param>
    /// <param name="fieldName"></param>
    /// <remarks>! Only direct conversion, non-negative or upcasting is allowed (ie. double can be converted to string, but not to float) !</remarks>
    /// <returns>Value of type T or exception</returns>
    /// <exception cref="ArgumentOutOfRangeException">When the column does not exist</exception>
    /// <exception cref="InvalidCastException">When value could not be resolved</exception>
    public static T GetValue<T>(this DbDataReader @this, string fieldName) where T : notnull
    {
        int index = @this.GetOrdinalNoThrow(fieldName);

        if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(fieldName), string.Format(Resources.Text_Error_DbDataReader_ColumnNotFound, fieldName));
        var desiredType = typeof(T);
        if (@this.IsDBNull(index))
        {
            if (Nullable.GetUnderlyingType(desiredType) is not null)
                return default!;
            throw new InvalidCastException(string.Format(Resources.Text_Error_DbDataReader_ColumnValueIsNull, fieldName, typeof(T).Name));
        }

        var type = @this.GetFieldType(index);
        if (desiredType == type || Nullable.GetUnderlyingType(desiredType) == type)
            return @this.GetFieldValue<T>(index);
        return Convert<T>(@this, index, fieldName, type);
    }

    /// <summary>
    /// Method for getting a value from DataReader, returns the value only if the column
    /// is present and can be converted to the given type. If the value is DBNULL, produces
    /// NULL.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="this"></param>
    /// <param name="fieldName"></param>
    /// <remarks>! Only direct conversion, non-negative or upcasting is allowed (ie. double can be converted to string, but not to float) !</remarks>
    /// <returns>Value of type T, null or exception</returns>
    /// <exception cref="ArgumentOutOfRangeException">When the column does not exist</exception>
    /// <exception cref="InvalidCastException">When value could not be resolved</exception>
    public static T? GetNullableValue<T>(this DbDataReader @this, string fieldName) where T : struct
    {
        int index = @this.GetOrdinalNoThrow(fieldName);

        if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(fieldName), string.Format(Resources.Text_Error_DbDataReader_ColumnNotFound, fieldName));
        if (@this.IsDBNull(index))
            return null;

        var desiredType = typeof(T);
        var type = @this.GetFieldType(index);
        if (desiredType == type || Nullable.GetUnderlyingType(desiredType) == type)
            return @this.GetFieldValue<T>(index);
        return Convert<T>(@this, index, fieldName, type);
    }

    /// <summary>
    /// Method for getting a string value from DataReader, returns the value only if the
    /// column is present and can be converted to string. Allows null.
    /// </summary>
    /// <param name="this"></param>
    /// <param name="fieldName"></param>
    /// <remarks>! Only direct conversion, non-negative or upcasting is allowed (ie. double can be converted to string, but not to float) !</remarks>
    /// <returns>String value or null</returns>
    /// <exception cref="ArgumentOutOfRangeException">When the column does not exist</exception>
    /// <exception cref="InvalidCastException">When value could not be resolved</exception>"
    public static string? GetNullableString(this DbDataReader @this, string fieldName)
    {
        int index = @this.GetOrdinalNoThrow(fieldName);

        if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(fieldName), string.Format(Resources.Text_Error_DbDataReader_ColumnNotFound, fieldName));
        if (@this.IsDBNull(index))
            return null;

        var type = @this.GetFieldType(index);
        if (type == typeof(string))
            return @this.GetString(index);
        return Convert<string>(@this, index, fieldName, type);
    }

    /// <summary>
    /// Method for getting a value from DataReader, if the column is not present or
    /// its value is NULL, returns the default value. Otherwise tries to convert it
    /// to the desired type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="this"></param>
    /// <param name="fieldName"></param>
    /// <param name="defaultValue"></param>
    /// <remarks>! Only direct conversion, non-negative or upcasting is allowed (ie. double can be converted to string, but not to float) !</remarks>
    /// <returns>Value of type T or fallback/default value</returns>
    /// <exception cref="InvalidCastException">When value could not be resolved</exception>"
    public static T GetValueOrDefault<T>(this DbDataReader @this, string fieldName, T defaultValue = default!)
    {
        int index = @this.GetOrdinalNoThrow(fieldName);

        if (index < 0)
            return defaultValue;
        if (@this.IsDBNull(index))
            return defaultValue;

        var type = @this.GetFieldType(index);
        if (typeof(T) == type)
            return @this.GetFieldValue<T>(index);
        return Convert<T>(@this, index, fieldName, type);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetOrdinalNoThrow(this DbDataReader @this, string fieldName)
    {
        for (int i = 0; i < @this.FieldCount; i++)
        {
            if (@this.GetName(i).Equals(fieldName, StringComparison.OrdinalIgnoreCase)) return i;
        }
        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T Convert<T>(DbDataReader reader, int index, string fieldName, Type fieldType)
    {
        try
        {
            T convertedValue = fieldType switch
            {
                Type t when t == typeof(string) => ConvertFromString<T>(reader.GetString(index)),
                Type t when t == typeof(decimal) => ConvertFromDecimal<T>(reader.GetDecimal(index)),
                Type t when t == typeof(double) => ConvertFromDouble<T>(reader.GetDouble(index)),
                Type t when t == typeof(float) => ConvertFromFloat<T>(reader.GetFloat(index)),
                Type t when t == typeof(long) => ConvertFromInt64<T>(reader.GetInt64(index)),
                Type t when t == typeof(int) => ConvertFromInt32<T>(reader.GetInt32(index)),
                Type t when t == typeof(short) => ConvertFromInt16<T>(reader.GetInt16(index)),
                Type t when t == typeof(byte) => ConvertFromInt8<T>(reader.GetByte(index)),
                Type t when t == typeof(bool) => ConvertFromBool<T>(reader.GetBoolean(index)),
                Type t when t == typeof(DateTime) => ConvertFromDateTime<T>(reader.GetDateTime(index)),
                Type t when t == typeof(DateTimeOffset) => ConvertFromDateTimeOffset<T>(reader.GetFieldValue<DateTimeOffset>(index)),
                Type t when t == typeof(Guid) => ConvertFromGuid<T>(reader.GetGuid(index)),
                _ => (T)reader.GetValue(index),
            };
            return convertedValue;
        }
        catch (ArgumentOutOfRangeException)
        {
            throw new InvalidCastException(string.Format(Resources.Text_Error_DbDataReader_ColumnValueOutOfRange, fieldName, fieldType.Name, typeof(T).Name));
        }
        catch
        {
            throw new InvalidCastException(string.Format(Resources.Text_Error_DbDataReader_ColumnValueNotConvertable, fieldName, fieldType.Name, typeof(T).Name));
        }
    }

    private static T ConvertFromString<T>(string value)
    {
        Type desiredType = Nullable.GetUnderlyingType(typeof(T)) is Type t ? t : typeof(T);

        if (desiredType == typeof(string))
            return ConvertNoBox<string, T>(value);

        if (desiredType == typeof(Guid))
            return ConvertNoBox<Guid, T>(Guid.Parse(value));
        if (desiredType == typeof(bool))
            return ConvertNoBox<bool, T>(bool.Parse(value) || value == "1");
        if (desiredType == typeof(char))
        {
            if (value.Length != 1)
                throw new ArgumentOutOfRangeException(nameof(value));
            return (T)(object)value[0];
        }
        // string to date
        if (desiredType == typeof(DateTime))
            return ConvertNoBox<DateTime, T>(DateTime.Parse(value));
        if (desiredType == typeof(DateTimeOffset))
            return ConvertNoBox<DateTimeOffset, T>(DateTimeOffset.Parse(value));
        if (desiredType == typeof(DateOnly))
            return ConvertNoBox<DateOnly, T>(DateOnly.Parse(value));
        if (desiredType == typeof(TimeOnly))
            return ConvertNoBox<TimeOnly, T>(TimeOnly.Parse(value));
        // string to number
        if (desiredType == typeof(decimal))
            return ConvertNoBox<decimal, T>(decimal.Parse(value));
        if (desiredType == typeof(double))
            return ConvertNoBox<double, T>(double.Parse(value));
        if (desiredType == typeof(float))
            return ConvertNoBox<float, T>(float.Parse(value));
        if (desiredType == typeof(long))
            return ConvertNoBox<long, T>(long.Parse(value));
        if (desiredType == typeof(int))
            return ConvertNoBox<int, T>(int.Parse(value));
        if (desiredType == typeof(short))
            return ConvertNoBox<short, T>(short.Parse(value));
        if (desiredType == typeof(byte))
            return ConvertNoBox<byte, T>(byte.Parse(value));
        if (desiredType == typeof(ulong))
            return ConvertNoBox<ulong, T>(ulong.Parse(value));
        if (desiredType == typeof(ushort))
            return ConvertNoBox<ushort, T>(ushort.Parse(value));
        if (desiredType == typeof(uint))
            return ConvertNoBox<uint, T>(uint.Parse(value));

        return (T)(object)value;
    }

    private static T ConvertFromDecimal<T>(decimal value)
    {
        Type desiredType = Nullable.GetUnderlyingType(typeof(T)) is Type t ? t : typeof(T);

        if (desiredType == typeof(decimal))
            return ConvertNoBox<decimal, T>(value);

        // decimal to other numerics
        if (desiredType == typeof(double))
            return ConvertNoBox<double, T>((double)value);
        if (desiredType == typeof(float))
            return ConvertNoBox<float, T>((float)value);
        // decimal to bool
        if (desiredType == typeof(bool))
            return ConvertNoBox<bool, T>(Math.Abs(value) > 0.0001M);
        // decimal to string
        if (desiredType == typeof(string))
            return ConvertNoBox<string, T>(value.ToString());

        return (T)(object)value;
    }

    private static T ConvertFromDouble<T>(double value)
    {
        Type desiredType = Nullable.GetUnderlyingType(typeof(T)) is Type t ? t : typeof(T);

        if (desiredType == typeof(double))
            return ConvertNoBox<double, T>(value);

        // double to other numerics
        if (desiredType == typeof(decimal))
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, (double)decimal.MinValue);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, (double)decimal.MaxValue);
            return ConvertNoBox<decimal, T>((decimal)value);
        }
        // double to bool
        if (desiredType == typeof(bool))
            return ConvertNoBox<bool, T>(Math.Abs(value) > 0.001);
        // double to string
        if (desiredType == typeof(string))
            return ConvertNoBox<string, T>(value.ToString());

        return (T)(object)value;
    }

    private static T ConvertFromFloat<T>(float value)
    {
        Type desiredType = Nullable.GetUnderlyingType(typeof(T)) is Type t ? t : typeof(T);

        if (desiredType == typeof(float))
            return ConvertNoBox<float, T>(value);

        // float to other numerics
        if (desiredType == typeof(decimal))
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, (double)decimal.MinValue);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, (double)decimal.MaxValue);
            return ConvertNoBox<decimal, T>((decimal)value);
        }
        // float to bool
        if (desiredType == typeof(bool))
            return ConvertNoBox<bool, T>(Math.Abs(value) > 0.001);
        // float to string
        if (desiredType == typeof(string))
            return ConvertNoBox<string, T>(value.ToString());

        return (T)(object)value;
    }

    private static T ConvertFromInt64<T>(long value)
    {
        Type desiredType = Nullable.GetUnderlyingType(typeof(T)) is Type t ? t : typeof(T);

        if (desiredType == typeof(long))
            return ConvertNoBox<long, T>(value);

        // long to other numerics
        if (desiredType == typeof(decimal))
            return ConvertNoBox<decimal, T>(value);
        if (desiredType == typeof(ulong))
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            return ConvertNoBox<ulong, T>((ulong)value);
        }
        // long to bool
        if (desiredType == typeof(bool))
            return ConvertNoBox<bool, T>(value != 0);
        // long to string
        if (desiredType == typeof(string))
            return ConvertNoBox<string, T>(value.ToString());

        return (T)(object)value;
    }

    private static T ConvertFromInt32<T>(int value)
    {
        Type desiredType = Nullable.GetUnderlyingType(typeof(T)) is Type t ? t : typeof(T);

        if (desiredType == typeof(int))
            return ConvertNoBox<int, T>(value);

        // int to other numerics
        if (desiredType == typeof(decimal))
            return ConvertNoBox<decimal, T>(value);
        if (desiredType == typeof(double))
            return ConvertNoBox<double, T>(value);
        if (desiredType == typeof(long))
            return ConvertNoBox<long, T>(value);
        if (desiredType == typeof(ulong))
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            return ConvertNoBox<ulong, T>((ulong)value);
        }
        if (desiredType == typeof(uint))
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            return ConvertNoBox<uint, T>((uint)value);
        }
        // int to bool
        if (desiredType == typeof(bool))
            return ConvertNoBox<bool, T>(value != 0);
        // int to string
        if (desiredType == typeof(string))
            return ConvertNoBox<string, T>(value.ToString());

        return (T)(object)value;
    }

    private static T ConvertFromInt16<T>(short value)
    {
        Type desiredType = Nullable.GetUnderlyingType(typeof(T)) is Type t ? t : typeof(T);

        if (desiredType == typeof(short))
            return ConvertNoBox<short, T>(value);

        // byte to other numerics
        if (desiredType == typeof(decimal))
            return ConvertNoBox<decimal, T>(value);
        if (desiredType == typeof(double))
            return ConvertNoBox<double, T>(value);
        if (desiredType == typeof(float))
            return ConvertNoBox<float, T>(value);
        if (desiredType == typeof(long))
            return ConvertNoBox<long, T>(value);
        if (desiredType == typeof(ulong))
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            return ConvertNoBox<ulong, T>((ulong)value);
        }
        if (desiredType == typeof(uint))
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            return ConvertNoBox<uint, T>((uint)value);
        }
        if (desiredType == typeof(ushort))
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            return ConvertNoBox<ushort, T>((ushort)value);
        }
        // byte to bool
        if (desiredType == typeof(bool))
            return ConvertNoBox<bool, T>(value != 0);
        // byte to string
        if (desiredType == typeof(string))
            return ConvertNoBox<string, T>(value.ToString());

        return (T)(object)value;
    }

    private static T ConvertFromInt8<T>(byte value)
    {
        Type desiredType = Nullable.GetUnderlyingType(typeof(T)) is Type t ? t : typeof(T);

        if (desiredType == typeof(byte))
            return ConvertNoBox<byte, T>(value);

        // byte to other numerics
        if (desiredType == typeof(decimal))
            return ConvertNoBox<decimal, T>(value);
        if (desiredType == typeof(double))
            return ConvertNoBox<double, T>(value);
        if (desiredType == typeof(float))
            return ConvertNoBox<float, T>(value);
        if (desiredType == typeof(long))
            return ConvertNoBox<long, T>(value);
        if (desiredType == typeof(short))
            return ConvertNoBox<short, T>(value);
        if (desiredType == typeof(ulong))
            return ConvertNoBox<ulong, T>(value);
        if (desiredType == typeof(uint))
            return ConvertNoBox<uint, T>(value);
        if (desiredType == typeof(ushort))
            return ConvertNoBox<ushort, T>(value);
        // byte to bool
        if (desiredType == typeof(bool))
            return ConvertNoBox<bool, T>(value != 0);
        // byte to string
        if (desiredType == typeof(string))
            return ConvertNoBox<string, T>(value.ToString());

        return (T)(object)value;
    }

    private static T ConvertFromBool<T>(bool value)
    {
        Type desiredType = Nullable.GetUnderlyingType(typeof(T)) is Type t ? t : typeof(T);

        // bool to other numerics
        if (desiredType == typeof(decimal))
            return ConvertNoBox<decimal, T>(value ? 1M : 0M);
        if (desiredType == typeof(double))
            return ConvertNoBox<double, T>(value ? 1.0 : 0.0);
        if (desiredType == typeof(float))
            return ConvertNoBox<float, T>(value ? 1.0f : 0.0f);
        if (desiredType == typeof(long))
            return ConvertNoBox<long, T>(value ? 1L : 0L);
        if (desiredType == typeof(int))
            return ConvertNoBox<int, T>(value ? 1 : 0);
        if (desiredType == typeof(short))
            return ConvertNoBox<short, T>(value ? (short)1 : (short)0);
        if (desiredType == typeof(byte))
            return ConvertNoBox<byte, T>(value ? (byte)1 : (byte)0);
        if (desiredType == typeof(ulong))
            return ConvertNoBox<ulong, T>(value ? 1u : 0u);
        if (desiredType == typeof(uint))
            return ConvertNoBox<uint, T>(value ? 1u : 0u);
        if (desiredType == typeof(ushort))
            return ConvertNoBox<ushort, T>(value ? (ushort)1 : (ushort)0);
        // bool to string
        if (desiredType == typeof(string))
            return ConvertNoBox<string, T>(value ? System.Convert.ToString(true) : System.Convert.ToString(false));

        return (T)(object)value;
    }

    private static T ConvertFromDateTime<T>(DateTime value)
    {
        Type desiredType = Nullable.GetUnderlyingType(typeof(T)) is Type t ? t : typeof(T);

        if (desiredType == typeof(DateTime))
            return ConvertNoBox<DateTime, T>(value);
        if (desiredType == typeof(DateTimeOffset))
            return ConvertNoBox<DateTimeOffset, T>(new DateTimeOffset(value));
        if (desiredType == typeof(DateOnly))
            return ConvertNoBox<DateOnly, T>(DateOnly.FromDateTime(value));
        if (desiredType == typeof(TimeOnly))
            return ConvertNoBox<TimeOnly, T>(TimeOnly.FromDateTime(value));

        return (T)(object)value;
    }

    private static T ConvertFromDateTimeOffset<T>(DateTimeOffset value)
    {
        Type desiredType = Nullable.GetUnderlyingType(typeof(T)) is Type t ? t : typeof(T);

        if (desiredType == typeof(DateTimeOffset))
            return ConvertNoBox<DateTimeOffset, T>(value);
        if (desiredType == typeof(DateTime))
            return ConvertNoBox<DateTime, T>(value.DateTime);
        if (desiredType == typeof(DateOnly))
            return ConvertNoBox<DateOnly, T>(DateOnly.FromDateTime(value.DateTime));
        if (desiredType == typeof(TimeOnly))
            return ConvertNoBox<TimeOnly, T>(TimeOnly.FromDateTime(value.DateTime));

        return (T)(object)value;
    }

    private static T ConvertFromGuid<T>(Guid value)
    {
        Type desiredType = Nullable.GetUnderlyingType(typeof(T)) is Type t ? t : typeof(T);

        if (desiredType == typeof(Guid))
            return ConvertNoBox<Guid, T>(value);
        if (desiredType == typeof(string))
            return ConvertNoBox<string, T>(value.ToString());

        return (T)(object)value;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static TTo ConvertNoBox<TFrom, TTo>(TFrom value)
    {
        if (typeof(TTo) == typeof(TFrom))
            return Unsafe.As<TFrom, TTo>(ref value);
        if (value is null)
            return default!;
        return (TTo)(object)value;
    }
}
