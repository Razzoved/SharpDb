namespace SharpDb.EntityFrameworkCore.Tests.Comparers

open System.Collections.Generic
open Xunit
open SharpDb.EntityFrameworkCore.Comparers

module CollectionByValuesValueComparerTests =

    [<Fact>]
    let ``EqualsByValues returns true for identical collections`` () =
        let comparer = CollectionByValuesValueComparer<int>()
        let a = ResizeArray([1; 2; 3]) :> ICollection<int>
        let b = ResizeArray([1; 2; 3]) :> ICollection<int>
        Assert.True(comparer.Equals(a, b))

    [<Fact>]
    let ``EqualsByValues returns false for collections with different values`` () =
        let comparer = CollectionByValuesValueComparer<int>()
        let a = ResizeArray([1; 2; 3]) :> ICollection<int>
        let b = ResizeArray([1; 2; 4]) :> ICollection<int>
        Assert.False(comparer.Equals(a, b))

    [<Fact>]
    let ``EqualsByValues returns false for collections with different counts`` () =
        let comparer = CollectionByValuesValueComparer<int>()
        let a = ResizeArray([1; 2; 3]) :> ICollection<int>
        let b = ResizeArray([1; 2]) :> ICollection<int>
        Assert.False(comparer.Equals(a, b))

    [<Fact>]
    let ``EqualsByValues returns true for same reference`` () =
        let comparer = CollectionByValuesValueComparer<int>()
        let a = ResizeArray([1; 2; 3]) :> ICollection<int>
        Assert.True(comparer.Equals(a, a))

    [<Fact>]
    let ``EqualsByValues returns false if either collection is null`` () =
        let comparer = CollectionByValuesValueComparer<int>()
        let a = null
        let b = ResizeArray([1; 2; 3]) :> ICollection<int>
        Assert.False(comparer.Equals(a, b))
        Assert.False(comparer.Equals(b, a))
        Assert.True(comparer.Equals(null, null))

    [<Fact>]
    let ``GetHashCodeByValues returns same hash for equal collections`` () =
        let comparer = CollectionByValuesValueComparer<int>()
        let a = ResizeArray([1; 2; 3]) :> ICollection<int>
        let b = ResizeArray([1; 2; 3]) :> ICollection<int>
        Assert.Equal(comparer.GetHashCode(a), comparer.GetHashCode(b))

    [<Fact>]
    let ``GetHashCodeByValues returns different hash for different collections`` () =
        let comparer = CollectionByValuesValueComparer<int>()
        let a = ResizeArray([1; 2; 3]) :> ICollection<int>
        let b = ResizeArray([1; 2; 4]) :> ICollection<int>
        Assert.NotEqual(comparer.GetHashCode(a), comparer.GetHashCode(b))
