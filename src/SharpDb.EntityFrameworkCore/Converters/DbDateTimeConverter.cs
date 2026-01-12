using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SharpDb.Entities.DataTypes;

namespace SharpDb.EntityFrameworkCore.Converters;

public sealed class DbDateTimeConverter : ValueConverter<DbDateTime, DateTime>
{
    public DbDateTimeConverter() : base(
        v => v.Value.DateTime,
        v => new(v))
    { }
}

public sealed class DbDateTimeToOffsetConverter : ValueConverter<DbDateTime, DateTimeOffset>
{
    public DbDateTimeToOffsetConverter() : base(
        v => v.Value,
        v => new(v))
    { }
}

public sealed class NullableDbDateTimeConverter : ValueConverter<DbDateTime?, DateTime?>
{
    public NullableDbDateTimeConverter() : base(
        v => v.HasValue ? v.Value.Value.DateTime : null,
        v => v.HasValue ? new(v.Value) : null)
    { }
}

public sealed class NullableDbDateTimeToOffsetConverter : ValueConverter<DbDateTime?, DateTimeOffset?>
{
    public NullableDbDateTimeToOffsetConverter() : base(
        v => v.HasValue ? v.Value.Value : null,
        v => v.HasValue ? new(v.Value) : null)
    { }
}
