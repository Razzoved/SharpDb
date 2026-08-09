namespace SharpDb.EntityFrameworkCore.Benchmarks

open System
open Microsoft.Data.Sqlite
open Microsoft.EntityFrameworkCore
open SharpDb.EntityFrameworkCore
open SharpDb.EntityFrameworkCore.Repositories

module DbTypes =

    type DummyEntity() =
        member val Id = 0 with get, set
        member val Name = "" with get, set

    type DummyDbContext(ctx: DbContextOptions<DummyDbContext>) =
        inherit DbContext(ctx)
        member this.Dummy: DbSet<DummyEntity> = this.Set<DummyEntity>()
        override this.OnModelCreating (modelBuilder: ModelBuilder) =
            modelBuilder.Entity<DummyEntity>().HasKey("Id") |> ignore
            modelBuilder.Entity<DummyEntity>().HasIndex("Name").IsUnique(false).IsClustered(false) |> ignore
            modelBuilder.Entity<DummyEntity>().Property(_.Id).ValueGeneratedOnAdd().HasColumnName("id") |> ignore

    type DummyUnitOfWork(ctxFactory: IDbContextFactory<DummyDbContext>) =
        inherit UnitOfWork<DummyDbContext>(ctxFactory)
        member this.PrivateContext with get() = this.DbContext
        member this.Repository = this.GetRepository(fun ctx -> DefaultRepository<DummyEntity>(ctx))

    type SqliteContextFactory() =
        let connection = new SqliteConnection($"Data Source={Guid.NewGuid()};mode=memory;cache=shared;")
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
