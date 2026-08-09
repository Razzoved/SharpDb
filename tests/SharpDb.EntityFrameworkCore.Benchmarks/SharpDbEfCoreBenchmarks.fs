namespace SharpDb.EntityFrameworkCore.Benchmarks

open Microsoft.EntityFrameworkCore
open System.ComponentModel

type DummyEntity() =
    let mutable id = 0
    let mutable name = ""
    
    member val Id with get, set = id
    member val Name with get, set = name

type InMemoryContextFactory() as this =
    interface IDbContextFactory<DummyDbContext> with
        member _.CreateDbContext() = 
            let options = DbContextOptionsBuilder<DummyDbContext>()
                            .UseInMemoryDatabase(Guid.NewGuid().ToString())
                            .Options
            let ctx = new DummyDbContext(options)
            ctx.Database.EnsureCreated() |> ignore
            ctx

type DummyDbContext(ctx: DbContextOptions<DummyDbContext>) as this =
    inherit DbContext(ctx)
    
    [<Microsoft.FSharp.Core.DefaultValue>] val mutable DummyEntities : DbSet<DummyEntity>
    
    override _.OnModelCreating(modelBuilder: ModelBuilder) =
        modelBuilder.Entity<DummyEntity>().HasKey(e -> e.Id) |> ignore
        modelBuilder.Entity<DummyEntity>().Property(e -> e.Id).ValueGeneratedOnAdd().HasColumnName("id") |> ignore

type SqliteContextFactory() as this =
    let connection = new System.Data.SqlClient.SqlConnection($"Data Source={Guid.NewGuid()};mode=memory;cache=shared;")
    
    interface IDisposable with
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
