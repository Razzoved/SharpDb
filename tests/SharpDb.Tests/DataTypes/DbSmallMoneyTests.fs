namespace SharpDb.Tests.DataTypes

open SharpDb.Entities.DataTypes
open System
open Xunit

module DbSmallMoneyTests =

    [<Fact>]
    let ``DbSmallMoney throws value below min`` () =
        let minValue = -214748.3648M
        let belowMin = minValue - 1.0M
        Assert.Throws<ArgumentOutOfRangeException>(fun () -> DbSmallMoney(belowMin) |> ignore)

    [<Fact>]
    let ``DbSmallMoney throws value above max`` () =
        let maxValue = 214748.3647M
        let aboveMax = maxValue + 1.0M
        Assert.Throws<ArgumentOutOfRangeException>(fun () -> DbSmallMoney(aboveMax) |> ignore)

    [<Fact>]
    let ``DbSmallMoney preserves valid value`` () =
        let value = 1234.56M
        let dbSmallMoney = DbSmallMoney(value)
        Assert.Equal(value, dbSmallMoney.Value)

    [<Fact>]
    let ``DbSmallMoney implements IComparable and IEquatable`` () =
        let v1 = DbSmallMoney(100M)
        let v2 = DbSmallMoney(100M)
        let v3 = DbSmallMoney(200M)
        Assert.Equal(0, v1.CompareTo(v2))
        Assert.True(v1.Equals(v2))
        Assert.False(v1.Equals(v3))
        Assert.Equal(-1, v1.CompareTo(v3))
        Assert.Equal(1, v3.CompareTo(v1))
