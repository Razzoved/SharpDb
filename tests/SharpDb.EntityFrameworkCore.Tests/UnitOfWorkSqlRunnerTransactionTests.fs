namespace SharpDb.EntityFrameworkCore.Tests

open System
open Microsoft.Data.Sqlite
open Microsoft.EntityFrameworkCore
open Xunit
open SharpDb.EntityFrameworkCore
open System.Reflection

module UnitOfWorkSqlRunnerTransactionTests =

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
    let ``EfcSqlRunner SqlExecuteAsync in UoW transaction commits successfully`` () =
        task {
            use dbContextFactory = new SqliteContextFactory()
            use uow = new DummyUnitOfWork(dbContextFactory)
            let name = "SqlRunnerTest"
            let! result = uow.InTransactionAsync(fun () ->
                task {
                    let r2 = uow.Sql.ExecuteAsync($"""INSERT INTO DummyEntity (Name) VALUES ({name})""").AsTask() |> Async.AwaitTask
                    let r3 = uow.Sql.ManyAsync<DummyEntity>($"""SELECT * FROM DummyEntity WHERE Name = {name}""", fun r -> new DummyEntity()).AsTask() |> Async.AwaitTask
                    ignore r2
                    ignore r3
                }
            )
            Assert.True(result.IsSuccess)
            Assert.Equal(1, uow.PrivateContext.Set<DummyEntity>().CountAsync(fun e -> e.Name = name).Result)
        }

    [<Fact>]
    let ``EfcSqlRunner SqlExecuteAsync in UoW transaction rolls back on exception`` () =
        use dbContextFactory = new SqliteContextFactory()
        use uow = new DummyUnitOfWork(dbContextFactory)
        let name = "SqlRunnerRollback"
        let result = uow.InTransaction(fun () ->
            uow.Sql.ExecuteAsync($"""INSERT INTO DummyEntity (Name) VALUES ({name})""").AsTask().Wait()
            raise (Exception("Rollback"))
        )
        Assert.False(result.IsSuccess)
        Assert.Equal(0, uow.PrivateContext.Set<DummyEntity>().CountAsync(fun e -> e.Name = name).Result)

    [<Fact>]
    let ``Nested UoW transactions with EfcSqlRunner roll back inner only`` () =
        use dbContextFactory = new SqliteContextFactory()
        use uow = new DummyUnitOfWork(dbContextFactory)
        let nameOuter = "OuterSql"
        let nameInner = "InnerSql"
        let result =
            uow.InTransaction(fun () ->
                uow.Sql.ExecuteAsync($"""INSERT INTO DummyEntity (Name) VALUES ({nameOuter})""").AsTask().Wait()
                let innerResult =
                    uow.InTransaction(fun () ->
                        uow.Sql.ExecuteAsync($"""INSERT INTO DummyEntity (Name) VALUES ({nameInner})""").AsTask().Wait()
                        raise (Exception("Inner rollback"))
                    )
                Assert.False(innerResult.IsSuccess)
            )
        Assert.True(result.IsSuccess)
        let countOuter = uow.PrivateContext.Set<DummyEntity>().CountAsync(fun e -> e.Name = nameOuter).Result
        let countInner = uow.PrivateContext.Set<DummyEntity>().CountAsync(fun e -> e.Name = nameInner).Result
        Assert.Equal(1, countOuter)
        Assert.Equal(0, countInner)

    [<Fact>]
    let ``Nested UoW transactions with EfcSqlRunner roll back all on outer exception`` () =
        use dbContextFactory = new SqliteContextFactory()
        use uow = new DummyUnitOfWork(dbContextFactory)
        let nameOuter = "OuterAllSql"
        let nameInner = "InnerAllSql"
        let result =
            uow.InTransaction(fun () ->
                uow.Sql.ExecuteAsync($"""INSERT INTO DummyEntity (Name) VALUES ({nameOuter})""").AsTask().Wait()
                let _ =
                    uow.InTransaction(fun () ->
                        uow.Sql.ExecuteAsync($"""INSERT INTO DummyEntity (Name) VALUES ({nameInner})""").AsTask().Wait()
                    )
                raise (Exception("Outer rollback"))
            )
        Assert.False(result.IsSuccess)
        let countOuter = uow.PrivateContext.Set<DummyEntity>().CountAsync(fun e -> e.Name = nameOuter).Result
        let countInner = uow.PrivateContext.Set<DummyEntity>().CountAsync(fun e -> e.Name = nameInner).Result
        Assert.Equal(0, countOuter)
        Assert.Equal(0, countInner)
