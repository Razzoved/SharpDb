namespace SharpDb.EntityFrameworkCore.Tests.Interceptors

open System
open Microsoft.EntityFrameworkCore
open SharpDb.EntityFrameworkCore.Entities
open SharpDb.EntityFrameworkCore.Interceptors
open SharpDb.Services
open Xunit

module SoftDeleteInterceptorTests =

    type TestEntity() =
        member val Id = 0 with get, set
        member val Name = "" with get, set
        member val DeletedAt = Nullable<DateTime>() with get, set
        interface ISoftDelete with
            member this.IsDeleted with get() = this.DeletedAt.HasValue
            member this.DeletedAt  with get() = this.DeletedAt and set(v) = this.DeletedAt <- v

    type TestDbContext(options: DbContextOptions<TestDbContext>) =
        inherit DbContext(options)
        member this.Entities : DbSet<TestEntity> = this.Set()

    type FixedDateTimeService(dateTime: DateTime) =
        interface IDateTimeService with
            member _.Now = DateTimeOffset(dateTime)
            member _.Today = DateTimeOffset(dateTime.Date)

    [<Fact>]
    let ``SavingChanges sets DeletedAt when entity is deleted`` () =
        let options = DbContextOptionsBuilder<TestDbContext>()
                        .UseInMemoryDatabase(Guid.NewGuid().ToString())
                        .AddInterceptors(SoftDeleteInterceptor(FixedDateTimeService(DateTime(2023, 6, 15, 10, 30, 0)) :> IDateTimeService))
                        .Options
        use ctx = new TestDbContext(options)
        let fixedDate = DateTime(2023, 6, 15, 10, 30, 0)

        let entity = TestEntity(Id = 1, Name = "Test")
        ctx.Entities.Add(entity) |> ignore
        ctx.SaveChanges() |> ignore

        ctx.Entities.Remove(entity) |> ignore
        ctx.SaveChanges() |> ignore

        Assert.True((entity :> ISoftDelete).DeletedAt.HasValue)
        Assert.Equal(fixedDate, (entity :> ISoftDelete).DeletedAt.Value)

    [<Fact>]
    let ``SavingChanges clears state when entity is deleted`` () =
        let options = DbContextOptionsBuilder<TestDbContext>()
                        .UseInMemoryDatabase(Guid.NewGuid().ToString())
                        .AddInterceptors(SoftDeleteInterceptor(FixedDateTimeService(DateTime(2023, 6, 15, 10, 30, 0)) :> IDateTimeService))
                        .Options
        use ctx = new TestDbContext(options)

        let entity = TestEntity(Id = 1, Name = "Test")
        ctx.Entities.Add(entity) |> ignore
        ctx.SaveChanges() |> ignore

        ctx.Entities.Remove(entity) |> ignore
        Assert.Equal(EntityState.Deleted, ctx.Entry(entity).State)

        ctx.SaveChanges() |> ignore
        Assert.Equal(EntityState.Unchanged, ctx.Entry(entity).State)
        Assert.True((entity :> ISoftDelete).DeletedAt.HasValue)
        Assert.True((entity :> ISoftDelete).IsDeleted)

    [<Fact>]
    let ``SavingChanges does not set DeletedAt if already set`` () =
        let fixedDate = DateTime(2023, 6, 15, 10, 30, 0)
        let options = DbContextOptionsBuilder<TestDbContext>()
                        .UseInMemoryDatabase(Guid.NewGuid().ToString())
                        .AddInterceptors(SoftDeleteInterceptor(FixedDateTimeService(fixedDate) :> IDateTimeService))
                        .Options
        use ctx = new TestDbContext(options)

        let entity = TestEntity(Id = 1, Name = "Test")
        (entity :> ISoftDelete).DeletedAt <- DateTime(2020, 1, 1) |> Nullable
        ctx.Entities.Add(entity) |> ignore
        ctx.SaveChanges() |> ignore

        ctx.Entities.Remove(entity) |> ignore
        ctx.SaveChanges() |> ignore

        Assert.Equal(DateTime(2020, 1, 1), (entity :> ISoftDelete).DeletedAt.Value)

    [<Fact>]
    let ``SavingChanges resets DeletedAt to null when cleared during update`` () =
        let options = DbContextOptionsBuilder<TestDbContext>()
                        .UseInMemoryDatabase(Guid.NewGuid().ToString())
                        .AddInterceptors(SoftDeleteInterceptor(FixedDateTimeService(DateTime(2023, 6, 15, 10, 30, 0)) :> IDateTimeService))
                        .Options
        use ctx = new TestDbContext(options)

        let entity = TestEntity(Id = 1, Name = "Test")
        (entity :> ISoftDelete).DeletedAt <- DateTime(2020, 1, 1) |> Nullable
        ctx.Entities.Add(entity) |> ignore
        ctx.SaveChanges() |> ignore

        (entity :> ISoftDelete).DeletedAt <- Nullable()
        ctx.Entities.Update(entity) |> ignore
        ctx.SaveChanges() |> ignore

        Assert.Null((entity :> ISoftDelete).DeletedAt)

    [<Fact>]
    let ``SavingChangesAsync behaves like SavingChanges`` () =
        let options = DbContextOptionsBuilder<TestDbContext>()
                        .UseInMemoryDatabase(Guid.NewGuid().ToString())
                        .AddInterceptors(SoftDeleteInterceptor(FixedDateTimeService(DateTime(2023, 6, 15, 10, 30, 0)) :> IDateTimeService))
                        .Options
        use ctx = new TestDbContext(options)
        let fixedDate = DateTime(2023, 6, 15, 10, 30, 0)

        let entity = TestEntity(Id = 1, Name = "Test")
        ctx.Entities.Add(entity) |> ignore
        ctx.SaveChanges() |> ignore

        ctx.Entities.Remove(entity) |> ignore
        ctx.SaveChangesAsync().GetAwaiter().GetResult() |> ignore

        Assert.True((entity :> ISoftDelete).DeletedAt.HasValue)
        Assert.Equal(fixedDate, (entity :> ISoftDelete).DeletedAt.Value)
