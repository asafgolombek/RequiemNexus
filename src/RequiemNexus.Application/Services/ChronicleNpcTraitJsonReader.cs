using System.Text.Json;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Reads individual attribute or skill dots from a chronicle NPC <c>AttributesJson</c> / <c>SkillsJson</c> blob.
/// Matches Danse Macabre defaults: attributes 1–5 (missing → 2); skills 0–5 (missing → 2).
/// </summary>
public static class ChronicleNpcTraitJsonReader
{
    private const int _attributeFallback = 2;
    private const int _skillFallback = 2;

    /// <summary>
    /// Reads one trait rating from the appropriate JSON object.
    /// </summary>
    /// <param name="traitName">Enum key (e.g. <c>Wits</c>, <c>Stealth</c>).</param>
    /// <param name="isAttribute">True for attributes; false for skills.</param>
    /// <param name="attributesJson">Chronicle NPC attributes JSON.</param>
    /// <param name="skillsJson">Chronicle NPC skills JSON.</param>
    /// <returns>Clamped dot value.</returns>
    public static int ReadTraitRating(
        string traitName,
        bool isAttribute,
        string? attributesJson,
        string? skillsJson)
    {
        string? json = isAttribute ? attributesJson : skillsJson;
        if (string.IsNullOrWhiteSpace(json))
        {
            return isAttribute ? _attributeFallback : _skillFallback;
        }

        try
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;
            return isAttribute
                ? ReadAttributeDot(root, traitName, _attributeFallback)
                : ReadSkillDot(root, traitName, _skillFallback);
        }
        catch (JsonException)
        {
            return isAttribute ? _attributeFallback : _skillFallback;
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

    private static int ReadSkillDot(JsonElement root, string name, int fallback)
    {
        if (!root.TryGetProperty(name, out JsonElement el) || el.ValueKind != JsonValueKind.Number)
        {
            return fallback;
        }

        return el.TryGetInt32(out int v) ? Math.Clamp(v, 0, 5) : fallback;
    }
}
