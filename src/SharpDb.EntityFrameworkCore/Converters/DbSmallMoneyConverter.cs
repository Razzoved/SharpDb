using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SharpDb.Entities.DataTypes;

namespace SharpDb.EntityFrameworkCore.Converters;

public sealed class DbSmallMoneyConverter() : ValueConverter<DbSmallMoney, decimal>(
    v => v.Value,
    v => new DbSmallMoney(v),
    new ConverterMappingHints(precision: 10, scale: 4));

public sealed class NullableDbSmallMoneyConverter() : ValueConverter<DbSmallMoney?, decimal?>(
    v => v.HasValue ? v.Value.Value : null,
    v => v.HasValue ? new DbSmallMoney(v.Value) : null,
    new ConverterMappingHints(precision: 10, scale: 4));
