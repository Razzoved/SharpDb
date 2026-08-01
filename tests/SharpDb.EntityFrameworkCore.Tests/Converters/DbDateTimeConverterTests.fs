namespace SharpDb.EntityFrameworkCore.Tests.Converters

open System
open Microsoft.EntityFrameworkCore.Storage.ValueConversion
open SharpDb.Entities.DataTypes
open SharpDb.EntityFrameworkCore.Converters
open Xunit

module DbDateTimeConverterTests =

    [<Fact>]
    let ``DbDateTimeConverter converts to DateTime`` () =
        let converter = DbDateTimeConverter()
        let dbDateTime = DbDateTime(DateTimeOffset(2020, 6, 15, 10, 30, 0, TimeSpan.Zero))
        let result = converter.ConvertToProviderTyped.Invoke(dbDateTime)
        Assert.Equal(DateTime(2020, 6, 15, 10, 30, 0), result)

    [<Fact>]
    let ``DbDateTimeConverter converts from DateTime`` () =
        let converter = DbDateTimeConverter()
        let dateTime = DateTime.SpecifyKind(DateTime(2020, 6, 15, 10, 30, 0), DateTimeKind.Utc)
        let result = converter.ConvertFromProviderTyped.Invoke(dateTime)
        Assert.Equal(DateTimeOffset(2020, 6, 15, 10, 30, 0, TimeSpan.Zero), result.Value)

    [<Fact>]
    let ``DbDateTimeToOffsetConverter converts to DateTimeOffset`` () =
        let converter = DbDateTimeToOffsetConverter()
        let dbDateTime = DbDateTime(DateTimeOffset(2020, 6, 15, 10, 30, 0, TimeSpan.Zero))
        let result = converter.ConvertToProviderTyped.Invoke(dbDateTime)
        Assert.Equal(DateTimeOffset(2020, 6, 15, 10, 30, 0, TimeSpan.Zero), result)

    [<Fact>]
    let ``DbDateTimeToOffsetConverter converts from DateTimeOffset`` () =
        let converter = DbDateTimeToOffsetConverter()
        let dateTimeOffset = DateTimeOffset(2020, 6, 15, 10, 30, 0, TimeSpan.Zero)
        let result = converter.ConvertFromProviderTyped.Invoke(dateTimeOffset)
        Assert.Equal(DateTimeOffset(2020, 6, 15, 10, 30, 0, TimeSpan.Zero), result.Value)

    [<Fact>]
    let ``NullableDbDateTimeConverter converts null to null`` () =
        let converter = NullableDbDateTimeConverter()
        let value = Nullable<DbDateTime>()
        let result = converter.ConvertToProviderTyped.Invoke(value)
        Assert.Null(result)

    [<Fact>]
    let ``NullableDbDateTimeConverter converts value to DateTime`` () =
        let converter = NullableDbDateTimeConverter()
        let dbDateTime = DbDateTime(DateTimeOffset(2020, 6, 15, 10, 30, 0, TimeSpan.Zero)) |> Nullable
        let result = converter.ConvertToProviderTyped.Invoke(dbDateTime)
        Assert.Equal<Nullable<DateTime>>(DateTime(2020, 6, 15, 10, 30, 0), result)

    [<Fact>]
    let ``NullableDbDateTimeConverter converts null from DateTime`` () =
        let converter = NullableDbDateTimeConverter()
        let value = Nullable<DateTime>()
        let result = converter.ConvertFromProviderTyped.Invoke(value)
        Assert.Null(result)

    [<Fact>]
    let ``NullableDbDateTimeConverter converts value from DateTime`` () =
        let converter = NullableDbDateTimeConverter()
        let dateTime = DateTime.SpecifyKind(DateTime(2020, 6, 15, 10, 30, 0), DateTimeKind.Utc) |> Nullable
        let result = converter.ConvertFromProviderTyped.Invoke(dateTime)
        Assert.True(result.HasValue)
        Assert.Equal(DateTimeOffset(2020, 6, 15, 10, 30, 0, TimeSpan.Zero), result.Value.Value)

    [<Fact>]
    let ``NullableDbDateTimeToOffsetConverter converts null to null`` () =
        let converter = NullableDbDateTimeToOffsetConverter()
        let value = Nullable<DbDateTime>()
        let result = converter.ConvertToProviderTyped.Invoke(value)
        Assert.Null(result)

    [<Fact>]
    let ``NullableDbDateTimeToOffsetConverter converts value to DateTimeOffset`` () =
        let converter = NullableDbDateTimeToOffsetConverter()
        let dbDateTime = DbDateTime(DateTimeOffset(2020, 6, 15, 10, 30, 0, TimeSpan.Zero)) |> Nullable
        let result = converter.ConvertToProviderTyped.Invoke(dbDateTime)
        Assert.Equal<Nullable<DateTimeOffset>>(DateTimeOffset(2020, 6, 15, 10, 30, 0, TimeSpan.Zero), result)

    [<Fact>]
    let ``NullableDbDateTimeToOffsetConverter converts null from DateTimeOffset`` () =
        let converter = NullableDbDateTimeToOffsetConverter()
        let value = Nullable<DateTimeOffset>()
        let result = converter.ConvertFromProviderTyped.Invoke(value)
        Assert.Null(result)

    [<Fact>]
    let ``NullableDbDateTimeToOffsetConverter converts value from DateTimeOffset`` () =
        let converter = NullableDbDateTimeToOffsetConverter()
        let dateTimeOffset = DateTimeOffset(2020, 6, 15, 10, 30, 0, TimeSpan.Zero) |> Nullable
        let result = converter.ConvertFromProviderTyped.Invoke(dateTimeOffset)
        Assert.True(result.HasValue)
        Assert.Equal(DateTimeOffset(2020, 6, 15, 10, 30, 0, TimeSpan.Zero), result.Value.Value)
