namespace SharpDb.EntityFrameworkCore.Benchmarks

open BenchmarkDotNet.Running

module Program =
    [<EntryPoint>]
    let main args =
        printfn "=== UoW Transaction Benchmarks ==="
        printfn ""

        // Run benchmarks
        //let switcher = BenchmarkSwitcher.FromAssembly(Assembly.GetExecutingAssembly()).Run(args, DebugInProcessConfig())

        let benchmarks = [|
            typeof<BenchmarksSmall.UnitOfWorkTest>;
            typeof<BenchmarksLarge.UnitOfWorkTest>
        |]

        BenchmarkRunner.Run(benchmarks) |> ignore

        0
