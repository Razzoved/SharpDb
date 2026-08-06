using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace SharpDb.EntityFrameworkCore.Comparers;

/// <summary>
/// Comparer that compares two collections by values, rather than references.
/// Use this comparer whenever you use Add or Update methods of DbContext and
/// you know that the collection may change its contents, but not its reference.
/// </summary>
/// <typeparam name="T">Type of collection items</typeparam>
public sealed class CollectionByValuesValueComparer<T>() : ValueComparer<ICollection<T>>(
    equalsExpression: (a, b) => EqualsByValues(a, b),
    hashCodeExpression: obj => GetHashCodeByValues(obj),
    snapshotExpression: obj => obj.ToImmutableArray())
{
    private static bool EqualsByValues(ICollection<T>? a, ICollection<T>? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        if (a.Count != b.Count) return false;

        using var enumeratorA = a.GetEnumerator();
        using var enumeratorB = b.GetEnumerator();

        while (enumeratorA.MoveNext() && enumeratorB.MoveNext())
        {
            T? aValue = enumeratorA.Current;
            T? bValue = enumeratorB.Current;

            if (ReferenceEquals(aValue, bValue)) continue;
            if (aValue is null || !aValue.Equals(bValue)) return false;
        }

        return true;
    }

    private static int GetHashCodeByValues(ICollection<T> obj)
    {
        HashCode hash = new();
        foreach (T item in obj)
        {
            hash.Add(item);
        }
        return hash.ToHashCode();
    }
}
