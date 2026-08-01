namespace SharpDb.EntityFrameworkCore.Tests.Interceptors

open System
open Microsoft.EntityFrameworkCore
open SharpDb.EntityFrameworkCore.Entities
open SharpDb.EntityFrameworkCore.Interceptors
open SharpDb.Services
open Xunit

module TrackSaveTimeInterceptorTests =

    type TestEntity() =
        member val Id = 0 with get, set
        member val Name = "" with get, set
        member val CreatedAt = DateTime.MinValue with get, set
        member val UpdatedAt = Nullable<DateTime>() with get, set
        interface ITrackSaveTime with
            member this.CreatedAt with get() = this.CreatedAt and set(v) = this.CreatedAt <- v
            member this.UpdatedAt with get() = this.UpdatedAt and set(v) = this.UpdatedAt <- v

    type TestDbContext(options: DbContextOptions<TestDbContext>) =
        inherit DbContext(options)
        member this.Entities : DbSet<TestEntity> = this.Set()

    type FixedDateTimeService(dateTime: DateTime) =
        interface IDateTimeService with
            member _.Now = DateTimeOffset(dateTime)
            member _.Today = DateTimeOffset(dateTime.Date)

    [<Fact>]
    let ``SavingChanges sets CreatedAt when entity is added`` () =
        let fixedDate = DateTime(2023, 6, 15, 10, 30, 0)
        let options = DbContextOptionsBuilder<TestDbContext>()
                        .UseInMemoryDatabase(Guid.NewGuid().ToString())
                        .AddInterceptors(TrackSaveTimeInterceptor(FixedDateTimeService(fixedDate) :> IDateTimeService))
                        .Options
        use ctx = new TestDbContext(options)

        let entity = TestEntity(Id = 1, Name = "Test")
        ctx.Entities.Add(entity) |> ignore
        ctx.SaveChanges() |> ignore

        Assert.Equal(fixedDate, (entity :> ITrackSaveTime).CreatedAt)

    [<Fact>]
    let ``SavingChanges sets UpdatedAt when entity is modified`` () =
        let fixedDate = DateTime(2023, 6, 15, 10, 30, 0)
        let options = DbContextOptionsBuilder<TestDbContext>()
                        .UseInMemoryDatabase(Guid.NewGuid().ToString())
                        .AddInterceptors(TrackSaveTimeInterceptor(FixedDateTimeService(fixedDate) :> IDateTimeService))
                        .Options
        use ctx = new TestDbContext(options)

        let entity = TestEntity(Id = 1, Name = "Test")
        ctx.Entities.Add(entity) |> ignore
        ctx.SaveChanges() |> ignore

        entity.Name <- "Updated"
        ctx.Entities.Update(entity) |> ignore
        ctx.SaveChanges() |> ignore

        Assert.True((entity :> ITrackSaveTime).UpdatedAt.HasValue)
        Assert.Equal(fixedDate, (entity :> ITrackSaveTime).UpdatedAt.Value)

    [<Fact>]
    let ``SavingChanges does not modify CreatedAt on update`` () =
        let fixedDate = DateTime(2023, 1, 1, 10, 30, 0)
        let options = DbContextOptionsBuilder<TestDbContext>()
                        .UseInMemoryDatabase(Guid.NewGuid().ToString())
                        .AddInterceptors(TrackSaveTimeInterceptor(FixedDateTimeService(fixedDate) :> IDateTimeService))
                        .Options
        use ctx = new TestDbContext(options)

        let entity = TestEntity(Id = 1, Name = "Test")
        ctx.Entities.Add(entity) |> ignore
        ctx.SaveChanges() |> ignore

        let originalCreatedAt = (entity :> ITrackSaveTime).CreatedAt
        let originalUpdatedAt = (entity :> ITrackSaveTime).UpdatedAt

        entity.Name <- "Updated"
        ctx.Entities.Update(entity) |> ignore
        ctx.SaveChanges() |> ignore

        Assert.Equal(originalCreatedAt, (entity :> ITrackSaveTime).CreatedAt)
        Assert.NotEqual(originalUpdatedAt, (entity :> ITrackSaveTime).UpdatedAt)

    [<Fact>]
    let ``SavingChanges does not modify UpdatedAt on add`` () =
        let fixedDate = DateTime(2023, 6, 15, 10, 30, 0)
        let options = DbContextOptionsBuilder<TestDbContext>()
                        .UseInMemoryDatabase(Guid.NewGuid().ToString())
                        .AddInterceptors(TrackSaveTimeInterceptor(FixedDateTimeService(fixedDate) :> IDateTimeService))
                        .Options
        use ctx = new TestDbContext(options)

        let entity = TestEntity(Id = 1, Name = "Test")
        ctx.Entities.Add(entity) |> ignore
        ctx.SaveChanges() |> ignore

        Assert.False((entity :> ITrackSaveTime).UpdatedAt.HasValue)

    [<Fact>]
    let ``SavingChangesAsync behaves like SavingChanges`` () =
        let fixedDate = DateTime(2023, 6, 15, 10, 30, 0)
        let options = DbContextOptionsBuilder<TestDbContext>()
                        .UseInMemoryDatabase(Guid.NewGuid().ToString())
                        .AddInterceptors(TrackSaveTimeInterceptor(FixedDateTimeService(fixedDate) :> IDateTimeService))
                        .Options
        use ctx = new TestDbContext(options)

        let entity = TestEntity(Id = 1, Name = "Test")
        ctx.Entities.Add(entity) |> ignore
        ctx.SaveChangesAsync().GetAwaiter().GetResult() |> ignore

        Assert.Equal(fixedDate, (entity :> ITrackSaveTime).CreatedAt)
