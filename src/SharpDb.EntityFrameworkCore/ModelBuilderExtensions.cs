using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace SharpDb.EntityFrameworkCore;

public static class ModelBuilderExtensions
{
    /// <summary>
    /// <inheritdoc cref="ModelBuilder.ApplyConfigurationsFromAssembly(Assembly, Func{Type, bool}?)"/>
    /// When any of the discovered configurations fails to apply it is retried at the end.
    /// </summary>
    /// <param name="modelBuilder">Model builder instance</param>
    /// <param name="assembly">Assembly to load configurations from</param>
    /// <param name="predicate">Filter for configurations to be applied</param>
    /// <exception cref="NullReferenceException"></exception>
    /// <inheritdoc cref="ModelBuilder.ApplyConfigurationsFromAssembly(Assembly, Func{Type, bool}?)"/>
    [RequiresUnreferencedCode("Uses reflection to gather configurations")]
    public static ModelBuilder ApplyConfigurationsFromAssemblyWithDependencyResolution(this ModelBuilder modelBuilder, Assembly assembly, Func<Type, bool>? predicate = null)
    {
        var applyEntityConfigurationMethod = typeof(ModelBuilder)
            .GetMethods()
            .Single(e => e is { Name: nameof(ModelBuilder.ApplyConfiguration), ContainsGenericParameters: true }
                && e.GetParameters().SingleOrDefault()?.ParameterType.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>));

