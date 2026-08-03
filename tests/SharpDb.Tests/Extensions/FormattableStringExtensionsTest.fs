namespace SharpDb.Tests.Extensions

open System.Runtime.CompilerServices
open SharpDb
open Xunit

module FormattableStringExtensionsTest =

    [<Fact>]
    let ``GetSqlCommandText returns correct SQL with parameters`` () =
        let sql = FormattableStringFactory.Create("SELECT * FROM Test WHERE Id = {0} AND Name = {1}", [| box 42; box "foo" |])
        let text = FormattableStringExtensions.GetSqlCommandText(sql)
        Assert.Equal("SELECT * FROM Test WHERE Id = @p0 AND Name = @p1", text)

    [<Fact>]
    let ``GetSqlCommandText returns SQL without parameters`` () =
        let sql = FormattableStringFactory.Create("SELECT 1", [||])
        let text = FormattableStringExtensions.GetSqlCommandText(sql)
        Assert.Equal("SELECT 1", text)

    [<Fact>]
    let ``GetSqlCommandParameters returns correct parameters`` () =
        let sql = FormattableStringFactory.Create("SELECT * FROM Test WHERE Id = {0} AND Name = {1}", [| box 42; box "foo" |])
        let parameters = FormattableStringExtensions.GetSqlCommandParameters(sql)
        Assert.Equal(2, parameters.Length)
        Assert.Equal("p0", parameters[0].Name)
        Assert.Equal(42, parameters[0].Value :?> int)
        Assert.Equal("p1", parameters[1].Name)
        Assert.Equal("foo", parameters[1].Value :?> string)

    [<Fact>]
    let ``GetSqlCommandParameters returns empty for no parameters`` () =
        let sql = FormattableStringFactory.Create("SELECT 1", [||])
        let parameters = FormattableStringExtensions.GetSqlCommandParameters(sql)
        Assert.Empty(parameters)
