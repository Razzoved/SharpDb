namespace SharpDb.Tests.DataTypes

open System
open Xunit
open SharpDb.Entities.DataTypes

module DbDateTimeTests =

    [<Fact>]
    let ``DbDateTime clamps year below 1753 to 1900-01-01`` () =
        let dt = DateTimeOffset(1000, 5, 5, 12, 0, 0, TimeSpan.Zero)
        let dbdt = DbDateTime(dt)
        let expected = DateTimeOffset(1753, 1, 1, 0, 0, 0, TimeSpan.Zero)
        Assert.Equal(expected, dbdt.Value)

    [<Fact>]
    let ``DbDateTime preserves valid date`` () =
        let dt = DateTimeOffset(2020, 6, 15, 10, 30, 0, TimeSpan.Zero)
        let dbdt = DbDateTime(dt)
        Assert.Equal(dt, dbdt.Value)

    [<Fact>]
    let ``DbDateTime implements IComparable and IEquatable`` () =
        let dt1 = DbDateTime(DateTime(2022, 1, 1))
        let dt2 = DbDateTime(DateTime(2022, 1, 1))
        let dt3 = DbDateTime(DateTime(2022, 1, 2))
        Assert.Equal(0, dt1.CompareTo(dt2))
        Assert.True(dt1.Equals(dt2))
        Assert.False(dt1.Equals(dt3))
        Assert.Equal(-1, dt1.CompareTo(dt3))
        Assert.Equal(1, dt3.CompareTo(dt1))
