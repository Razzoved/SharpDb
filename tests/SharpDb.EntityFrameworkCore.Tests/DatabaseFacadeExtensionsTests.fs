namespace SharpDb.EntityFrameworkCore.Tests

open System
open Microsoft.EntityFrameworkCore
open Xunit
open SharpDb
open SharpDb.EntityFrameworkCore
open System.Runtime.CompilerServices
open Microsoft.Data.Sqlite

module DatabaseFacadeExtensionsTests =

    type DummyDbContext(options: DbContextOptions<DummyDbContext>) =
        inherit DbContext(options)

    let createContext () =
        let options = DbContextOptionsBuilder<DummyDbContext>()
                        .UseInMemoryDatabase(Guid.NewGuid().ToString())
                        .Options
        new DummyDbContext(options)

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
    let ``GetSqlCommandText returns correct SQL with parameters`` () =
        let sql = FormattableStringFactory.Create("SELECT * FROM Test WHERE Id = {0} AND Name = {1}", [| box 42; box "foo" |])
        let text = DatabaseFacadeExtensions.GetSqlCommandText(sql)
        Assert.Equal("SELECT * FROM Test WHERE Id = @p0 AND Name = @p1", text)

    [<Fact>]
    let ``GetSqlCommandText returns SQL without parameters`` () =
        let sql = FormattableStringFactory.Create("SELECT 1", [||])
        let text = DatabaseFacadeExtensions.GetSqlCommandText(sql)
        Assert.Equal("SELECT 1", text)

    [<Fact>]
    let ``GetSqlCommandParameters returns correct parameters`` () =
        let sql = FormattableStringFactory.Create("SELECT * FROM Test WHERE Id = {0} AND Name = {1}", [| box 42; box "foo" |])
        let parameters = DatabaseFacadeExtensions.GetSqlCommandParameters(sql)
        Assert.Equal(2, parameters.Length)
        Assert.Equal("p0", parameters[0].Name)
        Assert.Equal(42, parameters[0].Value :?> int)
        Assert.Equal("p1", parameters[1].Name)
        Assert.Equal("foo", parameters[1].Value :?> string)

    [<Fact>]
    let ``GetSqlCommandParameters returns empty for no parameters`` () =
        let sql = FormattableStringFactory.Create("SELECT 1", [||])
        let parameters = DatabaseFacadeExtensions.GetSqlCommandParameters(sql)
        Assert.Empty(parameters)

    [<Fact>]
    let ``RawSqlSingleAsync returns failure when no rows`` () =
        let ctx, conn = createContextSqlite()
        try
            let db = ctx.Database
            let task =
                db.RawSqlSingleAsync<int>("SELECT 1 WHERE 1 = 0", (fun _ -> 1))
            let result = task.Result
            Assert.False(result.IsSuccess)
            Assert.IsType(typedefof<StringDbError>, result.Error)
        finally
            ctx.Dispose()
            conn.Close()
            conn.Dispose()

    [<Fact>]
    let ``RawSqlSingleAsync returns failure when more than one row`` () =
        let ctx, conn = createContextSqlite()
        try
            let db = ctx.Database
            let task =
                db.RawSqlSingleAsync<int>("SELECT 1 UNION SELECT 2", (fun r -> r.GetInt32(0)))
            let result = task.Result
            Assert.False(result.IsSuccess)
            Assert.IsType(typedefof<StringDbError>, result.Error)
        finally
            ctx.Dispose()
            conn.Close()
            conn.Dispose()

    [<Fact>]
    let ``RawSqlSingleAsync returns correct value for single row`` () =
        let ctx, conn = createContextSqlite()
        try
            let db = ctx.Database
            let task =
                db.RawSqlSingleAsync<int>("SELECT 42", (fun r -> r.GetInt32(0)))
            let result = task.Result
            Assert.True(result.IsSuccess)
            Assert.Equal(42, result.Data)
        finally
            ctx.Dispose()
            conn.Close()
            conn.Dispose()

    [<Fact>]
    let ``RawSqlManyAsync returns empty array when no rows`` () =
        let ctx, conn = createContextSqlite()
        try
            let db = ctx.Database
            let task =
                db.RawSqlManyAsync<int>("SELECT 1 WHERE 1 = 0", (fun _ -> 1))
            let result = task.Result
            Assert.True(result.IsSuccess)
            Assert.Empty(result.Data)
        finally
            ctx.Dispose()
            conn.Close()
            conn.Dispose()

    [<Fact>]
    let ``RawSqlManyAsync returns correct values for multiple rows`` () =
        let ctx, conn = createContextSqlite()
        try
            let db = ctx.Database
            let task =
                db.RawSqlManyAsync<int>("SELECT 1 UNION SELECT 2 UNION SELECT 3", (fun r -> r.GetInt32(0)))
            let result = task.Result
            Assert.True(result.IsSuccess)
            Assert.Equal([|1;2;3|], result.Data)
        finally
            ctx.Dispose()
            conn.Close()
            conn.Dispose()

    [<Fact>]
    let ``RawSqlExecuteAsync returns success and affected rows`` () =
        let ctx, conn = createContextSqlite()
        try
            let db = ctx.Database
            let task =
                db.RawSqlExecuteAsync("CREATE TABLE IF NOT EXISTS Test(Id INT)", [||])
            let result = task.Result
            Assert.True(result.IsSuccess)
            Assert.True(result.AffectedRows >= 0)
        finally
            ctx.Dispose()
            conn.Close()
            conn.Dispose()

    [<Fact>]
    let ``RawSqlExecuteAsync returns success for insert`` () =
        let ctx, conn = createContextSqlite()
        try
            let db = ctx.Database
            let _ = db.RawSqlExecuteAsync("CREATE TABLE IF NOT EXISTS Test(Id INT)", [||]).Result
            let task = db.RawSqlExecuteAsync("INSERT INTO Test(Id) VALUES (123)", [||])
            let result = task.Result
            Assert.True(result.IsSuccess)
            Assert.True(result.AffectedRows = 1)
        finally
            ctx.Dispose()
            conn.Close()
            conn.Dispose()

    [<Fact>]
    let ``SqlSingleAsync returns correct value for single row`` () =
        let ctx, conn = createContextSqlite()
        try
            let db = ctx.Database
            let sql = FormattableStringFactory.Create("SELECT 42", [||])
            let task = db.SqlSingleAsync(sql, fun r -> r.GetInt32(0))
            let result = task.Result
            Assert.True(result.IsSuccess)
            Assert.Equal(42, result.Data)
        finally
            ctx.Dispose()
            conn.Close()
            conn.Dispose()

    [<Fact>]
    let ``SqlManyAsync returns correct values for multiple rows`` () =
        let ctx, conn = createContextSqlite()
        try
            let db = ctx.Database
            let sql = FormattableStringFactory.Create("SELECT 1 UNION SELECT 2 UNION SELECT 3", [||])
            let task = db.SqlManyAsync(sql, fun r -> r.GetInt32(0))
            let result = task.Result
            Assert.True(result.IsSuccess)
            Assert.Equal([|1;2;3|], result.Data)
        finally
            ctx.Dispose()
            conn.Close()
            conn.Dispose()

    [<Fact>]
    let ``SqlExecuteAsync returns success for insert`` () =
        let ctx, conn = createContextSqlite()
        try
            let db = ctx.Database
            let createTable = db.SqlExecuteAsync(FormattableStringFactory.Create("CREATE TABLE IF NOT EXISTS Test(Id INT)", [||]))
            let _ = createTable.Result
            let insert = db.SqlExecuteAsync(FormattableStringFactory.Create("INSERT INTO Test(Id) VALUES ({0})", [| box 99 |]))
            let result = insert.Result
            Assert.True(result.IsSuccess)
            Assert.True(result.AffectedRows = 1)
        finally
            ctx.Dispose()
            conn.Close()
            conn.Dispose()

    [<Fact>]
    let ``StoredProcedureExecuteAsync returns failure for non-existent procedure`` () =
        let ctx, conn = createContextSqlite()
        try
            let db = ctx.Database
            let task = db.StoredProcedureExecuteAsync("NonExistentProcedure", [||])
            let result = task.Result
            Assert.False(result.IsSuccess)
            Assert.IsType(typedefof<ExceptionDbError>, result.Error)
        finally
            ctx.Dispose()
            conn.Close()
            conn.Dispose()

    [<Fact>]
    let ``SqlSingleAsync returns failure for more than one row`` () =
        let ctx, conn = createContextSqlite()
        try
            let db = ctx.Database
            let sql = FormattableStringFactory.Create("SELECT 1 UNION SELECT 2", [||])
            let task = db.SqlSingleAsync(sql, fun r -> r.GetInt32(0))
            let result = task.Result
            Assert.False(result.IsSuccess)
            Assert.IsType(typedefof<StringDbError>, result.Error)
        finally
            ctx.Dispose()
            conn.Close()
            conn.Dispose()

    [<Fact>]
    let ``SqlSingleAsync returns failure for no rows`` () =
        let ctx, conn = createContextSqlite()
        try
            let db = ctx.Database
            let sql = FormattableStringFactory.Create("SELECT 1 WHERE 1 = 0", [||])
            let task = db.SqlSingleAsync(sql, fun r -> r.GetInt32(0))
            let result = task.Result
            Assert.False(result.IsSuccess)
            Assert.IsType(typedefof<StringDbError>, result.Error)
        finally
            ctx.Dispose()
            conn.Close()
            conn.Dispose()
