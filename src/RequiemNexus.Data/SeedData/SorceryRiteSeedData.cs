using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using RequiemNexus.Data.Seeding;
using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Data.SeedData;

/// <summary>
/// Loads Crúac, Theban, and Kindred Necromancy rites from canonical <c>SeedSource/*.json</c> (unified catalog schema).
/// Falls back to a minimal inline set when primary files are not found.
/// </summary>
public static partial class SorceryRiteSeedData
{
    private const string _cruacCatalogFile = "cruac_rituales.json";
    private const string _thebanCatalogFile = "Theban_Sorcery_rituals.json";
    private const string _necromancyCatalogFile = "kindred_necromancy_rituals.json";

    /// <summary>
    /// Loads the full sorcery catalog for idempotent upserts (name-keyed).
    /// </summary>
    public static List<SorceryRiteCatalogEntry> LoadCatalogEntries(ILogger logger)
    {
        var result = new List<SorceryRiteCatalogEntry>();

        AppendFile(_cruacCatalogFile, SorceryType.Cruac, result, logger);
        AppendFile(_thebanCatalogFile, SorceryType.Theban, result, logger);
        AppendFile(_necromancyCatalogFile, SorceryType.Necromancy, result, logger);

        if (result.Count == 0)
        {
            foreach (var e in GetMinimalCruacEntries())
            {
                result.Add(e);
            }

            foreach (var e in GetMinimalThebanEntries())
            {
                result.Add(e);
            }
        }

        return result;
    }

    /// <summary>
    /// Loads rites from SeedSource JSON. Legacy tuple shape for <see cref="SorceryRiteSeeder"/>.
    /// </summary>
    public static List<(string Name, int Rating, string Prerequisites, string Effect, SorceryType SorceryType, int TargetSuccesses)> LoadFromDocs(ILogger logger)
    {
        return LoadCatalogEntries(logger)
            .Select(e => (e.Name, e.Rating, e.Prerequisites, e.Effect, e.SorceryType, e.TargetSuccesses))
            .ToList();
    }

    /// <summary>
    /// Fallback when seed JSON omits <c>TargetSuccesses</c>: keeps extended-action UI non-zero (audit P1-2).
    /// </summary>
    public static int DefaultTargetSuccessesForRating(int rating) => Math.Clamp(5 + rating, 1, 30);

    private static void AppendFile(string fileName, SorceryType sorceryType, List<SorceryRiteCatalogEntry> target, ILogger logger)
    {
        using JsonDocument? doc = SeedDataLoader.TryLoadJson(fileName, logger);
        if (doc == null)
        {
            logger.LogWarning("Sorcery catalog file {File} not found or invalid; tradition {Type} skipped.", fileName, sorceryType);
            return;
        }

        AppendArray(doc.RootElement, sorceryType, target);
    }

    private static void AppendArray(JsonElement root, SorceryType sorceryType, List<SorceryRiteCatalogEntry> target)
    {
        foreach (JsonElement el in root.EnumerateArray())
        {
            AppendCatalogElementsFromJsonObject(el, sorceryType, target);
        }
    }

    private static void AppendCatalogElementsFromJsonObject(JsonElement el, SorceryType sorceryType, List<SorceryRiteCatalogEntry> target)
    {
        string? name = GetStringProperty(el, "name");
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        string prerequisites = GetStringProperty(el, "Prerequisites") ?? string.Empty;
        string effect = GetStringProperty(el, "Effect", "effect") ?? string.Empty;
        string? rankingRaw = GetStringProperty(el, "Ranking", "ranking");
        string? targetSuccessesRaw = GetStringProperty(el, "Target Successes", "TargetSuccesses", "targetSuccesses");

        if (TryExpandDualRankVariant(name, rankingRaw, targetSuccessesRaw, prerequisites, effect, sorceryType, out List<SorceryRiteCatalogEntry>? dual)
            && dual != null)
        {
            target.AddRange(dual);
            return;
        }

        bool requiresElder = IsElderRankingToken(rankingRaw);
        int rating = requiresElder
            ? ExtractFirstIntOr(rankingRaw, defaultValue: 5)
            : ExtractFirstIntOr(rankingRaw, defaultValue: 1);
        if (requiresElder && rating < 1)
        {
            rating = 5;
        }

        int targetSuccesses = ParseTargetSuccesses(targetSuccessesRaw, rating);

        target.Add(new SorceryRiteCatalogEntry(
            name.Trim(),
            rating,
            prerequisites,
            effect,
            sorceryType,
            targetSuccesses,
            requiresElder));
    }

