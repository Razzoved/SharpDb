namespace SharpDb.Tests

open SharpDb
open Xunit
open System
open System.Data.Common
open System.Threading.Tasks

module ExceptionExtensionsTests =

    // Mock DbException for testing
    type MockDbException(message, isTransient) =
        inherit DbException(message)
        member val IsTransientValue = isTransient with get
        override _.IsTransient = isTransient

    // ── HasTransientDbError Tests ───────────────────────────────────────────

    [<Fact>]
    let ``HasTransientDbError returns true for transient DbException`` () =
        let ex = MockDbException("Transient error", true)
        Assert.True(ex.HasTransientDbError())

    [<Fact>]
    let ``HasTransientDbError returns false for non-transient DbException`` () =
        let ex = MockDbException("Regular error", false)
        Assert.False(ex.HasTransientDbError())

    [<Fact>]
    let ``HasTransientDbError returns false for non-DbException`` () =
        let ex = Exception("Regular exception")
        Assert.False(ex.HasTransientDbError())

    [<Fact>]
    let ``HasTransientDbError searches inner exception chain`` () =
        let inner = MockDbException("Transient inner", true)
        let outer = Exception("Outer", inner)
        Assert.True(outer.HasTransientDbError())

    [<Fact>]
    let ``HasTransientDbError respects search depth`` () =
        let inner = MockDbException("Transient inner", true)
        let middle = Exception("Middle", inner)
        let outer = Exception("Outer", middle)
        Assert.True(outer.HasTransientDbError(3uy))
        Assert.False(outer.HasTransientDbError(1uy))

    [<Fact>]
    let ``HasTransientDbError returns false when chain has no transient error`` () =
        let inner = MockDbException("Non-transient inner", false)
        let outer = Exception("Outer", inner)
        Assert.False(outer.HasTransientDbError())

    // ── GetTransientDbError Tests ───────────────────────────────────────────

    [<Fact>]
    let ``GetTransientDbError returns transient DbException`` () =
        let ex = MockDbException("Transient error", true)
        let result = ex.GetTransientDbError()
        Assert.NotNull(result)
        Assert.Same(ex, result)

    [<Fact>]
    let ``GetTransientDbError returns null for non-transient DbException`` () =
        let ex = MockDbException("Regular error", false)
        let result = ex.GetTransientDbError()
        Assert.Null(result)

    [<Fact>]
    let ``GetTransientDbError returns null for non-DbException`` () =
        let ex = Exception("Regular exception")
        let result = ex.GetTransientDbError()
        Assert.Null(result)

    [<Fact>]
    let ``GetTransientDbError finds transient in inner exception chain`` () =
        let inner = MockDbException("Transient inner", true)
        let outer = Exception("Outer", inner)
        let result = outer.GetTransientDbError()
        Assert.NotNull(result)
        Assert.Same(inner, result)

    [<Fact>]
    let ``GetTransientDbError respects search depth`` () =
        let inner = MockDbException("Transient inner", true)
        let middle = Exception("Middle", inner)
        let outer = Exception("Outer", middle)
        let result1 = outer.GetTransientDbError(3uy)
        let result2 = outer.GetTransientDbError(1uy)
        Assert.NotNull(result1)
        Assert.Null(result2)

    [<Fact>]
    let ``GetTransientDbError returns null when chain has no transient error`` () =
        let inner = MockDbException("Non-transient inner", false)
        let outer = Exception("Outer", inner)
        let result = outer.GetTransientDbError()
        Assert.Null(result)

    // ── ThrowIfFailed Tests (synchronous) ────────────────────────────────────

    type TestDbResult(isSuccess, error) =
        interface IDbResult with
            member _.IsSuccess = isSuccess
            member _.Error = error

    [<Fact>]
    let ``ThrowIfFailed returns result when successful`` () =
        let result = TestDbResult(true, NoDbError.Instance) :> IDbResult
        let returned = result.ThrowIfFailed()
        Assert.Same(result, returned)

    [<Fact>]
    let ``ThrowIfFailed throws when failed`` () =
        let error = StringDbError("Test error")
        let result = TestDbResult(false, error) :> IDbResult
        Assert.Throws<Exception>(fun () -> result.ThrowIfFailed() |> ignore) |> ignore

    [<Fact>]
    let ``ThrowIfFailed with apply returns result when successful`` () =
        let result = TestDbResult(true, NoDbError.Instance) :> IDbResult
        let apply (e: IDbError) = e.Prefix("Context:")
        let returned = result.ThrowIfFailed(apply)
        Assert.Same(result, returned)

    [<Fact>]
    let ``ThrowIfFailed with apply throws when failed`` () =
        let error = StringDbError("Test error")
        let result = TestDbResult(false, error) :> IDbResult
        let apply (e: IDbError) = e.Prefix("Context:")
        Assert.Throws<Exception>(fun () -> result.ThrowIfFailed(apply) |> ignore) |> ignore

    [<Fact>]
    let ``ThrowIfFailed with apply applies error transformation`` () =
        let error = StringDbError("Test error")
        let result = TestDbResult(false, error) :> IDbResult
        let apply (e: IDbError) = e.Prefix("Context:")
        let ex = Assert.Throws<Exception>(fun () -> result.ThrowIfFailed(apply) |> ignore)
        Assert.Contains("Context:", ex.Message)

    [<Fact>]
    let ``ThrowIfFailed with apply does not throw when apply returns NoDbError`` () =
        let error = StringDbError("Test error")
        let result = TestDbResult(false, error) :> IDbResult
        let apply (e: IDbError) = NoDbError.Instance :> IDbError
        let returned = result.ThrowIfFailed(apply)
        Assert.Same(result, returned)

    // ── ThrowIfFailed Tests (Task) ─────────────────────────────────────────

    [<Fact>]
    let ``ThrowIfFailed on Task returns result when successful`` () =
        let task = Task.FromResult(TestDbResult(true, NoDbError.Instance) :> IDbResult)
        let resultTask = task.ThrowIfFailed()
        let result = resultTask.Result
        Assert.True(result.IsSuccess)

    [<Fact>]
    let ``ThrowIfFailed on Task throws when failed`` () =
        let error = StringDbError("Test error")
        let task = Task.FromResult(TestDbResult(false, error) :> IDbResult).ThrowIfFailed()
        Assert.Throws<Exception>(fun () -> task.GetAwaiter().GetResult() |> ignore) |> ignore

    [<Fact>]
    let ``ThrowIfFailed on Task throws when task is faulted`` () =
        let task = Task.FromException<IDbResult>(Exception("Task failed")).ThrowIfFailed()
        Assert.Throws<Exception>(fun () -> task.GetAwaiter().GetResult() |> ignore) |> ignore

    [<Fact>]
    let ``ThrowIfFailed on Task with apply returns result when successful`` () =
        let task = Task.FromResult(TestDbResult(true, NoDbError.Instance) :> IDbResult)
        let apply (e: IDbError) = e.Prefix("Context:")
        let resultTask = task.ThrowIfFailed(apply)
        let result = resultTask.Result
        Assert.True(result.IsSuccess)

    [<Fact>]
    let ``ThrowIfFailed on Task with apply throws when failed`` () =
        let error = StringDbError("Test error")
        let task = Task.FromResult(TestDbResult(false, error) :> IDbResult)
        let apply (e: IDbError) = e.Prefix("Context:")
        let resultTask = task.ThrowIfFailed(apply)
        Assert.Throws<Exception>(fun () -> resultTask.GetAwaiter().GetResult() |> ignore) |> ignore

    // ── ThrowIfFailed Tests (ValueTask) ────────────────────────────────────

    [<Fact>]
    let ``ThrowIfFailed on ValueTask returns result when successful`` () =
        let valueTask = ValueTask.FromResult(TestDbResult(true, NoDbError.Instance) :> IDbResult)
        let resultTask = valueTask.ThrowIfFailed()
        let result = resultTask.Result
        Assert.True(result.IsSuccess)

    [<Fact>]
    let ``ThrowIfFailed on ValueTask throws when failed`` () =
        let error = StringDbError("Test error")
        let valueTask = ValueTask.FromResult(TestDbResult(false, error) :> IDbResult)
        let resultTask = valueTask.ThrowIfFailed()
        Assert.Throws<Exception>(fun () -> resultTask.GetAwaiter().GetResult() |> ignore) |> ignore

    [<Fact>]
    let ``ThrowIfFailed on ValueTask with apply returns result when successful`` () =
        let valueTask = ValueTask.FromResult(TestDbResult(true, NoDbError.Instance) :> IDbResult)
        let apply (e: IDbError) = e.Prefix("Context:")
        let resultTask = valueTask.ThrowIfFailed(apply)
        let result = resultTask.Result
        Assert.True(result.IsSuccess)

    [<Fact>]
    let ``ThrowIfFailed on ValueTask with apply throws when failed`` () =
        let error = StringDbError("Test error")
        let valueTask = ValueTask.FromResult(TestDbResult(false, error) :> IDbResult)
        let apply (e: IDbError) = e.Prefix("Context:")
        let resultTask = valueTask.ThrowIfFailed(apply)
        Assert.Throws<Exception>(fun () -> resultTask.GetAwaiter().GetResult() |> ignore) |> ignore