        // Gather available configurations from given assembly
        Dictionary<Type, List<(Type Type, Type Interface, object Instance)>> configurations = [];
        foreach (var t in assembly.GetTypes())
        {
            if (t.GetConstructor(Type.EmptyTypes) is null) continue;
            if (predicate is not null && !predicate.Invoke(t)) continue;
            object? typeInstance = null;
            foreach (var i in t.GetInterfaces())
            {
                if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>))
                {
                    typeInstance ??= Activator.CreateInstance(t) ?? throw new NullReferenceException(string.Format(Resources.Text_Error_TypeInstantiationFailed, t.Name));
                    Type entityType = i.GetGenericArguments().First();
                    if (!configurations.TryGetValue(entityType, out var existingConfigs))
                    {
                        existingConfigs = [];
                        configurations[entityType] = existingConfigs;
                    }
                    existingConfigs.Add((
                        Type: t,
                        Interface: i,
                        Instance: typeInstance));
                }
            }
        }

        // Construct an entity dependency graph
        TypeDependencyGraph graph = new();
        foreach (var t in configurations.Keys)
        {
            graph.AddType(t);
        }

        // Apply configurations in dependency order
        foreach (var entityType in graph.Flatten())
        {
            if (configurations.TryGetValue(entityType, out var configs))
            {
                foreach (var config in configs)
                {
                    applyEntityConfigurationMethod
                        .MakeGenericMethod(entityType)
                        .Invoke(modelBuilder, [config.Instance]);
                }
            }
        }

        return modelBuilder;
    }

    private readonly ref struct TypeDependencyGraph()
    {
        private readonly Dictionary<Type, HashSet<Type>> _graph = [];

        private IEnumerable<Type> Vertices => _graph.Keys;
        private int VertexCount => _graph.Count;
        private HashSet<Type> OutgoingEdges(Type type) => _graph[type];

        public void AddType(Type type)
        {
            BuildDependencies(type, []);
        }

        /// <summary>
        /// Flattens the dependency graph into an ordered list of types, where dependent types appear after the types they depend on.
        /// </summary>
        /// <returns>Ordered list of types</returns>
        public ReadOnlyCollection<Type> Flatten()
        {
            List<Type> result = [];

            // compute strongly connected components
            var sccs = GetStronglyConnectedComponents();
            var sccsOutgoingEdges = GetOutgoingComponentEdges(sccs);

            // process all components in topological order
            int sccIndex = -1;
            while (sccs.Count > 0)
            {
                if (sccIndex < 0 || sccIndex > sccs.Count - 1) sccIndex = sccs.Count - 1;
                // process components that have no incoming edges
                var scc = sccs[sccIndex];
                var outgoingEdges = sccsOutgoingEdges[scc];
                if (outgoingEdges.Count == 0)
                {
                    // order types within this component, add to processed
                    var orderedTypes = OrderTypes(scc);
                    result.AddRange(orderedTypes);

                    // remove this component from processing
                    sccs.RemoveAt(sccIndex);
                    sccsOutgoingEdges.Remove(scc);

                    // remove edges for this component from other components
                    foreach (var edges in sccsOutgoingEdges)
                    {
                        edges.Value.Remove(scc);
                    }
                }
                sccIndex--;
            }

            return result.AsReadOnly();
        }

        private static Type? GetRelevantType(Type type)
        {
            while (true)
            {
                if (type == typeof(string) || type.IsPrimitive) break;
                if (type.IsGenericType && typeof(System.Collections.IEnumerable).IsAssignableFrom(type))
                {
                    var arg = type.GetGenericArguments().LastOrDefault();
                    if (arg is not null) type = arg;
                }
                if (Nullable.GetUnderlyingType(type) is not { } underlyingType) break;
                type = underlyingType;
            }
            if (type == typeof(string) || type.IsPrimitive) return null;
            if (type.Namespace is not null && type.Namespace.StartsWith("System")) return null;
            return type;
        }

        private void BuildDependencies(Type type, HashSet<Type> visited)
        {
            if (!visited.Add(type)) return;

            if (!_graph.TryGetValue(type, out var dependencies))
            {
                dependencies = [];
                _graph[type] = dependencies;
            }

            foreach (var p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                // Get relevant type
                Type? propertyType = GetRelevantType(p.PropertyType);
                if (propertyType is null
                    || propertyType == type
                    || propertyType == typeof(string)
                    || propertyType.IsPrimitive
                    || visited.Contains(propertyType)) continue;

                // Add dependency along with its dependencies
                if (dependencies.Add(propertyType)) BuildDependencies(propertyType, visited);
            }
        }

        /// <summary>
        /// Fetches first strongly connected component via Tarjan's algorithm.
        /// </summary>
        /// <returns>Collection of types that are part of the SCC</returns>
        private List<List<Type>> GetStronglyConnectedComponents()
        {
            List<List<Type>> sccs = [];

            Dictionary<Type, int> indexMap = new(VertexCount);
            foreach (var v in Vertices)
            {
                indexMap[v] = indexMap.Count;
            }

            int preCount = 0;
            Stack<Type> stack = [];
            Span<bool> visited = stackalloc bool[VertexCount];
            Span<int> low = stackalloc int[VertexCount];

            Stack<int> minStack = [];
            Stack<IEnumerator<Type>> enumeratorStack = [];
            IEnumerator<Type> enumerator = Vertices.GetEnumerator();

            while (true)
            {
                if (enumerator.MoveNext()) // Search vertices
                {
                    Type v = enumerator.Current;
                    int vIndex = indexMap[v];
                    if (!visited[vIndex])
                    {
                        low[vIndex] = preCount++;
                        visited[vIndex] = true;
                        stack.Push(v);
                        minStack.Push(low[vIndex]);
                        enumeratorStack.Push(enumerator);
                        enumerator = OutgoingEdges(v).GetEnumerator();
                    }
                    else if (minStack.Count > 0)
                    {
                        int min = Math.Min(minStack.Pop(), low[vIndex]);
                        minStack.Push(min);
                    }
                }
                else if (enumeratorStack.Count > 0) // Level up
                {
                    enumerator.Dispose();
                    enumerator = enumeratorStack.Pop();

                    Type v = enumerator.Current;
                    int vIndex = indexMap[v];
                    int min = minStack.Pop();

                    if (min < low[vIndex])
                    {
                        low[vIndex] = min;
                    }
                    else
                    {
                        List<Type> scc = [];
                        int wIndex;
                        do
                        {
                            Type w = stack.Pop();
                            wIndex = indexMap[w];
                            scc.Add(w);
                            low[wIndex] = VertexCount;
                        } while (wIndex != vIndex);
                        sccs.Add(scc);
                    }

                    if (minStack.Count > 0)
                    {
                        min = Math.Min(minStack.Pop(), low[vIndex]);
                        minStack.Push(min);
                    }
                }
                else // End of search
                {
                    break;
                }
            }

            enumerator.Dispose();
            return sccs;
        }

        /// <summary>
        /// Fetches edges between strongly connected components.
        /// </summary>
        /// <param name="components">Strongly connected components to resolve edges for</param>
        /// <returns>Dictionary of outgoing component edges</returns>
        private Dictionary<List<Type>, HashSet<List<Type>>> GetOutgoingComponentEdges(List<List<Type>> components)
        {
            Dictionary<List<Type>, HashSet<List<Type>>> result = [];
            foreach (var scc in components)
            {
                result[scc] = [];
                foreach (var otherScc in components)
                {
                    if (ReferenceEquals(scc, otherScc)) continue;
                    bool hasEdge = false;
                    foreach (var vertex in scc)
                    {
                        foreach (var edge in OutgoingEdges(vertex))
                        {
                            if (otherScc.Contains(edge))
                            {
                                result[scc].Add(otherScc);
                                hasEdge = true;
                            }
                            if (hasEdge) break;
                        }
                        if (hasEdge) break;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Orders a list of types with predefined heuristics.
        /// </summary>
        /// <param name="types">Types to be ordered</param>
        /// <returns>Ordered types</returns>
        private static IEnumerable<Type> OrderTypes(List<Type> types)
        {
            return types
                .OrderByDescending(t => t.GetCustomAttribute<KeylessAttribute>(true) is not null)
                .ThenByDescending(t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance).All(p => p.GetCustomAttribute<KeyAttribute>(true) is null))
                .ThenByDescending(t => t.Name.EndsWith("_CL") || t.Name.EndsWith("_VW"))
                .ThenBy(t => t.Name.EndsWith("Item") || t.Name.EndsWith("Items"))
                .ThenBy(t => t.FullName);
        }
    }
}
