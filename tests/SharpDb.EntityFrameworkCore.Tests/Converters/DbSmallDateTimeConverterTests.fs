namespace SharpDb.EntityFrameworkCore.Tests.Converters

open System
open SharpDb.Entities.DataTypes
open SharpDb.EntityFrameworkCore.Converters
open Xunit

module DbSmallDateTimeConverterTests =

    [<Fact>]
    let ``DbSmallDateTimeConverter converts to DateTime`` () =
        let converter = DbSmallDateTimeConverter()
        let dbSmallDateTime = DbSmallDateTime(DateTimeOffset(2020, 6, 15, 10, 30, 0, TimeSpan.Zero))
        let result = converter.ConvertToProviderTyped.Invoke(dbSmallDateTime)
        Assert.Equal(DateTimeOffset(2020, 6, 15, 10, 30, 0, TimeSpan.Zero).DateTime, result)

    [<Fact>]
    let ``DbSmallDateTimeConverter converts from DateTime`` () =
        let converter = DbSmallDateTimeConverter()
        let dateTime = DateTime.SpecifyKind(DateTime(2020, 6, 15, 10, 30, 0), DateTimeKind.Utc)
        let result = converter.ConvertFromProviderTyped.Invoke(dateTime)
        Assert.Equal(DateTimeOffset(2020, 6, 15, 10, 30, 0, TimeSpan.Zero), result.Value)

    [<Fact>]
    let ``DbSmallDateTimeToOffsetConverter converts to DateTimeOffset`` () =
        let converter = DbSmallDateTimeToOffsetConverter()
        let dbSmallDateTime = DbSmallDateTime(DateTimeOffset(2020, 6, 15, 10, 30, 0, TimeSpan.Zero))
        let result = converter.ConvertToProviderTyped.Invoke(dbSmallDateTime)
        Assert.Equal(DateTimeOffset(2020, 6, 15, 10, 30, 0, TimeSpan.Zero), result)

    [<Fact>]
    let ``DbSmallDateTimeToOffsetConverter converts from DateTimeOffset`` () =
        let converter = DbSmallDateTimeToOffsetConverter()
        let dateTimeOffset = DateTimeOffset(2020, 6, 15, 10, 30, 0, TimeSpan.Zero)
        let result = converter.ConvertFromProviderTyped.Invoke(dateTimeOffset)
        Assert.Equal(DateTimeOffset(2020, 6, 15, 10, 30, 0, TimeSpan.Zero), result.Value)

    [<Fact>]
    let ``NullableDbSmallDateTimeConverter converts null to null`` () =
        let converter = NullableDbSmallDateTimeConverter()
        let value = Nullable<DbSmallDateTime>()
        let result = converter.ConvertToProviderTyped.Invoke(value)
        Assert.Null(result)

    [<Fact>]
    let ``NullableDbSmallDateTimeConverter converts value to DateTime`` () =
        let converter = NullableDbSmallDateTimeConverter()
        let dbSmallDateTime = DbSmallDateTime(DateTimeOffset(2020, 6, 15, 10, 30, 0, TimeSpan.Zero)) |> Nullable
        let result = converter.ConvertToProviderTyped.Invoke(dbSmallDateTime)
        Assert.Equal<Nullable<DateTime>>(DateTimeOffset(2020, 6, 15, 10, 30, 0, TimeSpan.Zero).DateTime, result)

    [<Fact>]
    let ``NullableDbSmallDateTimeConverter converts null from DateTime`` () =
        let converter = NullableDbSmallDateTimeConverter()
        let value = Nullable<DateTime>()
        let result = converter.ConvertFromProviderTyped.Invoke(value)
        Assert.Null(result)

    [<Fact>]
    let ``NullableDbSmallDateTimeConverter converts value from DateTime`` () =
        let converter = NullableDbSmallDateTimeConverter()
        let dateTime = DateTime.SpecifyKind(DateTime(2020, 6, 15, 10, 30, 0), DateTimeKind.Utc) |> Nullable
        let result = converter.ConvertFromProviderTyped.Invoke(dateTime)
        Assert.True(result.HasValue)
        Assert.Equal(DateTimeOffset(2020, 6, 15, 10, 30, 0, TimeSpan.Zero), result.Value.Value)

    [<Fact>]
    let ``NullableDbSmallDateTimeToOffsetConverter converts null to null`` () =
        let converter = NullableDbSmallDateTimeToOffsetConverter()
        let value = Nullable<DbSmallDateTime>()
        let result = converter.ConvertToProviderTyped.Invoke(value)
        Assert.Null(result)

    [<Fact>]
    let ``NullableDbSmallDateTimeToOffsetConverter converts value to DateTimeOffset`` () =
        let converter = NullableDbSmallDateTimeToOffsetConverter()
        let dbSmallDateTime = DbSmallDateTime(DateTimeOffset(2020, 6, 15, 10, 30, 0, TimeSpan.Zero)) |> Nullable
        let result = converter.ConvertToProviderTyped.Invoke(dbSmallDateTime)
        Assert.Equal<Nullable<DateTimeOffset>>(DateTimeOffset(2020, 6, 15, 10, 30, 0, TimeSpan.Zero), result)

    [<Fact>]
    let ``NullableDbSmallDateTimeToOffsetConverter converts null from DateTimeOffset`` () =
        let converter = NullableDbSmallDateTimeToOffsetConverter()
        let value = Nullable<DateTimeOffset>()
        let result = converter.ConvertFromProviderTyped.Invoke(value)
        Assert.Null(result)

    [<Fact>]
    let ``NullableDbSmallDateTimeToOffsetConverter converts value from DateTimeOffset`` () =
        let converter = NullableDbSmallDateTimeToOffsetConverter()
        let dateTimeOffset = DateTimeOffset(2020, 6, 15, 10, 30, 0, TimeSpan.Zero) |> Nullable
        let result = converter.ConvertFromProviderTyped.Invoke(dateTimeOffset)
        Assert.True(result.HasValue)
        Assert.Equal(DateTimeOffset(2020, 6, 15, 10, 30, 0, TimeSpan.Zero), result.Value.Value)
