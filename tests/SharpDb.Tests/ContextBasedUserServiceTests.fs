namespace SharpDb.Tests

open SharpDb.Entities
open SharpDb.Services.Impl
open Xunit

module ContextBaseUserServiceTests =

    type DummyUser() =
        interface IUser with
            member _.GetID() = 42 :> obj
            member _.GetDisplayName() = "Test User"

    [<Fact>]
    let ``GetCurrentUser returns None when no user is set`` () =
        let service = ContextBasedUserService()
        Assert.Null(service.GetCurrentUser())
        Assert.Null(service.GetCurrentUserID())
        Assert.Null(service.GetCurrentUserDisplayName())

    [<Fact>]
    let ``GetCurrentUser returns user when set in context`` () =
        let service = ContextBasedUserService()
        let user = DummyUser() :> IUser
        use _ctx = new ContextBasedUserService.UserContext(user)
        Assert.Equal(user, service.GetCurrentUser())
        Assert.Equal(42, service.GetCurrentUserID() :?> int)
        Assert.Equal("Test User", service.GetCurrentUserDisplayName())

    [<Fact>]
    let ``UserContext restores previous user on dispose`` () =
        let service = ContextBasedUserService()
        let user1 = DummyUser() :> IUser
        let user2 = DummyUser() :> IUser
        use ctx1 = new ContextBasedUserService.UserContext(user1)
        Assert.Equal(user1, service.GetCurrentUser())
        use ctx2 = new ContextBasedUserService.UserContext(user2)
        Assert.Equal(user2, service.GetCurrentUser())
        ctx2.Dispose()
        Assert.Equal(user1, service.GetCurrentUser())
        ctx1.Dispose()
        Assert.Null(service.GetCurrentUser())
