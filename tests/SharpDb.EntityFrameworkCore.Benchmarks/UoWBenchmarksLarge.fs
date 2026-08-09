namespace SharpDb.EntityFrameworkCore.Benchmarks

open System
open System.Linq
open BenchmarkDotNet.Attributes
open SharpDb
open SharpDb.EntityFrameworkCore.Benchmarks.DbTypes

module BenchmarksLarge =

    [<MemoryDiagnoser>]
    type UnitOfWorkTest() =
        let mutable factory = null
        let mutable uow = null
        let mutable entities = null

        #if DEBUG
        [<GlobalSetup>]
        member _.Debug() =
            System.Diagnostics.Debugger.Launch()
        #endif

        [<IterationSetup>]
        member this.createTestData() =
            factory <- new SqliteContextFactory()
            uow <- new DummyUnitOfWork(factory)
            let cnt = 200000
            [1..cnt]
                |> List.map (fun i -> DummyEntity(Id = i, Name = $"Entity_{i}"))
                |> List.map uow.PrivateContext.Dummy.Add
                |> ignore
            uow.PrivateContext.SaveChanges() |> ignore
            entities <- uow.PrivateContext.Dummy.ToArray()
            printfn $"Prepared %d{cnt} entities (id: %d{entities.Length} rows saved)"

        [<IterationCleanup>]
        member _.cleanup() =
            (factory :> IDisposable).Dispose()
            (uow :> IDisposable).Dispose()
            factory <- null
            uow <- null
            entities <- null

        // =============================================================================
        // SCENARIO 1: Single Transaction - Modify + Delete
        // =============================================================================

        [<Benchmark(Description = "Single transaction: 50000 modified, 10000 deleted, 20000 detached")>]
        member _.scenario1() =
            let i1 = 50000
            let i2 = i1 + 10000
            let i3 = i2 + 20000
            async {
                let t1 = uow.InTransactionAsync(fun () -> task {
                    for i in 1..entities.Length do
                        match i with
                        | i when i <= i1 -> entities[i].Name = entities[i].Name + "_mod" |> ignore
                        | i when i <= i2 -> uow.Repository.Delete entities[i]
                        | i when i <= i3 -> uow.Repository.Detach entities[i]
                        | _ -> 0 |> ignore
                    let! save = uow.SaveChangesAsync().AsTask() |> Async.AwaitTask
                    if not save.IsSuccess then failwith save.Error.Message
                    return ActionState.Complete()
                })
                let! t1Result = t1.AsTask() |> Async.AwaitTask
                return t1Result.AffectedRows
            } |> Async.RunSynchronously

        // =============================================================================
        // SCENARIO 2: Nested Transaction - Success (Partial + Overlapping)
        // =============================================================================

        [<Benchmark(Description = "Nested transaction SUCCESS: Level1=125000 modified, Level2=5000+25000 overlapping")>]
        member _.scenario2() =
            let changed = 100000
            let overlap1 = 20000
            let overlap2 = 5000
            let i2 = 5000
            let offset = 2000
            async {
                let t1 = uow.InTransactionAsync(fun () -> task {
                    entities |> Seq.take changed |> Seq.iter (fun x -> x.Name <- "1")
                    let t2 = uow.InTransactionAsync(fun () -> task {
                        entities |> Seq.skip (changed + offset) |> Seq.take overlap1 |> Seq.iter (fun x -> x.Name <- "2")
                        entities |> Seq.skip (changed + offset) |> Seq.take i2 |> Seq.iter uow.Repository.Delete
                        entities |> Seq.skip (changed + offset + i2 - overlap2) |> Seq.take overlap2 |> Seq.iter (fun x -> x.Name <- "2")
                        let! save = uow.SaveChangesAsync().AsTask() |> Async.AwaitTask
                        if not save.IsSuccess then failwith save.Error.Message
                        return ActionState.Abort("INNER")
                    })
                    let! t2Result = t2.AsTask() |> Async.AwaitTask
                    if t2Result.IsSuccess then failwith "INNER not failed when expected"
                    entities |> Seq.skip (changed + offset) |> Seq.take (overlap1 + overlap2) |> Seq.iter (fun x -> x.Name <- "3")
                    let! save = uow.SaveChangesAsync().AsTask() |> Async.AwaitTask
                    if not save.IsSuccess then failwith save.Error.Message
                    return ActionState.Complete()
                })
                let! t1Result = t1.AsTask() |> Async.AwaitTask
                return t1Result.AffectedRows
            } |> Async.RunSynchronously

        // =============================================================================
        // SCENARIO 3: Nested Transaction - Failure (Full Rollback)
        // =============================================================================

        [<Benchmark(Description = "Nested transaction FAILURE: Full rollback from outer")>]
        member _.scenario3() =
            let changed = 100000
            let overlap1 = 20000
            let overlap2 = 5000
            let i2 = 5000
            let offset = 2000
            async {
                let t1 = uow.InTransactionAsync(fun () -> task {
                    entities |> Seq.take changed |> Seq.iter (fun x -> x.Name <- "1")
                    let t2 = uow.InTransactionAsync(fun () -> task {
                        entities |> Seq.skip (changed + offset) |> Seq.take overlap1 |> Seq.iter (fun x -> x.Name <- "2")
                        entities |> Seq.skip (changed + offset) |> Seq.take i2 |> Seq.iter uow.Repository.Delete
                        entities |> Seq.skip (changed + offset + i2 - overlap2) |> Seq.take overlap2 |> Seq.iter (fun x -> x.Name <- "2")
                        let! save = uow.SaveChangesAsync().AsTask() |> Async.AwaitTask
                        if not save.IsSuccess then failwith save.Error.Message
                        return ActionState.Abort("INNER")
                    })
                    let! t2Result = t2.AsTask() |> Async.AwaitTask
                    if t2Result.IsSuccess then failwith "INNER not failed when expected"
                    entities |> Seq.skip (changed + offset) |> Seq.take (overlap1 + overlap2) |> Seq.iter (fun x -> x.Name <- "3")
                    let! save = uow.SaveChangesAsync().AsTask() |> Async.AwaitTask
                    if not save.IsSuccess then failwith save.Error.Message
                    return ActionState.Abort("OUTER")
                })
                let! t1Result = t1.AsTask() |> Async.AwaitTask
                return t1Result.AffectedRows
            } |> Async.RunSynchronously

