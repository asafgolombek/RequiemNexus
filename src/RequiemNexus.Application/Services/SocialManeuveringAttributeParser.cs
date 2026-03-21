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
        return (
            ChronicleNpcTraitJsonReader.ReadTraitRating("Resolve", isAttribute: true, attributesJson, null),
            ChronicleNpcTraitJsonReader.ReadTraitRating("Composure", isAttribute: true, attributesJson, null));
    }

    /// <summary>
    /// Parses Wits and Composure from JSON; defaults to 2 when missing or invalid (initiative = sum).
    /// </summary>
    public static (int Wits, int Composure) ReadWitsComposure(string? attributesJson)
    {
        return (
            ChronicleNpcTraitJsonReader.ReadTraitRating("Wits", isAttribute: true, attributesJson, null),
            ChronicleNpcTraitJsonReader.ReadTraitRating("Composure", isAttribute: true, attributesJson, null));
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
}
