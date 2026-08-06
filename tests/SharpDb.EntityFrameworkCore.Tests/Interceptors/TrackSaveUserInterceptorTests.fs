namespace SharpDb.EntityFrameworkCore.Tests.Interceptors

open System
open Microsoft.EntityFrameworkCore
open SharpDb.Entities
open SharpDb.EntityFrameworkCore.Entities
open SharpDb.EntityFrameworkCore.Interceptors
open SharpDb.Services
open Xunit

module TrackSaveUserInterceptorTests =

    [<AllowNullLiteral>]
    type TestUser() =
        member val Id = 0 with get, set
        member val DisplayName = "" with get, set
        interface IUser with
            member this.GetId() = this.Id :> obj
            member this.GetDisplayName() = this.DisplayName

    type TestEntityC() =
        member val Id = 0 with get, set
        member val Name = "" with get, set
        member val CreatedByUser: TestUser = null with get, set
        interface ITrackUserC with
            member this.CreatedByUser with get() = this.CreatedByUser and set v = this.CreatedByUser <- v :?> TestUser

    type TestEntityU() =
        member val Id = 0 with get, set
        member val Name = "" with get, set
        member val UpdatedByUser: TestUser = null with get, set
        interface ITrackUserU with
            member this.UpdatedByUser with get() = this.UpdatedByUser and set v = this.UpdatedByUser <- v :?> TestUser

    type TestEntityD() =
        member val Id = 0 with get, set
        member val Name = "" with get, set
        member val DeletedByUser: TestUser = null with get, set
        member val DeletedAt = Nullable<DateTime>() with get, set
        interface ITrackUserD with
            member this.DeletedByUser with get() = this.DeletedByUser and set v = this.DeletedByUser <- v :?> TestUser
            member this.DeletedAt with get() = this.DeletedAt and set v = this.DeletedAt <- v

    type TestEntityCU() =
        member val Id = 0 with get, set
        member val Name = "" with get, set
        member val CreatedByUser: TestUser = null with get, set
        member val UpdatedByUser: TestUser = null with get, set
        interface ITrackUserC with
            member this.CreatedByUser with get() = this.CreatedByUser and set v = this.CreatedByUser <- v :?> TestUser
        interface ITrackUserU with
            member this.UpdatedByUser with get() = this.UpdatedByUser and set v = this.UpdatedByUser <- v :?> TestUser

    type TestDbContext(options: DbContextOptions<TestDbContext>) =
        inherit DbContext(options)
        member this.Users : DbSet<TestUser> = this.Set()
        member this.EntitiesC : DbSet<TestEntityC> = this.Set()
        member this.EntitiesU : DbSet<TestEntityU> = this.Set()
        member this.EntitiesD : DbSet<TestEntityD> = this.Set()
        member this.EntitiesCU : DbSet<TestEntityCU> = this.Set()

    type TestUserService(user: IUser option) =
        interface IUserService with
            member this.GetCurrentUser() =
                match user with
                | Some u -> u
                | None -> null
            member this.GetCurrentUserDisplayName() = if user.IsSome then user.Value.GetDisplayName() else null
            member this.GetCurrentUserId() = if user.IsSome then user.Value.GetId() else null

    let user =
        let x = TestUser()
        x.Id <- 1
        x.DisplayName <- "Test User 1"
        x :> IUser
    let otherUser =
        let x = TestUser()
        x.Id <- 2
        x.DisplayName <- "Test User 2"
        x :> IUser

    let createContextCore(user: IUser option) =
        let options = DbContextOptionsBuilder<TestDbContext>()
                        .UseInMemoryDatabase(Guid.NewGuid().ToString())
                        .AddInterceptors(TrackSaveUserInterceptor(TestUserService(user) :> IUserService))
                        .Options
        let ctx = new TestDbContext(options)
        if user.IsSome then ctx.Users.Add(user.Value :?> TestUser) |> ignore
        ctx.Users.Add(otherUser :?> TestUser) |> ignore
        ctx.SaveChanges() |> ignore
        ctx
    let createContext() = createContextCore(Some user)
    let createContextWithoutUser() = createContextCore(None)

    [<Fact>]
    let ``SavingChanges sets CreatedByUser when entity is added`` () =
        use ctx = createContext()

        let entity = TestEntityC(Id = 1, Name = "Test")
        ctx.EntitiesC.Add(entity) |> ignore
        ctx.SaveChanges() |> ignore

        Assert.NotNull((entity :> ITrackUserC).CreatedByUser)
        Assert.Same(user, (entity :> ITrackUserC).CreatedByUser)

    [<Fact>]
    let ``SavingChanges does not override existing CreatedByUser`` () =
        use ctx = createContext()

        let entity = TestEntityC(Id = 1, Name = "Test")
        (entity :> ITrackUserC).CreatedByUser <- user
        ctx.EntitiesC.Add(entity) |> ignore
        ctx.SaveChanges() |> ignore

        Assert.Same(user, (entity :> ITrackUserC).CreatedByUser)

    [<Fact>]
    let ``SavingChanges sets UpdatedByUser when entity is modified`` () =
        use ctx = createContext()

        let entity = TestEntityU(Id = 1, Name = "Test")
        ctx.EntitiesU.Add(entity) |> ignore
        ctx.SaveChanges() |> ignore

        entity.Name <- "Updated"
        ctx.EntitiesU.Update(entity) |> ignore
        ctx.SaveChanges() |> ignore

        Assert.NotNull((entity :> ITrackUserU).UpdatedByUser)
        Assert.Same(user, (entity :> ITrackUserU).UpdatedByUser)

    [<Fact>]
    let ``SavingChanges overrides existing UpdatedByUser`` () =
        use ctx = createContext()

        let entity = TestEntityU(Id = 1, Name = "Test")
        (entity :> ITrackUserU).UpdatedByUser <- otherUser
        ctx.EntitiesU.Add(entity) |> ignore
        ctx.SaveChanges() |> ignore

        Assert.Same(otherUser, (entity :> ITrackUserU).UpdatedByUser)

        entity.Name <- "Updated"
        ctx.EntitiesU.Update(entity) |> ignore
        ctx.SaveChanges() |> ignore

        Assert.Same(user, (entity :> ITrackUserU).UpdatedByUser)

    [<Fact>]
    let ``SavingChanges sets DeletedByUser when entity is deleted`` () =
        use ctx = createContext()

        let entity = TestEntityD(Id = 1, Name = "Test")
        ctx.EntitiesD.Add(entity) |> ignore
        ctx.SaveChanges() |> ignore

        ctx.EntitiesD.Remove(entity) |> ignore
        ctx.SaveChanges() |> ignore

        Assert.NotNull((entity :> ITrackUserD).DeletedByUser)
        Assert.Same(user, (entity :> ITrackUserD).DeletedByUser)

    [<Fact>]
    let ``SavingChanges sets DeletedByUser when entity is marked as deleted`` () =
        use ctx = createContext()

        let entity = TestEntityD(Id = 1, Name = "Test")
        (entity :> ITrackUserD).DeletedAt <- DateTime.Now
        ctx.EntitiesD.Add(entity) |> ignore
        ctx.SaveChanges() |> ignore

        entity.Name <- "Updated"
        ctx.EntitiesD.Update(entity) |> ignore
        ctx.SaveChanges() |> ignore

        Assert.NotNull((entity :> ITrackUserD).DeletedByUser)
        Assert.Same(user, (entity :> ITrackUserD).DeletedByUser)

    [<Fact>]
    let ``SavingChanges does not set user when no user is available`` () =
        use ctx = createContextWithoutUser()

        let entity = TestEntityC(Id = 1, Name = "Test")
        ctx.EntitiesC.Add(entity) |> ignore
        ctx.SaveChanges() |> ignore

        Assert.Null((entity :> ITrackUserC).CreatedByUser)

    [<Fact>]
    let ``SavingChanges handles entity with multiple user tracking interfaces`` () =
        use ctx = createContext()

        let entity = TestEntityCU(Id = 1, Name = "Test")
        ctx.EntitiesCU.Add(entity) |> ignore
        ctx.SaveChanges() |> ignore

        Assert.NotNull((entity :> ITrackUserC).CreatedByUser)
        Assert.Same(user, (entity :> ITrackUserC).CreatedByUser)
        Assert.Null((entity :> ITrackUserU).UpdatedByUser)

    [<Fact>]
    let ``SavingChangesAsync behaves like SavingChanges`` () =
        use ctx = createContext()

        let entity = TestEntityC(Id = 1, Name = "Test")
        ctx.EntitiesC.Add(entity) |> ignore
        ctx.SaveChangesAsync().GetAwaiter().GetResult() |> ignore

        Assert.NotNull((entity :> ITrackUserC).CreatedByUser)
        Assert.Same(user, (entity :> ITrackUserC).CreatedByUser)
