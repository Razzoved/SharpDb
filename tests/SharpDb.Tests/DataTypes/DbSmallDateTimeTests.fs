namespace SharpDb.Tests.DataTypes

open SharpDb.Entities.DataTypes
open System
open Xunit

module DbSmallDateTimeTests =

    [<Fact>]
    let ``DbSmallDateTime clamps year below 1900 to 1900-01-01`` () =
        let dt = DateTimeOffset(1000, 5, 5, 12, 0, 0, TimeSpan.Zero)
        let dbdt = DbSmallDateTime(dt)
        let expected = DateTimeOffset(1900, 1, 1, 0, 0, 0, TimeSpan.Zero)
        Assert.Equal(expected, dbdt.Value)

    [<Fact>]
    let ``DbSmallDateTime clamps year above 2079 to 2079-06-06 23:59:59`` () =
        let dt = DateTimeOffset(2100, 1, 1, 0, 0, 0, TimeSpan.Zero)
        let dbdt = DbSmallDateTime(dt)
        let expected = DateTimeOffset(2079, 6, 6, 23, 59, 59, TimeSpan.Zero)
        Assert.Equal(expected, dbdt.Value)

    [<Fact>]
    let ``DbSmallDateTime preserves valid date`` () =
        let dt = DateTimeOffset(2020, 6, 15, 10, 30, 0, TimeSpan.Zero)
        let dbdt = DbSmallDateTime(dt)
        Assert.Equal(dt, dbdt.Value)

    [<Fact>]
    let ``DbSmallDateTime implements IComparable and IEquatable`` () =
        let dt1 = DbSmallDateTime(DateTime(2022, 1, 1))
        let dt2 = DbSmallDateTime(DateTime(2022, 1, 1))
        let dt3 = DbSmallDateTime(DateTime(2022, 1, 2))
        Assert.Equal(0, dt1.CompareTo(dt2))
        Assert.True(dt1.Equals(dt2))
        Assert.False(dt1.Equals(dt3))
        Assert.Equal(-1, dt1.CompareTo(dt3))
        Assert.Equal(1, dt3.CompareTo(dt1))
