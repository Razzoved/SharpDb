namespace SharpDb.EntityFrameworkCore.Tests

open Microsoft.Data.Sqlite
open Microsoft.Data.SqlClient
open Microsoft.EntityFrameworkCore
open SharpDb
open SharpDb.EntityFrameworkCore
open System
open System.Reflection
open System.Threading.Tasks
open Xunit

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
        member this.PrivateContext = this.DbContext

    type SqliteContextFactory(?g: Guid) =
        let guid = if g.IsSome then g.Value else Guid.NewGuid()
        let connection = new SqliteConnection($"Data Source={guid};mode=memory;cache=shared;")
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

    type MssqlContextFactory(?dbName: string) =
        let name = defaultArg dbName ("SharpDbTest_" + Guid.NewGuid().ToString("N"))
        let connStr = $"Server=(localdb)\\mssqllocaldb;Database={name};Trusted_Connection=True;MultipleActiveResultSets=True"
        let mutable created = false
        interface System.IDisposable with
            member _.Dispose() =
                use conn = new SqlConnection("Server=(localdb)\\mssqllocaldb;Trusted_Connection=True")
                conn.Open()
                use cmd = conn.CreateCommand()
                cmd.CommandText <- $"IF DB_ID('{name}') IS NOT NULL BEGIN ALTER DATABASE [{name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{name}] END"
                cmd.ExecuteNonQuery() |> ignore
        interface IDbContextFactory<DummyDbContext> with
            member _.CreateDbContext() =
                let options = DbContextOptionsBuilder<DummyDbContext>().UseSqlServer(connStr, fun o -> o.EnableRetryOnFailure() |> ignore).EnableDetailedErrors().Options
                let ctx = new DummyDbContext(options)
                if not created then
                    ctx.Database.EnsureCreated() |> ignore
                    created <- true
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
                    return ActionState.Complete()
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
    let ``EfcSqlRunner SqlExecuteAsync in UoW transaction rolls back with SQL-side rollback`` () =
        use dbContextFactory = new SqliteContextFactory()
        use uow = new DummyUnitOfWork(dbContextFactory)
        let name = "SqlRunnerRollback"
        let result = uow.InTransaction(fun () ->
            uow.Sql.ExecuteAsync($"""INSERT INTO DummyEntity (Name) VALUES ({name})""").AsTask().Wait()
            uow.InTransaction(fun () ->
                let rollbackResult = uow.Sql.RawExecuteAsync("ROLLBACK").AsTask() |> Async.AwaitTask |> Async.RunSynchronously
                Assert.True(rollbackResult.IsSuccess)
                raise (Exception("This is a custom error: Rolling back now"))
            ) |> ignore
            Assert.Fail()
        )
        Assert.False(result.IsSuccess)
        Assert.Equal("This is a custom error: Rolling back now", result.Error.Message)
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

    [<Fact>]
    [<Trait("Category", "LocalDB")>]
    let ``SqlRunner commands should retry and succeed on deadlock`` () =
        use dbContextFactory = new MssqlContextFactory()

        // Seed the shared database via a throw-away context
        use seedCtx = (dbContextFactory :> IDbContextFactory<DummyDbContext>).CreateDbContext()
        seedCtx.Database.ExecuteSqlRaw("DELETE FROM DummyEntity") |> ignore
        seedCtx.Database.ExecuteSqlRaw("SET IDENTITY_INSERT DummyEntity ON; INSERT INTO DummyEntity (Id, Name) VALUES (1, 'TestRowA'); INSERT INTO DummyEntity (Id, Name) VALUES (2, 'TestRowB'); SET IDENTITY_INSERT DummyEntity OFF") |> ignore
        seedCtx.Dispose()

        // Both UoWs target the same database so EF transactions will contend
        use uow1 = new DummyUnitOfWork(dbContextFactory)
        use uow2 = new DummyUnitOfWork(dbContextFactory)

        let started1 = TaskCompletionSource<unit>(TaskCreationOptions.RunContinuationsAsynchronously)
        let started2 = TaskCompletionSource<unit>(TaskCreationOptions.RunContinuationsAsynchronously)
        let valueTask1 = uow1.InTransactionAsync(fun () -> task {
            started1.TrySetResult(()) |> ignore
            do! started2.Task
            let! t1 = uow1.Sql.RawExecuteAsync("UPDATE DummyEntity SET Name = 'Locked1' WHERE Id = 1")
            if not t1.IsSuccess then
                return ActionState.Abort(t1.Error)
            else
                do! Task.Delay(1000)
                let! t2 = uow1.Sql.RawExecuteAsync("UPDATE DummyEntity SET Name = 'Locked1' WHERE Id = 2")
                if not t2.IsSuccess then
                    return ActionState.Abort(t2.Error)
                else
                    return ActionState.Complete()
        })
        let valueTask2 = uow2.InTransactionAsync(fun () -> task {
            started2.TrySetResult(()) |> ignore
            do! started1.Task
            let! t1 = uow2.Sql.RawExecuteAsync("UPDATE DummyEntity SET Name = 'Locked2' WHERE Id = 2")
            if not t1.IsSuccess then
                return ActionState.Abort(t1.Error)
            else
                do! Task.Delay(1000)
                let! t2 = uow2.Sql.RawExecuteAsync("UPDATE DummyEntity SET Name = 'Locked2' WHERE Id = 1")
                if not t2.IsSuccess then
                    return ActionState.Abort(t2.Error)
                else
                    return ActionState.Complete()
        })
        let task1 = valueTask1.AsTask()
        let task2 = valueTask2.AsTask()
        let results = Task.WhenAll([|task1; task2|]) |> Async.AwaitTask |> Async.RunSynchronously

        let res1, res2 = results[0], results[1]
        let err1 = if res1.IsSuccess then "" else res1.Error.Message
        let err2 = if res2.IsSuccess then "" else res2.Error.Message

        Assert.True(res1.IsSuccess && res2.IsSuccess, $"Both must complete, albeit with retry: Task1: {err1}, Task2: {err2}")

        use verifyCtx = (dbContextFactory :> IDbContextFactory<DummyDbContext>).CreateDbContext()
        let checkFor1 = verifyCtx.Set<DummyEntity>().CountAsync(fun e -> e.Name = "Locked1").Result
        let checkFor2 = verifyCtx.Set<DummyEntity>().CountAsync(fun e -> e.Name = "Locked2").Result
        if checkFor1 = 0 then
            Assert.Equal(2, checkFor2) // task1 was retried; task2 updated both rows
        else
            Assert.Equal(0, checkFor2)
            Assert.Equal(2, checkFor1) // task2 was retried; task1 updated both rows
