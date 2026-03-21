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

    /// <summary>
    /// Parses Wits and Composure from JSON; defaults to 2 when missing or invalid (initiative = sum).
    /// </summary>
    public static (int Wits, int Composure) ReadWitsComposure(string? attributesJson)
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
            int wits = ReadAttributeDot(root, "Wits", fallback);
            int composure = ReadAttributeDot(root, "Composure", fallback);
            return (wits, composure);
        }
        catch (JsonException)
        {
            return (fallback, fallback);
        }
    }

    /// <summary>
    /// Parses Blood Potency from JSON (keys <c>BloodPotency</c> or <c>Blood Potency</c>); defaults to 1 when missing or invalid.
    /// </summary>
    public static int ReadBloodPotency(string? attributesJson, int fallback = 1)
    {
        if (string.IsNullOrWhiteSpace(attributesJson))
        {
            return Math.Clamp(fallback, 1, 10);
        }

        try
        {
            using JsonDocument doc = JsonDocument.Parse(attributesJson);
            JsonElement root = doc.RootElement;
            int bp = ReadBloodPotencyDot(root, "BloodPotency", 0);
            if (bp < 1)
            {
                bp = ReadBloodPotencyDot(root, "Blood Potency", 0);
            }

            if (bp < 1)
            {
                bp = fallback;
            }

            return Math.Clamp(bp, 1, 10);
        }
        catch (JsonException)
        {
            return Math.Clamp(fallback, 1, 10);
        }
    }

    private static int ReadBloodPotencyDot(JsonElement root, string name, int fallback)
    {
        if (!root.TryGetProperty(name, out JsonElement el) || el.ValueKind != JsonValueKind.Number)
        {
            return fallback;
        }

        return el.TryGetInt32(out int v) ? v : fallback;
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
