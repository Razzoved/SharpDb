using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SharpDb.Entities.DataTypes;

namespace SharpDb.EntityFrameworkCore.Converters;

public sealed class DbSmallDateTimeConverter : ValueConverter<DbSmallDateTime, DateTime>
{
    public DbSmallDateTimeConverter() : base(
        v => v.Value.DateTime,
        v => new(v))
    { }
}

public sealed class DbSmallDateTimeToOffsetConverter : ValueConverter<DbSmallDateTime, DateTimeOffset>
{
    public DbSmallDateTimeToOffsetConverter() : base(
        v => v.Value,
        v => new(v))
    { }
}

public sealed class NullableDbSmallDateTimeConverter : ValueConverter<DbSmallDateTime?, DateTime?>
{
    public NullableDbSmallDateTimeConverter() : base(
        v => v.HasValue ? v.Value.Value.DateTime : null,
        v => v.HasValue ? new(v.Value) : null)
    { }
}

public sealed class NullableDbSmallDateTimeToOffsetConverter : ValueConverter<DbSmallDateTime?, DateTimeOffset?>
{
    public NullableDbSmallDateTimeToOffsetConverter() : base(
        v => v.HasValue ? v.Value.Value : null,
        v => v.HasValue ? new(v.Value) : null)
    { }
}
