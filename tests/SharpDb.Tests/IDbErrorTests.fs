namespace SharpDb.Tests

open SharpDb
open Xunit
open System

module IDbErrorTests =

    // ── NoDbError Tests ─────────────────────────────────────────────────────

    [<Fact>]
    let ``NoDbError Instance returns singleton`` () =
        let error1 = NoDbError.Instance
        let error2 = NoDbError.Instance
        Assert.Same(error1, error2)

    [<Fact>]
    let ``NoDbError Message is empty`` () =
        let error = NoDbError.Instance
        Assert.Equal("", error.Message)

    [<Fact>]
    let ``NoDbError Prefix throws NotSupportedException`` () =
        let error: IDbError = NoDbError.Instance
        Assert.Throws<NotSupportedException>(fun () -> error.Prefix("prefix") |> ignore) |> ignore

    [<Fact>]
    let ``NoDbError Set throws NotSupportedException`` () =
        let error: IDbError = NoDbError.Instance
        Assert.Throws<NotSupportedException>(fun () -> error.Set("new message") |> ignore) |> ignore

    [<Fact>]
    let ``NoDbError ToException throws NotSupportedException`` () =
        let error: IDbError = NoDbError.Instance
        Assert.Throws<NotSupportedException>(fun () -> error.ToException() |> ignore) |> ignore

    [<Fact>]
    let ``NoDbError Equals returns true for same type`` () =
        let error1 = NoDbError.Instance
        let error2 = NoDbError.Instance
        Assert.True(error1.Equals(error2))

    [<Fact>]
    let ``NoDbError Equals returns false for different type`` () =
        let error = NoDbError.Instance
        let other = StringDbError("test")
        Assert.False(error.Equals(other))

    [<Fact>]
    let ``NoDbError GetHashCode returns consistent value`` () =
        let error = NoDbError.Instance
        let hash1 = error.GetHashCode()
        let hash2 = error.GetHashCode()
        Assert.Equal(hash1, hash2)

    [<Fact>]
    let ``NoDbError ToString returns empty string`` () =
        let error = NoDbError.Instance
        Assert.Equal("", error.ToString())

    // ── StringDbError Tests ─────────────────────────────────────────────────

    [<Fact>]
    let ``StringDbError stores message`` () =
        let error = StringDbError("Test error message")
        Assert.Equal("Test error message", error.Message)

    [<Fact>]
    let ``StringDbError Set updates message`` () =
        let error = StringDbError("Original")
        let updated = error.Set("Updated")
        Assert.Same(error, updated)
        Assert.Equal("Updated", error.Message)

    [<Fact>]
    let ``StringDbError Prefix adds prefix to message`` () =
        let error: IDbError = StringDbError("Error details")
        let prefixed = error.Prefix("Context:")
        Assert.Equal("Context: - Error details", prefixed.Message)

    [<Fact>]
    let ``StringDbError Prefix with empty message sets to prefix`` () =
        let error: IDbError = StringDbError("")
        let prefixed = error.Prefix("Context:")
        Assert.Equal("Context:", prefixed.Message)

    [<Fact>]
    let ``StringDbError ToException returns exception with message`` () =
        let error: IDbError = StringDbError("Test error")
        let ex = error.ToException()
        Assert.Equal("Test error", ex.Message)

    [<Fact>]
    let ``StringDbError Equals returns true for same message`` () =
        let error1 = StringDbError("Same message")
        let error2 = StringDbError("Same message")
        Assert.True(error1.Equals(error2))

    [<Fact>]
    let ``StringDbError Equals returns false for different message`` () =
        let error1 = StringDbError("Message 1")
        let error2 = StringDbError("Message 2")
        Assert.False(error1.Equals(error2))

    [<Fact>]
    let ``StringDbError GetHashCode returns consistent value for same message`` () =
        let error1 = StringDbError("Same message")
        let error2 = StringDbError("Same message")
        Assert.Equal(error1.GetHashCode(), error2.GetHashCode())

    [<Fact>]
    let ``StringDbError ToString returns message`` () =
        let error = StringDbError("Test message")
        Assert.Equal("Test message", error.ToString())

    // ── ExceptionDbError Tests ─────────────────────────────────────────────

    [<Fact>]
    let ``ExceptionDbError stores exception`` () =
        let ex = Exception("Test exception")
        let error = ExceptionDbError(ex)
        Assert.Same(ex, error.Exception)

    [<Fact>]
    let ``ExceptionDbError Message defaults to exception message`` () =
        let ex = Exception("Test exception message")
        let error = ExceptionDbError(ex)
        Assert.Equal("Test exception message", error.Message)

    [<Fact>]
    let ``ExceptionDbError with custom message uses custom message`` () =
        let ex = Exception("Original message")
        let error = ExceptionDbError(ex, "Custom message")
        Assert.Equal("Custom message", error.Message)

    [<Fact>]
    let ``ExceptionDbError IsTransient defaults to false for non-transient exceptions`` () =
        let ex = Exception("Regular exception")
        let error = ExceptionDbError(ex)
        Assert.False(error.IsTransient)

    [<Fact>]
    let ``ExceptionDbError Set updates message`` () =
        let ex = Exception("Test")
        let error = ExceptionDbError(ex)
        let updated = error.Set("Updated message")
        Assert.Same(error, updated)
        Assert.Equal("Updated message", error.Message)

    [<Fact>]
    let ``ExceptionDbError Prefix adds prefix to message`` () =
        let ex = Exception("Test")
        let error: IDbError = ExceptionDbError(ex)
        let prefixed = error.Prefix("Operation failed:")
        Assert.Equal("Operation failed: - Test", prefixed.Message)

    [<Fact>]
    let ``ExceptionDbError ToException returns original when message matches`` () =
        let ex = Exception("Test message")
        let error: IDbError = ExceptionDbError(ex)
        let resultEx = error.ToException()
        Assert.Same(ex, resultEx)

    [<Fact>]
    let ``ExceptionDbError ToException returns new exception when message differs`` () =
        let ex = Exception("Original")
        let error: IDbError = ExceptionDbError(ex, "Custom")
        let resultEx = error.ToException()
        Assert.NotSame(ex, resultEx)
        Assert.Equal("Custom", resultEx.Message)
        Assert.Same(ex, resultEx.InnerException)

    [<Fact>]
    let ``ExceptionDbError Equals returns true for same exception`` () =
        let ex = Exception("Test")
        let error1 = ExceptionDbError(ex)
        let error2 = ExceptionDbError(ex)
        Assert.True(error1.Equals(error2))

    [<Fact>]
    let ``ExceptionDbError Equals returns false for different exception`` () =
        let ex1 = Exception("Test 1")
        let ex2 = Exception("Test 2")
        let error1 = ExceptionDbError(ex1)
        let error2 = ExceptionDbError(ex2)
        Assert.False(error1.Equals(error2))

    [<Fact>]
    let ``ExceptionDbError GetHashCode returns consistent value`` () =
        let ex = Exception("Test")
        let error1 = ExceptionDbError(ex)
        let error2 = ExceptionDbError(ex)
        Assert.Equal(error1.GetHashCode(), error2.GetHashCode())

    // ── IDbError.AreEqual Tests ────────────────────────────────────────────

    [<Fact>]
    let ``IDbError.AreEqual returns true for same reference`` () =
        let error = StringDbError("Test")
        Assert.True(IDbError.AreEqual(error, error))

    [<Fact>]
    let ``IDbError.AreEqual returns true for equal values`` () =
        let error1 = StringDbError("Same")
        let error2 = StringDbError("Same")
        Assert.True(IDbError.AreEqual(error1, error2))

    [<Fact>]
    let ``IDbError.AreEqual returns false for different values`` () =
        let error1 = StringDbError("One")
        let error2 = StringDbError("Two")
        Assert.False(IDbError.AreEqual(error1, error2))

    [<Fact>]
    let ``IDbError.AreEqual returns false when one is null`` () =
        let error = StringDbError("Test")
        Assert.False(IDbError.AreEqual(error, null))
        Assert.False(IDbError.AreEqual(null, error))

    [<Fact>]
    let ``IDbError.AreEqual returns true when both are null`` () =
        Assert.True(IDbError.AreEqual(null, null))
