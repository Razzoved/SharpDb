namespace SharpDb.Tests.Exceptions

open SharpDb
open SharpDb.Exceptions
open Xunit
open System

module TransactionRollbackExceptionTests =

    [<Fact>]
    let ``Constructor stores DbError`` () =
        let error = StringDbError("Test error")
        let ex = TransactionRollbackException(error, null)
        Assert.Same(error, ex.DbError)

    [<Fact>]
    let ``Constructor stores RollbackException`` () =
        let error = StringDbError("Test error")
        let rollbackEx = Exception("Rollback failed")
        let ex = TransactionRollbackException(error, rollbackEx)
        Assert.Same(rollbackEx, ex.RollbackException)

    [<Fact>]
    let ``Constructor accepts null RollbackException`` () =
        let error = StringDbError("Test error")
        let ex = TransactionRollbackException(error, null)
        Assert.Null(ex.RollbackException)

    [<Fact>]
    let ``Message returns DbError message`` () =
        let error = StringDbError("Custom error message")
        let ex = TransactionRollbackException(error, null)
        Assert.Equal("Custom error message", ex.Message)

    [<Fact>]
    let ``Message returns DbError message when RollbackException is present`` () =
        let error = StringDbError("Db error message")
        let rollbackEx = Exception("Rollback message")
        let ex = TransactionRollbackException(error, rollbackEx)
        Assert.Equal("Db error message", ex.Message)

    [<Fact>]
    let ``InnerException is set when DbError is ExceptionDbError`` () =
        let innerEx = Exception("Inner exception")
        let error = ExceptionDbError(innerEx)
        let ex = TransactionRollbackException(error, null)
        Assert.Same(innerEx, ex.InnerException)

    [<Fact>]
    let ``InnerException is null when DbError is StringDbError`` () =
        let error = StringDbError("String error")
        let ex = TransactionRollbackException(error, null)
        Assert.Null(ex.InnerException)

    [<Fact>]
    let ``InnerException is null when DbError is NoDbError`` () =
        let error = NoDbError.Instance :> IDbError
        let ex = TransactionRollbackException(error, null)
        Assert.Null(ex.InnerException)

    [<Fact>]
    let ``Constructor with ExceptionDbError uses exception as inner`` () =
        let innerEx = Exception("Database error")
        let error = ExceptionDbError(innerEx)
        let rollbackEx = Exception("Rollback error")
        let ex = TransactionRollbackException(error, rollbackEx)
        Assert.Same(innerEx, ex.InnerException)
        Assert.Same(rollbackEx, ex.RollbackException)
