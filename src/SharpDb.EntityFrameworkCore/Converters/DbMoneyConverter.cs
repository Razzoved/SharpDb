using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SharpDb.Entities.DataTypes;

namespace SharpDb.EntityFrameworkCore.Converters;

public sealed class DbMoneyConverter : ValueConverter<DbMoney, decimal>
{
    public DbMoneyConverter() : base(
        v => v.Value,
        v => new(v),
        new ConverterMappingHints(precision: 19, scale: 4))
    { }
}

public sealed class NullableDbMoneyConverter : ValueConverter<DbMoney?, decimal?>
{
    public NullableDbMoneyConverter() : base(
        v => v.HasValue ? v.Value.Value : null,
        v => v.HasValue ? new(v.Value) : null,
        new ConverterMappingHints(precision: 19, scale: 4))
    { }
}
