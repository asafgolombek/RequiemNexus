using System.Text.Json;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Reads Resolve/Composure dots from chronicle NPC <c>AttributesJson</c> for Social maneuvering.
/// </summary>
internal static class SocialManeuveringAttributeParser
{
    /// <summary>
    /// Parses Resolve and Composure from JSON; defaults to 2 when missing or invalid.
    /// </summary>
    public static (int Resolve, int Composure) ReadResolveComposure(string? attributesJson)
    {
        const int fallback = 2;
        if (string.IsNullOrWhiteSpace(attributesJson))
        {
            return (fallback, fallback);
        }

        try
        {
            using JsonDocument doc = JsonDocument.Parse(attributesJson);
            JsonElement root = doc.RootElement;
            int resolve = ReadAttributeDot(root, "Resolve", fallback);
            int composure = ReadAttributeDot(root, "Composure", fallback);
            return (resolve, composure);
        }
        catch (JsonException)
        {
            return (fallback, fallback);
        }
    }

    private static int ReadAttributeDot(JsonElement root, string name, int fallback)
    {
        if (!root.TryGetProperty(name, out JsonElement el) || el.ValueKind != JsonValueKind.Number)
        {
            return fallback;
        }

        return el.TryGetInt32(out int v) ? Math.Clamp(v, 1, 5) : fallback;
    }
}
