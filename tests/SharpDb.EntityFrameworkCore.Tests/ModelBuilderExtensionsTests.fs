namespace SharpDb.EntityFrameworkCore.Tests

open System
open System.Reflection
open Microsoft.EntityFrameworkCore
open Xunit
open SharpDb.EntityFrameworkCore
open Microsoft.EntityFrameworkCore.Metadata.Builders

module ModelBuilderExtensionsTests =

    type Parent() =
        member val Id = 0 with get, set
        member val Name = "" with get, set

    [<Keyless>]
    type Child() =
        member val Id = 0 with get, set
        member val ParentId = 0 with get, set
        member val Parent : Parent | null = null with get, set

    type Left() =
        member val Id = 0 with get, set
        member val ChildId = 0 with get, set
        member val Child : Child | null = null with get, set

    [<Keyless>]
    type CycleB() =
        member val Id = 0 with get, set
        member val CycleBId = 0 with get, set
        member val CycleA : CycleA | null = null with get, set

    and CycleA() =
        member val Id = 0 with get, set
        member val CycleAId = 0 with get, set
        member val CycleB : CycleB | null = null with get, set

    type Orphan() =
        member val Id = 0 with get, set
        member val Value = "" with get, set

    type ParentConfig() =
        interface IEntityTypeConfiguration<Parent> with
            member _.Configure(builder: EntityTypeBuilder<Parent>) =
                builder.HasKey("Id") |> ignore
                builder.Property(fun p -> p.Name).IsRequired().HasComment("PARENT") |> ignore

    type ChildConfig() =
        interface IEntityTypeConfiguration<Child> with
            member _.Configure(builder: EntityTypeBuilder<Child>) =
                builder.HasKey("Id") |> ignore
                builder.HasOne(fun c -> c.Parent).WithMany() |> ignore

    type LeftConfig() =
        interface IEntityTypeConfiguration<Left> with
            member _.Configure(builder: EntityTypeBuilder<Left>) =
                builder.HasKey("Id") |> ignore
                builder.HasOne(fun l -> l.Child).WithMany() |> ignore

    type CycleAConfig() =
        interface IEntityTypeConfiguration<CycleA> with
            member _.Configure(builder: EntityTypeBuilder<CycleA>) =
                builder.HasKey("Id") |> ignore
                builder.HasOne(fun a -> a.CycleB).WithMany() |> ignore

    type CycleBConfig() =
        interface IEntityTypeConfiguration<CycleB> with
            member _.Configure(builder: EntityTypeBuilder<CycleB>) =
                builder.HasKey("Id") |> ignore
                builder.HasOne(fun b -> b.CycleA).WithMany() |> ignore

    type OrphanConfig() =
        interface IEntityTypeConfiguration<Orphan> with
            member _.Configure(builder: EntityTypeBuilder<Orphan>) =
                builder.HasKey("Id") |> ignore
                builder.Property(fun o -> o.Value).IsRequired().HasComment("ORPHAN") |> ignore

    [<Fact>]
    let ``ApplyConfigurationsFromAssemblyWithDependencyResolution handles complex dependencies`` () =
        let asm = Assembly.GetExecutingAssembly()
        let modelBuilder = ModelBuilder()
        modelBuilder.Model.AddEntityType(typeof<Parent>) |> ignore
        modelBuilder.Model.AddEntityType(typeof<Orphan>) |> ignore
        modelBuilder.Model.AddEntityType(typeof<Child>) |> ignore
        modelBuilder.Model.AddEntityType(typeof<Left>) |> ignore
        modelBuilder.Model.AddEntityType(typeof<CycleA>) |> ignore
        modelBuilder.Model.AddEntityType(typeof<CycleB>) |> ignore

        modelBuilder.ApplyConfigurationsFromAssemblyWithDependencyResolution(asm) |> ignore

        let parentEntity = modelBuilder.Model.FindEntityType(typeof<Parent>)
        let orphanEntity = modelBuilder.Model.FindEntityType(typeof<Orphan>)
        Assert.Equal("PARENT", parentEntity.GetProperty("Name").GetComment())
        Assert.Equal("ORPHAN", orphanEntity.GetProperty("Value").GetComment())

    [<Fact>]
    let ``ApplyConfigurationsFromAssemblyWithDependencyResolution applies only predicate-matching configs`` () =
        let asm = Assembly.GetExecutingAssembly()
        let modelBuilder = ModelBuilder()
        modelBuilder.Model.AddEntityType(typeof<Parent>) |> ignore
        modelBuilder.Model.AddEntityType(typeof<Orphan>) |> ignore
        modelBuilder.Model.AddEntityType(typeof<Child>) |> ignore
        modelBuilder.Model.AddEntityType(typeof<Left>) |> ignore
        modelBuilder.Model.AddEntityType(typeof<CycleA>) |> ignore
        modelBuilder.Model.AddEntityType(typeof<CycleB>) |> ignore

        let predicate (t: Type) = t.Name <> "OrphanConfig"
        modelBuilder.ApplyConfigurationsFromAssemblyWithDependencyResolution(asm, predicate) |> ignore

        let parentEntity = modelBuilder.Model.FindEntityType(typeof<Parent>)
        let orphanEntity = modelBuilder.Model.FindEntityType(typeof<Orphan>)
        Assert.Equal("PARENT", parentEntity.GetProperty("Name").GetComment())
        Assert.ThrowsAny(fun () -> orphanEntity.GetProperty("Value").GetComment() |> ignore) |> ignore
