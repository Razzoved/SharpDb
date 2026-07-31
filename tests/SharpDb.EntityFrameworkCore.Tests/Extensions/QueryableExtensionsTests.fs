namespace SharpDb.EntityFrameworkCore.Tests.Extensions

open System
open System.Linq
open Microsoft.EntityFrameworkCore
open Xunit
open SharpDb.EntityFrameworkCore

module QueryableExtensionsTests =

    type DummyEntity() =
        member val Id = 0 with get, set
        member val Name = "" with get, set
        member val Value = 0 with get, set

    let private testData = [|
        DummyEntity(Id = 1, Name = "Alice",   Value = 100);
        DummyEntity(Id = 2, Name = "Bob",     Value = 200);
        DummyEntity(Id = 3, Name = "Charlie", Value = 150);
        DummyEntity(Id = 4, Name = "David",   Value = 300);
    |]

    type DummyDbContext(ctx: DbContextOptions<DummyDbContext>) =
        inherit DbContext(ctx)
        [<DefaultValue>] val mutable DummyEntities : DbSet<DummyEntity>
        override _.OnModelCreating(modelBuilder: ModelBuilder) =
            modelBuilder.Entity<DummyEntity>().HasKey("Id") |> ignore
            modelBuilder.Entity<DummyEntity>().Property(fun e -> e.Id).ValueGeneratedOnAdd().HasColumnName("id") |> ignore
            modelBuilder.Entity<DummyEntity>().HasData(testData) |> ignore

    type InMemoryContextFactory() =
        interface IDbContextFactory<DummyDbContext> with
            member _.CreateDbContext() =
                let options = DbContextOptionsBuilder<DummyDbContext>()
                                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                                .Options
                let ctx = new DummyDbContext(options)
                ctx.Database.EnsureCreated() |> ignore
                ctx

    let private makeQueryable () =
        let factory = InMemoryContextFactory() :> IDbContextFactory<DummyDbContext>
        let ctx = factory.CreateDbContext()
        ctx.Set<DummyEntity>().AsQueryable()

    // ── Single(OrDefault) ──────────────────────────────────────────────────

    [<Fact>]
    let ``SingleResult returns success for unique match`` () =
        let result = makeQueryable().SingleResult(fun x -> x.Name = "Alice")
        Assert.True(result.IsSuccess)
        Assert.Equal("Alice", result.Data.Name)

    [<Fact>]
    let ``SingleResult returns failure when no match`` () =
        let result = makeQueryable().SingleResult(fun x -> x.Name = "Nobody")
        Assert.False(result.IsSuccess)

    [<Fact>]
    let ``SingleResult returns failure when multiple matches`` () =
        let result = makeQueryable().SingleResult(fun x -> x.Value > 100)
        Assert.False(result.IsSuccess)

    [<Fact>]
    let ``SingleOrDefaultResult returns success with value when match found`` () =
        let result = makeQueryable().SingleOrDefaultResult(fun x -> x.Name = "Bob")
        Assert.True(result.IsSuccess)
        Assert.Equal("Bob", result.Data.Name)

    [<Fact>]
    let ``SingleOrDefaultResult returns success with null when no match`` () =
        let result = makeQueryable().SingleOrDefaultResult(fun x -> x.Name = "Nobody")
        Assert.True(result.IsSuccess)
        Assert.Equal(Unchecked.defaultof<DummyEntity>, result.Data)

    // ── First(OrDefault) ──────────────────────────────────────────────────

    [<Fact>]
    let ``FirstResult returns first matching item`` () =
        let result = makeQueryable().FirstResult(fun x -> x.Value > 100)
        Assert.True(result.IsSuccess)
        Assert.Equal("Bob", result.Data.Name)

    [<Fact>]
    let ``FirstResult returns failure when no match`` () =
        let result = makeQueryable().FirstResult(fun x -> x.Value > 9999)
        Assert.False(result.IsSuccess)

    [<Fact>]
    let ``FirstOrDefaultResult returns success with value when match found`` () =
        let result = makeQueryable().FirstOrDefaultResult(fun x -> x.Name = "Charlie")
        Assert.True(result.IsSuccess)
        Assert.Equal("Charlie", result.Data.Name)

    [<Fact>]
    let ``FirstOrDefaultResult returns success with null when no match`` () =
        let result = makeQueryable().FirstOrDefaultResult(fun x -> x.Name = "Nobody")
        Assert.True(result.IsSuccess)
        Assert.Equal(Unchecked.defaultof<DummyEntity>, result.Data)

    // ── Last(OrDefault) ───────────────────────────────────────────────────

    [<Fact>]
    let ``LastResult returns last matching item`` () =
        let result = makeQueryable().LastResult(fun x -> x.Value < 200)
        Assert.True(result.IsSuccess)
        Assert.Equal("Charlie", result.Data.Name)

    [<Fact>]
    let ``LastResult returns failure when no match`` () =
        let result = makeQueryable().LastResult(fun x -> x.Value > 9999)
        Assert.False(result.IsSuccess)

    [<Fact>]
    let ``LastOrDefaultResult returns success with value when match found`` () =
        let result = makeQueryable().LastOrDefaultResult(fun x -> x.Value < 300)
        Assert.True(result.IsSuccess)
        Assert.Equal("Charlie", result.Data.Name)

    [<Fact>]
    let ``LastOrDefaultResult returns success with null when no match`` () =
        let result = makeQueryable().LastOrDefaultResult(fun x -> x.Name = "Nobody")
        Assert.True(result.IsSuccess)
        Assert.Equal(Unchecked.defaultof<DummyEntity>, result.Data)

    // ── ToArray / ToList ──────────────────────────────────────────────────

    [<Fact>]
    let ``ToArrayResult returns all items as array`` () =
        let result = makeQueryable().ToArrayResult()
        Assert.True(result.IsSuccess)
        Assert.Equal(4, result.Data.Length)

    [<Fact>]
    let ``ToListResult returns all items as list`` () =
        let result = makeQueryable().ToListResult()
        Assert.True(result.IsSuccess)
        Assert.Equal(4, result.Data.Count)

    // ── ToHashSet ─────────────────────────────────────────────────────────

    [<Fact>]
    let ``ToHashSetResult returns all unique items`` () =
        let result = makeQueryable().ToHashSetResult()
        Assert.True(result.IsSuccess)
        Assert.Equal(4, result.Data.Count)

    // ── Count / LongCount ─────────────────────────────────────────────────

    [<Fact>]
    let ``CountResult returns total count`` () =
        let result = makeQueryable().CountResult()
        Assert.True(result.IsSuccess)
        Assert.Equal(4, result.Data)

    [<Fact>]
    let ``CountResult with predicate returns filtered count`` () =
        let result = makeQueryable().CountResult(fun x -> x.Value > 150)
        Assert.True(result.IsSuccess)
        Assert.Equal(2, result.Data)

    [<Fact>]
    let ``LongCountResult returns total count as long`` () =
        let result = makeQueryable().LongCountResult()
        Assert.True(result.IsSuccess)
        Assert.Equal(4L, result.Data)

    [<Fact>]
    let ``LongCountResult with predicate returns filtered count`` () =
        let result = makeQueryable().LongCountResult(fun x -> x.Value > 150)
        Assert.True(result.IsSuccess)
        Assert.Equal(2L, result.Data)

    // ── Any / All / Contains ──────────────────────────────────────────────

    [<Fact>]
    let ``AnyResult returns true when items match predicate`` () =
        let result = makeQueryable().AnyResult(fun x -> x.Value > 250)
        Assert.True(result.IsSuccess)
        Assert.True(result.Data)

    [<Fact>]
    let ``AnyResult returns false when no items match predicate`` () =
        let result = makeQueryable().AnyResult(fun x -> x.Value > 9999)
        Assert.True(result.IsSuccess)
        Assert.False(result.Data)

    [<Fact>]
    let ``AnyResult without predicate returns true for non-empty sequence`` () =
        let result = makeQueryable().AnyResult()
        Assert.True(result.IsSuccess)
        Assert.True(result.Data)

    [<Fact>]
    let ``AllResult returns true when all items match predicate`` () =
        let result = makeQueryable().AllResult(fun x -> x.Id > 0)
        Assert.True(result.IsSuccess)
        Assert.True(result.Data)

    [<Fact>]
    let ``AllResult returns false when some items do not match`` () =
        let result = makeQueryable().AllResult(fun x -> x.Value > 100)
        Assert.True(result.IsSuccess)
        Assert.False(result.Data)

    [<Fact>]
    let ``ContainsResult returns true when item is present`` () =
        let item = testData.[0]
        let result = makeQueryable().ContainsResult(item)
        Assert.True(result.IsSuccess)
        Assert.True(result.Data)

    [<Fact>]
    let ``ContainsResult returns false when item is absent`` () =
        let item = DummyEntity(Id = 99, Name = "Ghost", Value = 0)
        let result = makeQueryable().ContainsResult(item)
        Assert.True(result.IsSuccess)
        Assert.False(result.Data)

    // ── Sum ───────────────────────────────────────────────────────────────

    [<Fact>]
    let ``SumResult int returns correct sum`` () =
        let result = makeQueryable().SumResult(fun x -> x.Value)
        Assert.True(result.IsSuccess)
        Assert.Equal(750, result.Data)

    [<Fact>]
    let ``SumResult long returns correct sum`` () =
        let result = makeQueryable().SumResult(fun x -> int64 x.Value)
        Assert.True(result.IsSuccess)
        Assert.Equal(750L, result.Data)

    [<Fact>]
    let ``SumResult double returns correct sum`` () =
        let result = makeQueryable().SumResult(fun x -> float x.Value)
        Assert.True(result.IsSuccess)
        Assert.Equal(750.0, result.Data)

    [<Fact>]
    let ``SumResult decimal returns correct sum`` () =
        let result = makeQueryable().SumResult(fun x -> decimal x.Value)
        Assert.True(result.IsSuccess)
        Assert.Equal(750m, result.Data)

    // ── Average ───────────────────────────────────────────────────────────

    [<Fact>]
    let ``AverageResult int returns correct average`` () =
        let result = makeQueryable().AverageResult(fun x -> x.Value)
        Assert.True(result.IsSuccess)
        Assert.Equal(187.5, result.Data)

    [<Fact>]
    let ``AverageResult decimal returns correct average`` () =
        let result = makeQueryable().AverageResult(fun x -> decimal x.Value)
        Assert.True(result.IsSuccess)
        Assert.Equal(187.5m, result.Data)

    // ── Min / Max ─────────────────────────────────────────────────────────

    [<Fact>]
    let ``MinResult with selector returns minimum value`` () =
        let result = makeQueryable().MinResult(fun x -> x.Value)
        Assert.True(result.IsSuccess)
        Assert.Equal(100, result.Data)

    [<Fact>]
    let ``MaxResult with selector returns maximum value`` () =
        let result = makeQueryable().MaxResult(fun x -> x.Value)
        Assert.True(result.IsSuccess)
        Assert.Equal(300, result.Data)

    // ── Async variants ────────────────────────────────────────────────────

    [<Fact>]
    let ``SingleOrDefaultAsyncResult returns success`` () =
        task {
            let! result = makeQueryable().SingleOrDefaultAsyncResult(fun x -> x.Name = "Alice")
            Assert.True(result.IsSuccess)
            Assert.Equal("Alice", result.Data.Name)
        }

    [<Fact>]
    let ``FirstOrDefaultAsyncResult returns success`` () =
        task {
            let! result = makeQueryable().FirstOrDefaultAsyncResult(fun x -> x.Value > 100)
            Assert.True(result.IsSuccess)
            Assert.True(result.Data.Value > 100)
        }

    [<Fact>]
    let ``ToListAsyncResult returns all items`` () =
        task {
            let! result = makeQueryable().ToListAsyncResult()
            Assert.True(result.IsSuccess)
            Assert.Equal(4, result.Data.Count)
        }

    [<Fact>]
    let ``ToArrayAsyncResult returns all items`` () =
        task {
            let! result = makeQueryable().ToArrayAsyncResult()
            Assert.True(result.IsSuccess)
            Assert.Equal(4, result.Data.Length)
        }

    [<Fact>]
    let ``CountAsyncResult returns total count`` () =
        task {
            let! result = makeQueryable().CountAsyncResult()
            Assert.True(result.IsSuccess)
            Assert.Equal(4, result.Data)
        }

    [<Fact>]
    let ``CountAsyncResult with predicate returns filtered count`` () =
        task {
            let! result = makeQueryable().CountAsyncResult(fun x -> x.Value > 150)
            Assert.True(result.IsSuccess)
            Assert.Equal(2, result.Data)
        }

    [<Fact>]
    let ``AnyAsyncResult returns true when items match`` () =
        task {
            let! result = makeQueryable().AnyAsyncResult(fun x -> x.Value > 250)
            Assert.True(result.IsSuccess)
            Assert.True(result.Data)
        }

    [<Fact>]
    let ``AllAsyncResult returns true when all items match`` () =
        task {
            let! result = makeQueryable().AllAsyncResult(fun x -> x.Id > 0)
            Assert.True(result.IsSuccess)
            Assert.True(result.Data)
        }

    [<Fact>]
    let ``MinAsyncResult with selector returns minimum`` () =
        task {
            let! result = makeQueryable().MinAsyncResult(fun x -> x.Value)
            Assert.True(result.IsSuccess)
            Assert.Equal(100, result.Data)
        }

    [<Fact>]
    let ``MaxAsyncResult with selector returns maximum`` () =
        task {
            let! result = makeQueryable().MaxAsyncResult(fun x -> x.Value)
            Assert.True(result.IsSuccess)
            Assert.Equal(300, result.Data)
        }
