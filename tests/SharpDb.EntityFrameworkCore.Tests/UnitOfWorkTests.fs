namespace SharpDb.EntityFrameworkCore.Tests

open SharpDb.EntityFrameworkCore
open System
open System.Reflection
open System.Threading.Tasks
open Microsoft.Data.Sqlite
open Microsoft.EntityFrameworkCore
open Xunit

module UnitOfWorkTests =

    type DummyEntity() =
        member val Id = 0 with get, set
        member val Name = "" with get, set

    type DummyDbContext(ctx: DbContextOptions<DummyDbContext>) =
        inherit DbContext(ctx)
        [<DefaultValue>] val mutable DummyEntities : DbSet<DummyEntity>
        override _.OnModelCreating(modelBuilder: ModelBuilder) =
            modelBuilder.Entity<DummyEntity>().HasKey("Id") |> ignore
            modelBuilder.Entity<DummyEntity>().Property(fun e -> e.Id).ValueGeneratedOnAdd().HasColumnName("id") |> ignore

    type DummyUnitOfWork(ctxFactory: IDbContextFactory<DummyDbContext>) =
        inherit UnitOfWork<DummyDbContext>(ctxFactory)
        member this.PrivateContext = this.GetContext()

    type InMemoryContextFactory() =
        interface IDbContextFactory<DummyDbContext> with
            member _.CreateDbContext() =
                let options = DbContextOptionsBuilder<DummyDbContext>()
                                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                                .Options
                let ctx = new DummyDbContext(options)
                ctx.Database.EnsureCreated() |> ignore
                ctx

    type SqliteContextFactory() =
        let connection = new SqliteConnection($"Data Source={Guid.NewGuid()};mode=memory;cache=shared;")
        interface System.IDisposable with
            member _.Dispose() =
                if connection.State = System.Data.ConnectionState.Open then
                    connection.Close()
                connection.Dispose()
        interface IDbContextFactory<DummyDbContext> with
            member _.CreateDbContext() =
                if connection.State <> System.Data.ConnectionState.Open then
                    connection.Open()
                let options = DbContextOptionsBuilder<DummyDbContext>()
                                .UseSqlite(connection)
                                .Options
                let ctx = new DummyDbContext(options)
                ctx.Database.EnsureCreated() |> ignore
                ctx

    [<Fact>]
    let ``Attach sets entity state to Unchanged if Detached`` () =
        let dbContextFactory = new InMemoryContextFactory()
        use uow = new DummyUnitOfWork(dbContextFactory)
        let entity = DummyEntity()
        uow.PrivateContext.Add(entity) |> ignore
        uow.PrivateContext.Entry(entity).State <- EntityState.Detached
        uow.Attach(entity)
        Assert.Equal(EntityState.Unchanged, uow.PrivateContext.Entry(entity).State)

    [<Fact>]
    let ``Attach does not change entity state if not Detached`` () =
        let dbContextFactory = new InMemoryContextFactory()
        use uow = new DummyUnitOfWork(dbContextFactory)
        let entity = DummyEntity()
        uow.PrivateContext.Add(entity) |> ignore
        uow.PrivateContext.Entry(entity).State <- EntityState.Modified
        uow.Attach(entity)
        Assert.Equal(EntityState.Modified, uow.PrivateContext.Entry(entity).State)

    [<Fact>]
    let ``Attach adds entity if not tracked`` () =
        let dbContextFactory = new InMemoryContextFactory()
        use uow = new DummyUnitOfWork(dbContextFactory)
        let entity = DummyEntity()
        Assert.Equal(EntityState.Detached, uow.PrivateContext.Entry(entity).State)
        uow.Attach(entity)
        Assert.Equal(EntityState.Unchanged, uow.PrivateContext.Entry(entity).State)

    [<Fact>]
    let ``Detach sets entity state to Detached if not Detached`` () =
        let dbContextFactory = new InMemoryContextFactory()
        use uow = new DummyUnitOfWork(dbContextFactory)
        let entity = DummyEntity()
        uow.PrivateContext.Add(entity) |> ignore
        uow.Detach(entity)
        Assert.Equal(EntityState.Detached, uow.PrivateContext.Entry(entity).State)

    [<Fact>]
    let ``SaveChanges returns affected rows`` () =
        let dbContextFactory = new InMemoryContextFactory()
        use uow = new DummyUnitOfWork(dbContextFactory)
        let entity = DummyEntity()
        uow.PrivateContext.Add(entity) |> ignore
        let affected = uow.SaveChanges()
        Assert.Equal(1, affected)

    [<Fact>]
    let ``SaveChangesAsync returns affected rows`` () =
        let dbContextFactory = new InMemoryContextFactory()
        use uow = new DummyUnitOfWork(dbContextFactory)
        let entity = DummyEntity()
        uow.PrivateContext.Add(entity) |> ignore
        let affected = uow.SaveChangesAsync().Result
        Assert.Equal(1, affected)

    [<Fact>]
    let ``DiscardChanges clears change tracker`` () =
        let dbContextFactory = new InMemoryContextFactory()
        use uow = new DummyUnitOfWork(dbContextFactory)
        let entity = DummyEntity()
        uow.PrivateContext.Add(entity) |> ignore
        uow.DiscardChanges()
        Assert.Empty(uow.PrivateContext.ChangeTracker.Entries())

    [<Fact>]
    let ``InTransaction executes action and returns success`` () =
        use dbContextFactory = new SqliteContextFactory()
        use uow = new DummyUnitOfWork(dbContextFactory)
        let entity = DummyEntity()
        entity.Name <- "Test"
        Assert.False(uow.PrivateContext.Set<DummyEntity>().AnyAsync(fun e -> e.Name = entity.Name) |> Async.AwaitTask |> Async.RunSynchronously)
        let result = uow.InTransaction(fun () ->
            uow.PrivateContext.Add(entity) |> ignore
            uow.SaveChanges() |> ignore
        )
        Assert.True(result.IsSuccess)
        Assert.True(uow.PrivateContext.Set<DummyEntity>().AnyAsync(fun e -> e.Name = entity.Name) |> Async.AwaitTask |> Async.RunSynchronously)

    [<Fact>]
    let ``InTransactionAsync executes action and returns success`` () : Task =
        task {
            use dbContextFactory = new SqliteContextFactory()
            use uow = new DummyUnitOfWork(dbContextFactory)
            let entity = DummyEntity()
            entity.Name <- "Test"
            Assert.False(uow.PrivateContext.Set<DummyEntity>().AnyAsync(fun e -> e.Name = entity.Name) |> Async.AwaitTask |> Async.RunSynchronously)
            let! result = uow.InTransactionAsync(fun () -> task {
                uow.PrivateContext.Add(entity) |> ignore
                uow.SaveChangesAsync() |> ignore
            })
            Assert.True(result.IsSuccess)
            Assert.True(uow.PrivateContext.Set<DummyEntity>().AnyAsync(fun e -> e.Name = entity.Name) |> Async.AwaitTask |> Async.RunSynchronously)
        }

    [<Fact>]
    let ``InTransaction rolls back on exception`` () =
        use dbContextFactory = new SqliteContextFactory()
        use uow = new DummyUnitOfWork(dbContextFactory)
        let entity = DummyEntity()
        entity.Name <- "Test"
        Assert.False(uow.PrivateContext.Set<DummyEntity>().AnyAsync(fun e -> e.Name = entity.Name) |> Async.AwaitTask |> Async.RunSynchronously)
        let result = uow.InTransaction(fun () ->
            uow.PrivateContext.Add(entity) |> ignore
            uow.SaveChanges() |> ignore
            raise (Exception("Test exception"))
        )
        Assert.False(result.IsSuccess)
        Assert.False(uow.PrivateContext.Set<DummyEntity>().AnyAsync(fun e -> e.Name = entity.Name) |> Async.AwaitTask |> Async.RunSynchronously)

    [<Fact>]
    let ``InTransaction rolls back to previous state on exception`` () =
        use dbContextFactory = new SqliteContextFactory()
        use uow = new DummyUnitOfWork(dbContextFactory)
        let entity = DummyEntity()
        entity.Name <- "Test"
        Assert.Empty(uow.PrivateContext.Set<DummyEntity>().Local) |> ignore
        uow.PrivateContext.Add(entity) |> ignore
        Assert.Single(uow.PrivateContext.Set<DummyEntity>().Local) |> ignore
        let result = uow.InTransaction(fun () ->
            Assert.Equal(uow.PrivateContext.Entry(entity).State, EntityState.Added)
            uow.SaveChanges() |> ignore
            Assert.Equal(uow.PrivateContext.Entry(entity).State, EntityState.Unchanged)
            Assert.True(uow.PrivateContext.Set<DummyEntity>().AnyAsync(fun e -> e.Name = "Test") |> Async.AwaitTask |> Async.RunSynchronously)
            raise (Exception("Test exception"))
        )
        Assert.False(result.IsSuccess)
        Assert.False(uow.PrivateContext.Set<DummyEntity>().AnyAsync(fun e -> e.Name = "Test") |> Async.AwaitTask |> Async.RunSynchronously)
        Assert.Single(uow.PrivateContext.Set<DummyEntity>().Local) |> ignore
        Assert.Equal(uow.PrivateContext.Entry(entity).State, EntityState.Added)
        Assert.Equal("Test", entity.Name)

    [<Fact>]
    let ``InTransaction rolls back to previous state and values on exception`` () =
        use dbContextFactory = new SqliteContextFactory()
        use uow = new DummyUnitOfWork(dbContextFactory)
        let entity = DummyEntity()
        entity.Name <- "Test"
        Assert.Empty(uow.PrivateContext.Set<DummyEntity>().Local) |> ignore
        uow.PrivateContext.Add(entity) |> ignore
        Assert.Single(uow.PrivateContext.Set<DummyEntity>().Local) |> ignore
        let result = uow.InTransaction(fun () ->
            Assert.Equal(uow.PrivateContext.Entry(entity).State, EntityState.Added)
            entity.Name <- "Changed1"
            uow.SaveChanges() |> ignore
            Assert.True(uow.PrivateContext.Set<DummyEntity>().AnyAsync(fun e -> e.Name = "Changed1") |> Async.AwaitTask |> Async.RunSynchronously)
            Assert.Equal(uow.PrivateContext.Entry(entity).State, EntityState.Unchanged)
            entity.Name <- "Changed2"
            uow.PrivateContext.Update(entity) |> ignore
            Assert.Equal(uow.PrivateContext.Entry(entity).State, EntityState.Modified)
            uow.SaveChanges() |> ignore
            Assert.Equal(uow.PrivateContext.Entry(entity).State, EntityState.Unchanged)
            Assert.True(uow.PrivateContext.Set<DummyEntity>().AnyAsync(fun e -> e.Name = "Changed2") |> Async.AwaitTask |> Async.RunSynchronously)
            raise (Exception("Test exception"))
        )
        Assert.False(result.IsSuccess)
        Assert.False(uow.PrivateContext.Set<DummyEntity>().AnyAsync(fun e -> e.Name = "Changed2") |> Async.AwaitTask |> Async.RunSynchronously)
        Assert.False(uow.PrivateContext.Set<DummyEntity>().AnyAsync(fun e -> e.Name = "Changed1") |> Async.AwaitTask |> Async.RunSynchronously)
        Assert.False(uow.PrivateContext.Set<DummyEntity>().AnyAsync(fun e -> e.Name = "Test") |> Async.AwaitTask |> Async.RunSynchronously)
        Assert.Single(uow.PrivateContext.Set<DummyEntity>().Local) |> ignore
        Assert.Equal(uow.PrivateContext.Entry(entity).State, EntityState.Added)
        Assert.Equal("Test", entity.Name)


