namespace SharpDb.EntityFrameworkCore.Tests.Converters

open System
open SharpDb.Entities.DataTypes
open SharpDb.EntityFrameworkCore.Converters
open Xunit

module DbMoneyConverterTests =

    [<Fact>]
    let ``DbMoneyConverter converts to decimal`` () =
        let converter = DbMoneyConverter()
        let dbMoney = DbMoney(12345.67M)
        let result = converter.ConvertToProviderTyped.Invoke(dbMoney)
        Assert.Equal(12345.67M, result)

    [<Fact>]
    let ``DbMoneyConverter converts from decimal`` () =
        let converter = DbMoneyConverter()
        let value = 12345.67M
        let result = converter.ConvertFromProviderTyped.Invoke(value)
        Assert.Equal(12345.67M, result.Value)

    [<Fact>]
    let ``NullableDbMoneyConverter converts null to null`` () =
        let converter = NullableDbMoneyConverter()
        let value = Nullable<DbMoney>()
        let result = converter.ConvertToProviderTyped.Invoke(value)
        Assert.Null(result)

    [<Fact>]
    let ``NullableDbMoneyConverter converts value to decimal`` () =
        let converter = NullableDbMoneyConverter()
        let dbMoney = DbMoney(12345.67M) |> Nullable
        let result = converter.ConvertToProviderTyped.Invoke(dbMoney)
        Assert.Equal<Nullable<decimal>>(12345.67M, result)

    [<Fact>]
    let ``NullableDbMoneyConverter converts null from decimal`` () =
        let converter = NullableDbMoneyConverter()
        let value = Nullable<decimal>()
        let result = converter.ConvertFromProviderTyped.Invoke(value)
        Assert.Null(result)

    [<Fact>]
    let ``NullableDbMoneyConverter converts value from decimal`` () =
        let converter = NullableDbMoneyConverter()
        let value = 12345.67M |> Nullable
        let result = converter.ConvertFromProviderTyped.Invoke(value)
        Assert.True(result.HasValue)
        Assert.Equal(12345.67M, result.Value.Value)
