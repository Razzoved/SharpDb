using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SharpDb.Entities.DataTypes;

namespace SharpDb.EntityFrameworkCore.Converters;

public sealed class DbSmallDateTimeConverter() : ValueConverter<DbSmallDateTime, DateTime>(
    v => v.Value.DateTime,
    v => new DbSmallDateTime(v));

public sealed class DbSmallDateTimeToOffsetConverter() : ValueConverter<DbSmallDateTime, DateTimeOffset>(
    v => v.Value,
    v => new DbSmallDateTime(v));

public sealed class NullableDbSmallDateTimeConverter() : ValueConverter<DbSmallDateTime?, DateTime?>(
    v => v.HasValue ? v.Value.Value.DateTime : null,
    v => v.HasValue ? new DbSmallDateTime(v.Value) : null);

public sealed class NullableDbSmallDateTimeToOffsetConverter() : ValueConverter<DbSmallDateTime?, DateTimeOffset?>(
    v => v.HasValue ? v.Value.Value : null,
    v => v.HasValue ? new DbSmallDateTime(v.Value) : null);
