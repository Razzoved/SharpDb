namespace SharpDb.Tests

open SharpDb
open Xunit

module ActionStateTests =

    [<Fact>]
    let ``Complete returns successful state`` () =
        let state = ActionState.Complete()
        Assert.False(state.IsAborted)
        Assert.IsType<NoDbError>(state.Error)

    [<Fact>]
    let ``Abort with IDbError returns aborted state`` () =
        let error = StringDbError("Test error")
        let state = ActionState.Abort(error)
        Assert.True(state.IsAborted)
        Assert.Equal("Test error", state.Error.Message)

    [<Fact>]
    let ``Abort with string returns aborted state`` () =
        let state = ActionState.Abort("Test error")
        Assert.True(state.IsAborted)
        Assert.Contains("Test error", state.Error.Message)

    [<Fact>]
    let ``Abort with null IDbError uses unspecified error`` () =
        let state = ActionState.Abort(null :> IDbError)
        Assert.True(state.IsAborted)
        Assert.Contains("Unspecified error", state.Error.Message)

    [<Fact>]
    let ``Abort with NoDbError uses unspecified error`` () =
        let state = ActionState.Abort(NoDbError.Instance)
        Assert.True(state.IsAborted)
        Assert.Contains("Unspecified error", state.Error.Message)

    [<Fact>]
    let ``Abort with string includes caller information`` () =
        let state = ActionState.Abort("Validation failed")
        Assert.True(state.IsAborted)
        Assert.Contains("Validation failed", state.Error.Message)
        Assert.Contains("[", state.Error.Message)
        Assert.Contains("]", state.Error.Message)

    [<Fact>]
    let ``Error property returns NoDbError for Complete state`` () =
        let state = ActionState.Complete()
        Assert.Same(NoDbError.Instance, state.Error)

    [<Fact>]
    let ``Error property returns error for Abort state`` () =
        let error = StringDbError("Custom error")
        let state = ActionState.Abort(error)
        Assert.Same(error, state.Error)
