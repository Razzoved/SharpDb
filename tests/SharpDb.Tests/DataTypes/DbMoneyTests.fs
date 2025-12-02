namespace SharpDb.Tests.DataTypes

open SharpDb.Entities.DataTypes
open Xunit

module DbMoneyTests =

    [<Fact>]
    let ``DbMoney preserves valid value`` () =
        let value = 12345.67M
        let dbMoney = DbMoney(value)
        Assert.Equal(value, dbMoney.Value)

    [<Fact>]
    let ``DbMoney implements IComparable and IEquatable`` () =
        let v1 = DbMoney(100M)
        let v2 = DbMoney(100M)
        let v3 = DbMoney(200M)
        Assert.Equal(0, v1.CompareTo(v2))
        Assert.True(v1.Equals(v2))
        Assert.False(v1.Equals(v3))
        Assert.Equal(-1, v1.CompareTo(v3))
        Assert.Equal(1, v3.CompareTo(v1))
