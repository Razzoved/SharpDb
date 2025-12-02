namespace SharpDb.EntityFrameworkCore.Tests

open SharpDb.EntityFrameworkCore
open System
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

    type DummyUnitOfWork(ctx: DbContext) =
        inherit UnitOfWork(ctx)


    let createContext() =
        let options = DbContextOptionsBuilder<DummyDbContext>()
                        .UseInMemoryDatabase(Guid.NewGuid().ToString())
                        .Options
        let ctx = new DummyDbContext(options)
        ctx.Database.EnsureCreated() |> ignore
        ctx

    let createContextSqlite() =
        let conn = new SqliteConnection($"Data Source={Guid.NewGuid()};mode=memory;cache=shared;")
        conn.Open()
        let options = DbContextOptionsBuilder<DummyDbContext>()
                        .UseSqlite(conn)
                        .Options
        let ctx = new DummyDbContext(options)
        ctx.Database.EnsureCreated() |> ignore
        ctx, conn

    [<Fact>]
    let ``Attach sets entity state to Unchanged if Detached`` () =
        use ctx = createContext()
        let uow = DummyUnitOfWork(ctx)
        let entity = DummyEntity()
        ctx.Add(entity) |> ignore
        ctx.Entry(entity).State <- EntityState.Detached
        uow.Attach(entity)
        Assert.Equal(EntityState.Unchanged, ctx.Entry(entity).State)

    [<Fact>]
    let ``Attach does not change entity state if not Detached`` () =
        use ctx = createContext()
        let uow = DummyUnitOfWork(ctx)
        let entity = DummyEntity()
        ctx.Add(entity) |> ignore
        ctx.Entry(entity).State <- EntityState.Modified
        uow.Attach(entity)
        Assert.Equal(EntityState.Modified, ctx.Entry(entity).State)

    [<Fact>]
    let ``Attach adds entity if not tracked`` () =
        use ctx = createContext()
        let uow = DummyUnitOfWork(ctx)
        let entity = DummyEntity()
        Assert.Equal(EntityState.Detached, ctx.Entry(entity).State)
        uow.Attach(entity)
        Assert.Equal(EntityState.Unchanged, ctx.Entry(entity).State)

    [<Fact>]
    let ``Detach sets entity state to Detached if not Detached`` () =
        use ctx = createContext()
        let uow = DummyUnitOfWork(ctx)
        let entity = DummyEntity()
        ctx.Add(entity) |> ignore
        uow.Detach(entity)
        Assert.Equal(EntityState.Detached, ctx.Entry(entity).State)

    [<Fact>]
    let ``SaveChanges returns affected rows`` () =
        use ctx = createContext()
        let uow = DummyUnitOfWork(ctx)
        let entity = DummyEntity()
        ctx.Add(entity) |> ignore
        let affected = uow.SaveChanges()
        Assert.Equal(1, affected)

    [<Fact>]
    let ``SaveChangesAsync returns affected rows`` () =
        use ctx = createContext()
        let uow = DummyUnitOfWork(ctx)
        let entity = DummyEntity()
        ctx.Add(entity) |> ignore
        let affected = uow.SaveChangesAsync().Result
        Assert.Equal(1, affected)

    [<Fact>]
    let ``DiscardChanges clears change tracker`` () =
        use ctx = createContext()
        let uow = DummyUnitOfWork(ctx)
        let entity = DummyEntity()
        ctx.Add(entity) |> ignore
        uow.DiscardChanges()
        Assert.Empty(ctx.ChangeTracker.Entries())

    [<Fact>]
    let ``InTransaction executes action and returns success`` () =
        let ctx, conn = createContextSqlite()
        try
            let uow = DummyUnitOfWork(ctx)
            let entity = DummyEntity()
            entity.Name <- "Test"
            Assert.False(ctx.Set<DummyEntity>().AnyAsync(fun e -> e.Name = entity.Name) |> Async.AwaitTask |> Async.RunSynchronously)
            let result = uow.InTransaction(fun () ->
                ctx.Add(entity) |> ignore
                uow.SaveChanges() |> ignore
            )
            Assert.True(result.IsSuccess)
            Assert.True(ctx.Set<DummyEntity>().AnyAsync(fun e -> e.Name = entity.Name) |> Async.AwaitTask |> Async.RunSynchronously)
        finally
            ctx.Dispose()
            conn.Close()
            conn.Dispose()

    [<Fact>]
    let ``InTransactionAsync executes action and returns success`` () : Task =
        let ctx, conn = createContextSqlite()
        try
            let uow = DummyUnitOfWork(ctx)
            let entity = DummyEntity()
            entity.Name <- "Test"
            Assert.False(ctx.Set<DummyEntity>().AnyAsync(fun e -> e.Name = entity.Name) |> Async.AwaitTask |> Async.RunSynchronously)
            task {
                let! result = uow.InTransactionAsync(fun () -> task {
                    ctx.Add(entity) |> ignore
                    uow.SaveChangesAsync() |> ignore
                })
                Assert.True(result.IsSuccess)
                Assert.True(ctx.Set<DummyEntity>().AnyAsync(fun e -> e.Name = entity.Name) |> Async.AwaitTask |> Async.RunSynchronously)
            }
        finally
            ctx.Dispose()
            conn.Close()
            conn.Dispose()

    [<Fact>]
    let ``InTransaction rolls back on exception`` () =
        let ctx, conn = createContextSqlite()
        try
            let uow = DummyUnitOfWork(ctx)
            let entity = DummyEntity()
            entity.Name <- "Test"
            Assert.False(ctx.Set<DummyEntity>().AnyAsync(fun e -> e.Name = entity.Name) |> Async.AwaitTask |> Async.RunSynchronously)
            let result = uow.InTransaction(fun () ->
                ctx.Add(entity) |> ignore
                uow.SaveChanges() |> ignore
                raise (Exception("Test exception"))
            )
            Assert.False(result.IsSuccess)
            Assert.False(ctx.Set<DummyEntity>().AnyAsync(fun e -> e.Name = entity.Name) |> Async.AwaitTask |> Async.RunSynchronously)
        finally
            ctx.Dispose()
            conn.Close()
            conn.Dispose()

    [<Fact>]
    let ``InTransaction rolls back to previous state on exception`` () =
        let ctx, conn = createContextSqlite()
        try
            let uow = DummyUnitOfWork(ctx)
            let entity = DummyEntity()
            entity.Name <- "Test"
            Assert.Empty(ctx.Set<DummyEntity>().Local) |> ignore
            ctx.Add(entity) |> ignore
            Assert.Single(ctx.Set<DummyEntity>().Local) |> ignore
            let result = uow.InTransaction(fun () ->
                Assert.Equal(ctx.Entry(entity).State, EntityState.Added)
                uow.SaveChanges() |> ignore
                Assert.Equal(ctx.Entry(entity).State, EntityState.Unchanged)
                Assert.True(ctx.Set<DummyEntity>().AnyAsync(fun e -> e.Name = "Test") |> Async.AwaitTask |> Async.RunSynchronously)
                raise (Exception("Test exception"))
            )
            Assert.False(result.IsSuccess)
            Assert.False(ctx.Set<DummyEntity>().AnyAsync(fun e -> e.Name = "Test") |> Async.AwaitTask |> Async.RunSynchronously)
            Assert.Single(ctx.Set<DummyEntity>().Local) |> ignore
            Assert.Equal(ctx.Entry(entity).State, EntityState.Added)
            Assert.Equal("Test", entity.Name)
        finally
            ctx.Dispose()
            conn.Close()
            conn.Dispose()

    [<Fact>]
    let ``InTransaction rolls back to previous state and values on exception`` () =
        let ctx, conn = createContextSqlite()
        try
            let uow = DummyUnitOfWork(ctx)
            let entity = DummyEntity()
            entity.Name <- "Test"
            Assert.Empty(ctx.Set<DummyEntity>().Local) |> ignore
            ctx.Add(entity) |> ignore
            Assert.Single(ctx.Set<DummyEntity>().Local) |> ignore
            let result = uow.InTransaction(fun () ->
                Assert.Equal(ctx.Entry(entity).State, EntityState.Added)
                entity.Name <- "Changed1"
                uow.SaveChanges() |> ignore
                Assert.True(ctx.Set<DummyEntity>().AnyAsync(fun e -> e.Name = "Changed1") |> Async.AwaitTask |> Async.RunSynchronously)
                Assert.Equal(ctx.Entry(entity).State, EntityState.Unchanged)
                entity.Name <- "Changed2"
                ctx.Update(entity) |> ignore
                Assert.Equal(ctx.Entry(entity).State, EntityState.Modified)
                uow.SaveChanges() |> ignore
                Assert.Equal(ctx.Entry(entity).State, EntityState.Unchanged)
                Assert.True(ctx.Set<DummyEntity>().AnyAsync(fun e -> e.Name = "Changed2") |> Async.AwaitTask |> Async.RunSynchronously)
                raise (Exception("Test exception"))
            )
            Assert.False(result.IsSuccess)
            Assert.False(ctx.Set<DummyEntity>().AnyAsync(fun e -> e.Name = "Changed2") |> Async.AwaitTask |> Async.RunSynchronously)
            Assert.False(ctx.Set<DummyEntity>().AnyAsync(fun e -> e.Name = "Changed1") |> Async.AwaitTask |> Async.RunSynchronously)
            Assert.False(ctx.Set<DummyEntity>().AnyAsync(fun e -> e.Name = "Test") |> Async.AwaitTask |> Async.RunSynchronously)
            Assert.Single(ctx.Set<DummyEntity>().Local) |> ignore
            Assert.Equal(ctx.Entry(entity).State, EntityState.Added)
            Assert.Equal("Test", entity.Name)
        finally
            ctx.Dispose()
            conn.Close()
            conn.Dispose()


