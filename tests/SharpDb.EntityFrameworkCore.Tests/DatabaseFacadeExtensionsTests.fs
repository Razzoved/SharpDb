namespace SharpDb.EntityFrameworkCore.Tests

open System
open Microsoft.EntityFrameworkCore
open Xunit
open SharpDb
open SharpDb.EntityFrameworkCore
open System.Runtime.CompilerServices
open Microsoft.Data.Sqlite
open System.Threading
open System.Threading.Tasks

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
    let ``RawSqlFirstOrDefaultAsync returns null when no rows`` () =
        let ctx, conn = createContextSqlite()
        try
            let db = ctx.Database
            let task =
                db.RawSqlFirstOrDefaultAsync<Nullable<int>>("SELECT 1 WHERE 1 = 0", (fun _ -> 1 |> Nullable))
            let result = task.Result
            Assert.True(result.IsSuccess)
            Assert.Null(result.Data);
        finally
            ctx.Dispose()
            conn.Close()
            conn.Dispose()

    [<Fact>]
    let ``RawSqlFirstOrDefaultAsync returns correct value for one row`` () =
        let ctx, conn = createContextSqlite()
        try
            let db = ctx.Database
            let task =
                db.RawSqlFirstOrDefaultAsync<int>("SELECT 42", (fun r -> r.GetInt32(0)))
            let result = task.Result
            Assert.True(result.IsSuccess)
            Assert.Equal(42, result.Data)
        finally
            ctx.Dispose()
            conn.Close()
            conn.Dispose()

    [<Fact>]
    let ``RawSqlFirstOrDefaultAsync returns correct value for multiple rows`` () =
        let ctx, conn = createContextSqlite()
        try
            let db = ctx.Database
            let task =
                db.RawSqlFirstOrDefaultAsync<int>("SELECT 1 UNION SELECT 2", (fun r -> r.GetInt32(0)))
            let result = task.Result
            Assert.True(result.IsSuccess)
            Assert.Equal(1, result.Data)
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

    [<Fact>]
    let ``RawSqlSingleAsync honors cancellation`` () = task {
        let ctx, conn = createContextSqlite()
        try
            let db = ctx.Database
            use cts = new CancellationTokenSource()
            cts.Cancel()
            let! result = db.RawSqlSingleAsync<int>("SELECT 1", (fun r -> r.GetInt32(0)), cts.Token)
            Assert.False(result.IsSuccess)
            let typedErr = Assert.IsType<ExceptionDbError>(result.Error)
            Assert.IsType<OperationCanceledException>(typedErr.Exception, exactMatch = false) |> ignore
        finally
            ctx.Dispose()
            conn.Close()
            conn.Dispose()
    }

    [<Fact>]
    let ``RawSqlFirstOrDefaultAsync honors cancellation`` () = task {
        let ctx, conn = createContextSqlite()
        try
            let db = ctx.Database
            use cts = new CancellationTokenSource()
            cts.Cancel()
            let! result = db.RawSqlFirstOrDefaultAsync<int>("SELECT 1", (fun r -> r.GetInt32(0)), cts.Token)
            Assert.False(result.IsSuccess)
            let typedErr = Assert.IsType<ExceptionDbError>(result.Error)
            Assert.IsType<OperationCanceledException>(typedErr.Exception, exactMatch = false) |> ignore
        finally
            ctx.Dispose()
            conn.Close()
            conn.Dispose()
    }

    [<Fact>]
    let ``RawSqlManyAsync honors cancellation`` () = task {
        let ctx, conn = createContextSqlite()
        try
            let db = ctx.Database
            use cts = new CancellationTokenSource()
            cts.Cancel()
            let! result = db.RawSqlManyAsync<int>("SELECT 1", (fun r -> r.GetInt32(0)), cts.Token)
            Assert.False(result.IsSuccess)
            let typedErr = Assert.IsType<ExceptionDbError>(result.Error)
            Assert.IsType<OperationCanceledException>(typedErr.Exception, exactMatch = false) |> ignore
        finally
            ctx.Dispose()
            conn.Close()
            conn.Dispose()
    }

    [<Fact>]
    let ``RawSqlExecuteAsync honors cancellation`` () = task {
        let ctx, conn = createContextSqlite()
        try
            let db = ctx.Database
            use cts = new CancellationTokenSource()
            cts.Cancel()
            let! result = db.RawSqlExecuteAsync("SELECT 1", cts.Token, [||])
            Assert.False(result.IsSuccess)
            let typedErr = Assert.IsType<ExceptionDbError>(result.Error)
            Assert.IsType<OperationCanceledException>(typedErr.Exception, exactMatch = false) |> ignore
        finally
            ctx.Dispose()
            conn.Close()
            conn.Dispose()
    }

    [<Fact>]
    let ``SqlFirstOrDefaultAsync honors cancellation`` () = task {
        let ctx, conn = createContextSqlite()
        try
            let db = ctx.Database
            let sql = FormattableStringFactory.Create("SELECT 1", [||])
            use cts = new CancellationTokenSource()
            cts.Cancel()
            let! result = db.SqlFirstOrDefaultAsync(sql, (fun r -> r.GetInt32(0)), cts.Token)
            Assert.False(result.IsSuccess)
            let typedErr = Assert.IsType<ExceptionDbError>(result.Error)
            Assert.IsType<OperationCanceledException>(typedErr.Exception, exactMatch = false) |> ignore
        finally
            ctx.Dispose()
            conn.Close()
            conn.Dispose()
    }

    [<Fact>]
    let ``SqlSingleAsync honors cancellation`` () = task {
        let ctx, conn = createContextSqlite()
        try
            let db = ctx.Database
            let sql = FormattableStringFactory.Create("SELECT 1", [||])
            use cts = new CancellationTokenSource()
            cts.Cancel()
            let! result = db.SqlSingleAsync(sql, (fun r -> r.GetInt32(0)), cts.Token)
            Assert.False(result.IsSuccess)
            let typedErr = Assert.IsType<ExceptionDbError>(result.Error)
            Assert.IsType<OperationCanceledException>(typedErr.Exception, exactMatch = false) |> ignore
        finally
            ctx.Dispose()
            conn.Close()
            conn.Dispose()
    }

    [<Fact>]
    let ``SqlManyAsync honors cancellation`` () = task {
        let ctx, conn = createContextSqlite()
        try
            let db = ctx.Database
            let sql = FormattableStringFactory.Create("SELECT 1", [||])
            use cts = new CancellationTokenSource()
            cts.Cancel()
            let! result = db.SqlManyAsync(sql, (fun r -> r.GetInt32(0)), cts.Token)
            Assert.False(result.IsSuccess)
            let typedErr = Assert.IsType<ExceptionDbError>(result.Error)
            Assert.IsType<OperationCanceledException>(typedErr.Exception, exactMatch = false) |> ignore
        finally
            ctx.Dispose()
            conn.Close()
            conn.Dispose()
    }

    [<Fact>]
    let ``SqlExecuteAsync honors cancellation`` () = task {
        let ctx, conn = createContextSqlite()
        try
            let db = ctx.Database
            let sql = FormattableStringFactory.Create("SELECT 1", [||])
            use cts = new CancellationTokenSource()
            cts.Cancel()
            let! result = db.SqlExecuteAsync(sql, cts.Token)
            Assert.False(result.IsSuccess)
            let typedErr = Assert.IsType<ExceptionDbError>(result.Error)
            Assert.IsType<OperationCanceledException>(typedErr.Exception, exactMatch = false) |> ignore
        finally
            ctx.Dispose()
            conn.Close()
            conn.Dispose()
    }

    [<Fact>]
    let ``StoredProcedureSingleAsync with no parameters returns correct value`` () = task {
        let ctx, conn = createContextSqlite()
        try
            let db = ctx.Database
            // Create a simple stored procedure-like view in SQLite
            let! _ = db.RawSqlExecuteAsync("DROP VIEW IF EXISTS sp_GetConstantValue", [||])
            let! _ = db.RawSqlExecuteAsync("CREATE VIEW sp_GetConstantValue AS SELECT 42 as ResultValue", [||])

            let! result = db.StoredProcedureSingleAsync<int>("sp_GetConstantValue", _.GetInt32(0), [||])
            Assert.True(result.IsSuccess)
            Assert.Equal(42, result.Data)
        finally
            ctx.Dispose()
            conn.Close()
            conn.Dispose()
    }

    [<Fact>]
    let ``StoredProcedureFirstOrDefaultAsync with no parameters returns correct value`` () = task {
        let ctx, conn = createContextSqlite()
        try
            let db = ctx.Database
            let! _ = db.RawSqlExecuteAsync("DROP VIEW IF EXISTS sp_GetFirstValue", [||])
            let! _ = db.RawSqlExecuteAsync("CREATE VIEW sp_GetFirstValue AS SELECT 99 as ResultValue", [||])

            let! result = db.StoredProcedureFirstOrDefaultAsync<int>("sp_GetFirstValue", _.GetInt32(0), [||])
            Assert.True(result.IsSuccess)
            Assert.Equal(99, result.Data)
        finally
            ctx.Dispose()
            conn.Close()
            conn.Dispose()
    }

    [<Fact>]
    let ``StoredProcedureManyAsync with no parameters returns correct values`` () = task {
        let ctx, conn = createContextSqlite()
        try
            let db = ctx.Database
            let! _ = db.RawSqlExecuteAsync("DROP VIEW IF EXISTS sp_GetMultipleValues", [||])
            let! _ = db.RawSqlExecuteAsync("CREATE VIEW sp_GetMultipleValues AS SELECT 1 as Value UNION ALL SELECT 2 UNION ALL SELECT 3", [||])

            let! result = db.StoredProcedureManyAsync<int>("sp_GetMultipleValues", _.GetInt32(0), [||])
            Assert.True(result.IsSuccess)
            Assert.Equal([|1;2;3|], result.Data)
        finally
            ctx.Dispose()
            conn.Close()
            conn.Dispose()
    }

    [<Fact>]
    let ``StoredProcedureSingleAsync with two parameters returns correct value`` () = task {
        let ctx, conn = createContextSqlite()
        try
            let db = ctx.Database
            // Create a table to serve as data source
            let! _ = db.RawSqlExecuteAsync("DROP TABLE IF EXISTS sp_Numbers", [||])
            let! _ = db.RawSqlExecuteAsync("CREATE TABLE sp_Numbers (id INTEGER PRIMARY KEY, val1 INTEGER, val2 INTEGER, result INTEGER)", [||])
            let! _ = db.RawSqlExecuteAsync("INSERT INTO sp_Numbers(val1, val2, result) VALUES (10, 32, 42)", [||])

            let param1 = DbParameter("p1", 10)
            let param2 = DbParameter("p2", 32)

            let! result = db.StoredProcedureSingleAsync<int>("sp_AddNumbers", _.GetInt32(0), param1, param2)
            Assert.False(result.IsSuccess)
        finally
            ctx.Dispose()
            conn.Close()
            conn.Dispose()
    }

    [<Fact>]
    let ``StoredProcedureFirstOrDefaultAsync with two parameters returns correct value`` () = task {
        let ctx, conn = createContextSqlite()
        try
            let db = ctx.Database
            let! _ = db.RawSqlExecuteAsync("DROP TABLE IF EXISTS sp_Multiply", [||])
            let! _ = db.RawSqlExecuteAsync("CREATE TABLE sp_Multiply (x INTEGER, y INTEGER, result INTEGER)", [||])
            let! _ = db.RawSqlExecuteAsync("INSERT INTO sp_Multiply(x, y, result) VALUES (5, 15, 75)", [||])

            let paramX = DbParameter("x", 5)
            let paramY = DbParameter("y", 15)

            let! result = db.StoredProcedureFirstOrDefaultAsync<int>("sp_MultiplyNumbers", _.GetInt32(0), paramX, paramY)
            Assert.False(result.IsSuccess)
        finally
            ctx.Dispose()
            conn.Close()
            conn.Dispose()
    }

    [<Fact>]
    let ``StoredProcedureManyAsync with two parameters returns multiple rows`` () = task {
        let ctx, conn = createContextSqlite()
        try
            let db = ctx.Database
            let! _ = db.RawSqlExecuteAsync("DROP TABLE IF EXISTS sp_Range", [||])
            let! _ = db.RawSqlExecuteAsync("CREATE TABLE sp_Range (num INTEGER)", [||])
            let! _ = db.RawSqlExecuteAsync("INSERT INTO sp_Range VALUES (1), (2), (3), (4), (5)", [||])

            let paramStart = DbParameter("start", 1)
            let paramEnd = DbParameter("end", 5)

            let! result = db.StoredProcedureManyAsync<int>("sp_GetRange", _.GetInt32(0), paramStart, paramEnd)
            Assert.False(result.IsSuccess)
        finally
            ctx.Dispose()
            conn.Close()
            conn.Dispose()
    }

    [<Fact>]
    let ``StoredProcedureExecuteAsync with two parameters inserts data`` () = task {
        let ctx, conn = createContextSqlite()
        try
            let db = ctx.Database
            let! _ = db.RawSqlExecuteAsync("DROP TABLE IF EXISTS sp_InsertData", [||])
            let! _ = db.RawSqlExecuteAsync("CREATE TABLE sp_InsertData (id INTEGER PRIMARY KEY, value INTEGER)", [||])

            let paramId = DbParameter("id", 99)
            let paramVal = DbParameter("val", 999)

            let! result = db.StoredProcedureExecuteAsync("sp_InsertValue", paramId, paramVal)
            Assert.False(result.IsSuccess)
            Assert.Equal(1, result.AffectedRows)
        finally
            ctx.Dispose()
            conn.Close()
            conn.Dispose()
    }
