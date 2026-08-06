using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SharpDb.Entities.DataTypes;

namespace SharpDb.EntityFrameworkCore.Converters;

public sealed class DbMoneyConverter() : ValueConverter<DbMoney, decimal>(
    v => v.Value,
    v => new DbMoney(v),
    new ConverterMappingHints(precision: 19, scale: 4));

public sealed class NullableDbMoneyConverter() : ValueConverter<DbMoney?, decimal?>(
    v => v.HasValue ? v.Value.Value : null,
    v => v.HasValue ? new DbMoney(v.Value) : null,
    new ConverterMappingHints(precision: 19, scale: 4));