    /// <summary>
    /// Blandishment-style rows: two dot ratings and two target success counts in the same JSON object.
    /// </summary>
    private static bool TryExpandDualRankVariant(
        string baseName,
        string? rankingRaw,
        string? targetSuccessesRaw,
        string prerequisites,
        string effect,
        SorceryType sorceryType,
        out List<SorceryRiteCatalogEntry>? entries)
    {
        entries = null;
        if (string.IsNullOrWhiteSpace(rankingRaw)
            || string.IsNullOrWhiteSpace(targetSuccessesRaw)
            || !rankingRaw.Contains('(', StringComparison.Ordinal))
        {
            return false;
        }

        IReadOnlyList<int> rankInts = ExtractAllInts(rankingRaw);
        IReadOnlyList<int> tsInts = ExtractAllInts(targetSuccessesRaw);
        if (rankInts.Count < 2 || tsInts.Count < 2)
        {
            return false;
        }

        entries =
        [
            new SorceryRiteCatalogEntry(
                baseName.Trim(),
                rankInts[0],
                prerequisites,
                effect,
                sorceryType,
                tsInts[0],
                RequiresElder: false),
            new SorceryRiteCatalogEntry(
                $"{baseName.Trim()} (Aggravated)",
                rankInts[1],
                prerequisites,
                effect,
                sorceryType,
                tsInts[1],
                RequiresElder: false),
        ];
        return true;
    }

    private static bool IsElderRankingToken(string? rankingRaw)
    {
        if (string.IsNullOrWhiteSpace(rankingRaw))
        {
            return false;
        }

        string trimmed = rankingRaw.Trim();
        return trimmed.Equals("elder", StringComparison.OrdinalIgnoreCase);
    }

    private static int ParseTargetSuccesses(string? raw, int rating)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return DefaultTargetSuccessesForRating(rating);
        }

        if (raw.Contains("not detailed", StringComparison.OrdinalIgnoreCase))
        {
            return DefaultTargetSuccessesForRating(rating);
        }

        int? first = ExtractFirstInt(raw);
        return first is >= 1 ? first.Value : DefaultTargetSuccessesForRating(rating);
    }

    private static int ExtractFirstIntOr(string? raw, int defaultValue)
    {
        int? n = ExtractFirstInt(raw);
        return n ?? defaultValue;
    }

    private static int? ExtractFirstInt(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        Match m = FirstIntRegex().Match(raw);
        return m.Success && int.TryParse(m.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v)
            ? v
            : null;
    }

    private static IReadOnlyList<int> ExtractAllInts(string raw)
    {
        var list = new List<int>();
        foreach (Match m in FirstIntRegex().Matches(raw))
        {
            if (int.TryParse(m.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v))
            {
                list.Add(v);
            }
        }

        return list;
    }

    [GeneratedRegex(@"\d+")]
    private static partial Regex FirstIntRegex();

    private static string? GetStringProperty(JsonElement el, params string[] candidateNames)
    {
        foreach (JsonProperty p in el.EnumerateObject())
        {
            foreach (string n in candidateNames)
            {
                if (string.Equals(p.Name, n, StringComparison.OrdinalIgnoreCase))
                {
                    return p.Value.ValueKind == JsonValueKind.String ? p.Value.GetString() : p.Value.ToString();
                }
            }
        }

        return null;
    }

    private static IEnumerable<SorceryRiteCatalogEntry> GetMinimalCruacEntries()
    {
        yield return new SorceryRiteCatalogEntry(
            "Lair of the Beast",
            1,
            "Smear Vitae over a central point of the territory.",
            "Extends the vampire's Predatory Aura over the entire territory.",
            SorceryType.Cruac,
            6);
        yield return new SorceryRiteCatalogEntry(
            "Pangs of Proserpina",
            2,
            "Target must be within a mile.",
            "Inflicts intense hunger on a victim, provoking frenzy in vampires.",
            SorceryType.Cruac,
            6);
        yield return new SorceryRiteCatalogEntry(
            "The Hydra's Vitae",
            3,
            "None specified.",
            "Transforms the ritualist's own blood into a toxic poison.",
            SorceryType.Cruac,
            5);
    }

    private static IEnumerable<SorceryRiteCatalogEntry> GetMinimalThebanEntries()
    {
        yield return new SorceryRiteCatalogEntry(
            "Apple of Eden",
            1,
            "Sacrament: An apple, a drop of Vitae.",
            "Grants temporary Intelligence and Wits dots.",
            SorceryType.Theban,
            5);
        yield return new SorceryRiteCatalogEntry(
            "Blood Scourge",
            1,
            "Sacrament: The ritualist's own blood (at least one Vitae).",
            "Transforms blood into a stinging whip.",
            SorceryType.Theban,
            6);
        yield return new SorceryRiteCatalogEntry(
            "Marian Apparition",
            2,
            "Sacrament: A piece of pure white cloth.",
            "Creates an apparition of a holy figure.",
            SorceryType.Theban,
            6);
    }
}
