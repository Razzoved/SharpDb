namespace SharpDb.Tests

open SharpDb
open Xunit

module DbParameterTests =

    [<Fact>]
    let ``Does not remove char when at sign is not present`` () =
        let param = DbParameter("paramName", 32)
        Assert.Equal("paramName", param.Name)
        Assert.Equal(32, Assert.IsType<int>(param.Value))

    [<Fact>]
    let ``Removes char when at sign is present`` () =
        let param = DbParameter("@paramName", 32)
        Assert.Equal("paramName", param.Name)
        Assert.Equal(32, Assert.IsType<int>(param.Value))

    [<Fact>]
    let ``Handles different data types`` () =
        let intParam = DbParameter("intParam", 42)
        let stringParam = DbParameter("stringParam", "test")
        let boolParam = DbParameter("boolParam", true)
        Assert.Equal("intParam", intParam.Name)
        Assert.Equal(42, Assert.IsType<int>(intParam.Value))
        Assert.Equal("stringParam", stringParam.Name)
        Assert.Equal("test", Assert.IsType<string>(stringParam.Value))
        Assert.Equal("boolParam", boolParam.Name)
        Assert.Equal(true, Assert.IsType<bool>(boolParam.Value))

    [<Fact>]
    let ``Handles null value`` () =
        let nullParam = DbParameter("nullParam", null)
        Assert.Equal("nullParam", nullParam.Name)
        Assert.Null(nullParam.Value)

    [<Fact>]
    let ``Throws on empty parameter name`` () =
        Assert.Throws<System.ArgumentException>(fun () -> DbParameter("", 10) |> ignore)

    [<Fact>]
    let ``Throws on whitespace parameter name`` () =
        Assert.Throws<System.ArgumentException>(fun () -> DbParameter("   ", 10) |> ignore)

    [<Fact>]
    let ``Throws on multi at-sign parameter name`` () =
        Assert.Throws<System.ArgumentException>(fun () -> DbParameter("@@param", 10) |> ignore)

    [<Fact>]
    let ``ToString() returns parameter representation`` () =
        let param = DbParameter("paramName", 100)
        Assert.Equal("@paramName = 100", param.ToString())

    [<Fact>]
    let ``ToString() handles null value`` () =
        let param = DbParameter("paramName", null)
        Assert.Equal("@paramName = null", param.ToString())

    [<Fact>]
    let ``ToString() handles string quoteation`` () =
        let param = DbParameter("paramName", "O'Reilly")
        Assert.Equal("@paramName = 'O''Reilly'", param.ToString())
