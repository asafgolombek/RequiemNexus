using System.Text.Json;

namespace RequiemNexus.Web.Components.Pages.Campaigns;

/// <summary>
/// JSON parse helpers for NPC attribute/skill blobs on <see cref="NpcDetail"/>.
/// </summary>
internal static class NpcDetailStatsHelper
{
    /// <summary>Parses a JSON stats blob; missing keys default to 2.</summary>
    /// <param name="json">Serialized dictionary from persistence.</param>
    /// <param name="categories">Category groupings defining expected keys.</param>
    /// <returns>A full dictionary with a value for every listed key.</returns>
    public static Dictionary<string, int> DeserializeStats(string json, (string, string[] Items)[] categories)
    {
        Dictionary<string, int> stored = [];
        try
        {
            stored = JsonSerializer.Deserialize<Dictionary<string, int>>(json) ?? [];
        }
        catch (JsonException)
        {
        }

        Dictionary<string, int> result = [];
        foreach ((_, string[] items) in categories)
        {
            foreach (string key in items)
            {
                result[key] = stored.TryGetValue(key, out int v) ? v : 2;
            }
        }

        return result;
    }
}
