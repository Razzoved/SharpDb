using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SharpDb.Entities.DataTypes;

namespace SharpDb.EntityFrameworkCore.Converters;

public sealed class DbDateTimeConverter() : ValueConverter<DbDateTime, DateTime>(
    v => v.Value.DateTime,
    v => new DbDateTime(v));

public sealed class DbDateTimeToOffsetConverter() : ValueConverter<DbDateTime, DateTimeOffset>(
    v => v.Value,
    v => new DbDateTime(v));

public sealed class NullableDbDateTimeConverter() : ValueConverter<DbDateTime?, DateTime?>(
    v => v.HasValue ? v.Value.Value.DateTime : null,
    v => v.HasValue ? new DbDateTime(v.Value) : null);

public sealed class NullableDbDateTimeToOffsetConverter() : ValueConverter<DbDateTime?, DateTimeOffset?>(
    v => v.HasValue ? v.Value.Value : null,
    v => v.HasValue ? new DbDateTime(v.Value) : null);
