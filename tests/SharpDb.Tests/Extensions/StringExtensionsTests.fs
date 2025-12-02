namespace SharpDb.Tests.Extensions

open SharpDb.Extensions
open System
open Xunit

module StringExtensionsTests =

    let errorTagNotFound tag = sprintf "Query tag '%s' not found." tag
    let errorEmpty = "Query string is empty."
    let errorTagCommentedOut tag = sprintf "Query tag '%s' is commented out." tag

    [<Fact>]
    let ``GetSingleQuery extracts query by tag`` () =
        let queries = "--tag1\nSELECT 1;\n--tag2\nSELECT 2;"
        let result = StringExtensions.GetSingleQuery(queries, "--tag2".AsSpan())
        Assert.Equal("SELECT 2", result.ToString())

    [<Fact>]
    let ``GetSingleQuery extracts trimmed query by tag`` () =
        let queries = "--tag1\nSELECT 1;\n--tag2\n   SELECT 2     ;"
        let result = StringExtensions.GetSingleQuery(queries, "--tag2".AsSpan())
        Assert.Equal("SELECT 2", result.ToString())

    [<Fact>]
    let ``GetSingleQuery extracts query with escaped string by tag`` () =
        let queries = "--tag1\nSELECT 1;\n--tag2\nSELECT 2 from X where Y = 'escaped mama''s string';"
        let result = StringExtensions.GetSingleQuery(queries, "--tag2".AsSpan())
        Assert.Equal("SELECT 2 from X where Y = 'escaped mama''s string'", result.ToString())

    [<Fact>]
    let ``GetSingleQuery extracts query with sl-comment in string`` () =
        let queries = "--tag1\nSELECT 1;\n--tag2\nSELECT 2 from X where Y = 'escaped mama\n--''s string';"
        let result = StringExtensions.GetSingleQuery(queries, "--tag2".AsSpan())
        Assert.Equal("SELECT 2 from X where Y = 'escaped mama\n--''s string'", result.ToString())

    [<Fact>]
    let ``GetSingleQuery extracts query with ml-comment in string`` () =
        let queries = "--tag1\nSELECT 1;\n--tag2\nSELECT 2 from X where Y = 'escaped mama/*''*/s string';"
        let result = StringExtensions.GetSingleQuery(queries, "--tag2".AsSpan())
        Assert.Equal("SELECT 2 from X where Y = 'escaped mama/*''*/s string'", result.ToString())

    [<Fact>]
    let ``GetSingleQuery extracts query with semicolon in string`` () =
        let queries = "--tag1\nSELECT 1;\n--tag2\nSELECT 2 from X where Y = 'escaped mama;s string';"
        let result = StringExtensions.GetSingleQuery(queries, "--tag2".AsSpan())
        Assert.Equal("SELECT 2 from X where Y = 'escaped mama;s string'", result.ToString())

    [<Fact>]
    let ``GetSingleQuery throws if semicolon not escaped (at end)`` () =
        let queries = "--tag1\nSELECT 1;\n--tag2\nSELECT 2 from X where Y = 'nonescaped mamas string'';"
        let call = fun () ->
            let _ = queries.GetSingleQuery("--tag2".AsSpan())
            ignore
        Assert.ThrowsAny(call()) |> ignore

    [<Fact>]
    let ``GetSingleQuery throws if semicolon not escaped (in middle)`` () =
        let queries = "--tag1\nSELECT 1;\n--tag2\nSELECT 2 from X where Y = 'nonescaped mama's string';"
        let call = fun () ->
            let _ = queries.GetSingleQuery("--tag2".AsSpan())
            ignore
        Assert.ThrowsAny(call()) |> ignore

    [<Fact>]
    let ``GetSingleQuery throws if semicolon not escaped (at start)`` () =
        let queries = "--tag1\nSELECT 1;\n--tag2\nSELECT 2 from X where Y = ''nonescaped mamas string';"
        let call = fun () ->
            let _ = queries.GetSingleQuery("--tag2".AsSpan())
            ignore
        Assert.ThrowsAny(call()) |> ignore

    [<Fact>]
    let ``GetSingleQuery throws if tag not found`` () =
        let queries = "--tag1\nSELECT 1;"
        let call = fun () ->
            let _ = queries.GetSingleQuery("--tag2".AsSpan())
            ignore
        Assert.ThrowsAny(call()) |> ignore

    [<Fact>]
    let ``GetSingleQuery trims at semicolon`` () =
        let queries = "--tag\nSELECT 1; SELECT 2;"
        let result = StringExtensions.GetSingleQuery(queries, "--tag".AsSpan())
        Assert.Equal("SELECT 1", result.ToString())

    [<Fact>]
    let ``GetSingleQuery throws if query is empty`` () =
        let queries = "--tag"
        let call = fun () ->
            let _ = queries.GetSingleQuery("--tag".AsSpan())
            ignore
        Assert.ThrowsAny(call()) |> ignore

    [<Fact>]
    let ``GetSingleQuery throws if query is fully commented out by sl-comment`` () =
        let queries = "--tag-- there is a single line comment here"
        let call = fun () ->
            let _ = queries.GetSingleQuery("--tag".AsSpan())
            ignore
        Assert.ThrowsAny(call()) |> ignore

    [<Fact>]
    let ``GetSingleQuery throws if query is fully commented out by ml-comment`` () =
        let queries = "--tag/*some text here\r\n which may span more than one line /* */"
        let call = fun () ->
            let _ = queries.GetSingleQuery("--tag".AsSpan())
            ignore
        Assert.ThrowsAny(call()) |> ignore

    [<Fact>]
    let ``GetSingleQuery throws if query is commented out by ml-comments (joined by whitespaces)`` () =
        let queries = "--tag/*some text here\r\n which may span more*/ \n   /*than one line */"
        let call = fun () ->
            let _ = queries.GetSingleQuery("--tag".AsSpan())
            ignore
        Assert.ThrowsAny(call()) |> ignore


    [<Fact>]
    let ``GetSingleQuery with required parameter succeeds if present and not commented`` () =
        let queries = "--tag\nSELECT @foo FROM bar;"
        let result = StringExtensions.GetSingleQuery(queries, "--tag".AsSpan(), "@foo".AsSpan())
        Assert.Contains("@foo", result.ToString())

    [<Fact>]
    let ``GetSingleQuery with required parameter throws if not present`` () =
        let queries = "--tag\nSELECT bar;"
        let call = fun () ->
            let _ = StringExtensions.GetSingleQuery(queries, "--tag".AsSpan(), "@foo".AsSpan())
            ignore
        let ex = Assert.ThrowsAny(call())
        Assert.Contains("@foo", ex.Message)

    [<Fact>]
    let ``GetSingleQuery with required parameter throws if commented out`` () =
        let queries = "--tag\nSELECT --@foo FROM bar;"
        let call = fun () ->
            let _ = StringExtensions.GetSingleQuery(queries, "--tag".AsSpan(), "@foo".AsSpan())
            ignore
        Assert.ThrowsAny(call()) |> ignore

