using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace SharpDb.EntityFrameworkCore.Comparers;

/// <summary>
/// Comparer that compares two collections by values, rather than references.
/// Use this comparer whenever you use Add or Update methods of DbContext and
/// you know that the collection may change its contents, but not its reference.
/// </summary>
/// <typeparam name="T">Type of collection items</typeparam>
public sealed class CollectionByValuesValueComparer<T> : ValueComparer<ICollection<T>>
{
    public CollectionByValuesValueComparer() : base(
        equalsExpression: (a, b) => EqualsByValues(a, b),
        hashCodeExpression: obj => GetHashCodeByValues(obj),
        snapshotExpression: obj => obj.ToImmutableArray())
    {
    }

    private static bool EqualsByValues(ICollection<T>? a, ICollection<T>? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        if (a.Count != b.Count) return false;

        for (int i = 0; i < a.Count; i++)
        {
            T? aValue = a.ElementAtOrDefault(i);
            T? bValue = b.ElementAtOrDefault(i);

            if (ReferenceEquals(aValue, bValue)) continue;
            if (aValue is null || !aValue.Equals(bValue)) return false;
        }

        return true;
    }

    private static int GetHashCodeByValues(ICollection<T> obj)
    {
        HashCode hash = new();
        for (int i = 0; i < obj.Count; i++)
        {
            hash.Add(obj.ElementAtOrDefault(i));
        }
        return hash.ToHashCode();
    }
}
