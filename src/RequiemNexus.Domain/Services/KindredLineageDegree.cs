namespace RequiemNexus.Domain.Services;

/// <summary>
/// Computes shortest-path separation between Kindred using in-chronicle PC sire links (V:tR 2e Blood Sympathy graph).
/// </summary>
public static class KindredLineageDegree
{
    /// <summary>
    /// Returns the shortest path length between two character IDs using undirected sire edges within the roster map.
    /// </summary>
    /// <param name="fromCharacterId">The starting character.</param>
    /// <param name="toCharacterId">The target character.</param>
    /// <param name="sireByCharacterId">Maps each chronicle PC id to its in-roster sire id, when any.</param>
    /// <returns>Zero when both ids match; otherwise the shortest number of edges; <see langword="null"/> when either id is missing from the map or no path exists.</returns>
    public static int? TryGetShortestDegree(
        int fromCharacterId,
        int toCharacterId,
        IReadOnlyDictionary<int, int?> sireByCharacterId)
    {
        if (!sireByCharacterId.ContainsKey(fromCharacterId) || !sireByCharacterId.ContainsKey(toCharacterId))
        {
            return null;
        }

        if (fromCharacterId == toCharacterId)
        {
            return 0;
        }

        var adjacency = new Dictionary<int, List<int>>();
        foreach ((int id, int? sireId) in sireByCharacterId)
        {
            if (!adjacency.ContainsKey(id))
            {
                adjacency[id] = [];
            }

            if (sireId.HasValue)
            {
                if (!adjacency.ContainsKey(sireId.Value))
                {
                    adjacency[sireId.Value] = [];
                }

                adjacency[id].Add(sireId.Value);
                adjacency[sireId.Value].Add(id);
            }
        }

        var queue = new Queue<(int Node, int Depth)>();
        var visited = new HashSet<int>();
        queue.Enqueue((fromCharacterId, 0));
        visited.Add(fromCharacterId);
        while (queue.Count > 0)
        {
            (int node, int depth) = queue.Dequeue();
            if (node == toCharacterId)
            {
                return depth;
            }

            if (!adjacency.TryGetValue(node, out List<int>? neighbors))
            {
                continue;
            }

            foreach (int n in neighbors)
            {
                if (visited.Add(n))
                {
                    queue.Enqueue((n, depth + 1));
                }
            }
        }

        return null;
    }
}
