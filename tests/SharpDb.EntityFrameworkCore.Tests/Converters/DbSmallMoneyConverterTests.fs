namespace SharpDb.EntityFrameworkCore.Tests.Converters

open System
open SharpDb.Entities.DataTypes
open SharpDb.EntityFrameworkCore.Converters
open Xunit

module DbSmallMoneyConverterTests =

    [<Fact>]
    let ``DbSmallMoneyConverter converts to decimal`` () =
        let converter = DbSmallMoneyConverter()
        let dbSmallMoney = DbSmallMoney(1234.56M)
        let result = converter.ConvertToProviderTyped.Invoke(dbSmallMoney)
        Assert.Equal(1234.56M, result)

    [<Fact>]
    let ``NullableDbSmallMoneyConverter converts null to null`` () =
        let converter = NullableDbSmallMoneyConverter()
        let value = Nullable<DbSmallMoney>()
        let result = converter.ConvertToProviderTyped.Invoke(value)
        Assert.Null(result)

    [<Fact>]
    let ``NullableDbSmallMoneyConverter converts value to decimal`` () =
        let converter = NullableDbSmallMoneyConverter()
        let dbSmallMoney = DbSmallMoney(1234.56M) |> Nullable
        let result = converter.ConvertToProviderTyped.Invoke(dbSmallMoney)
        Assert.Equal<Nullable<decimal>>(1234.56M, result)

    [<Fact>]
    let ``DbSmallMoneyConverter converts from decimal`` () =
        let converter = DbSmallMoneyConverter()
        let value = 1234.56M
        let result = converter.ConvertFromProviderTyped.Invoke(value)
        Assert.Equal(1234.56M, result.Value)

    [<Fact>]
    let ``NullableDbSmallMoneyConverter converts null from decimal`` () =
        let converter = NullableDbSmallMoneyConverter()
        let value = Nullable<decimal>()
        let result = converter.ConvertFromProviderTyped.Invoke(value)
        Assert.Null(result)

    [<Fact>]
    let ``NullableDbSmallMoneyConverter converts value from decimal`` () =
        let converter = NullableDbSmallMoneyConverter()
        let value = 1234.56M |> Nullable
        let result = converter.ConvertFromProviderTyped.Invoke(value)
        Assert.True(result.HasValue)
        Assert.Equal(1234.56M, result.Value.Value)
