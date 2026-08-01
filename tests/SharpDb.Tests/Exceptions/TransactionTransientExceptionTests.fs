namespace SharpDb.Tests.Exceptions

open SharpDb.Exceptions
open Xunit
open System
open System.Data.Common

module TransactionTransientExceptionTests =

    // Mock DbException for testing
    type MockTransientDbException(message) =
        inherit DbException(message)
        override _.IsTransient = true

    type MockNonTransientDbException(message) =
        inherit DbException(message)
        override _.IsTransient = false

    [<Fact>]
    let ``Constructor with exception extracts transient DbException`` () =
        let dbEx = MockTransientDbException("Transient error")
        let ex = TransactionTransientException(dbEx)
        Assert.Same(dbEx, ex.DbException)

    [<Fact>]
    let ``Constructor with exception and message uses custom message`` () =
        let dbEx = MockTransientDbException("Transient error")
        let ex = TransactionTransientException("Custom message", dbEx)
        Assert.Equal("Custom message", ex.Message)

    [<Fact>]
    let ``Constructor with exception uses exception message by default`` () =
        let dbEx = MockTransientDbException("Transient error")
        let ex = TransactionTransientException(dbEx)
        Assert.Equal("Transient error", ex.Message)

    [<Fact>]
    let ``Constructor throws when exception has no transient DbError`` () =
        let ex = Exception("Regular exception")
        Assert.Throws<ArgumentException>(fun () -> TransactionTransientException(ex) |> ignore) |> ignore

    [<Fact>]
    let ``Constructor throws when exception has non-transient DbException`` () =
        let dbEx = MockNonTransientDbException("Non-transient error")
        Assert.Throws<ArgumentException>(fun () -> TransactionTransientException(dbEx) |> ignore) |> ignore

    [<Fact>]
    let ``Constructor finds transient DbException in inner exception chain`` () =
        let innerDbEx = MockTransientDbException("Transient inner")
        let outerEx = Exception("Outer", innerDbEx)
        let ex = TransactionTransientException(outerEx)
        Assert.Same(innerDbEx, ex.DbException)

    [<Fact>]
    let ``Constructor sets inner exception correctly`` () =
        let dbEx = MockTransientDbException("Transient error")
        let ex = TransactionTransientException(dbEx)
        Assert.Same(dbEx, ex.InnerException)

    [<Fact>]
    let ``Constructor with custom message sets inner exception correctly`` () =
        let dbEx = MockTransientDbException("Transient error")
        let ex = TransactionTransientException("Custom message", dbEx)
        Assert.Same(dbEx, ex.InnerException)
