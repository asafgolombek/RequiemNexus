using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.SeedData;
using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Data.Seeding;

/// <summary>
/// Shared sorcery rite catalog building and discipline gate helpers for <see cref="SorceryRiteSeeder"/> and
/// <see cref="BloodSorceryExtensionSeeder"/>.
/// </summary>
internal static class SorceryRiteSeedingHelper
{
    /// <summary>Default activation cost when no structured requirements are specified (Phase 9.5).</summary>
    internal const string DefaultRiteRequirementsJson = """[{"type":"InternalVitae","value":1,"isConsumed":true}]""";

    internal static (string CostDesc, string ReqJson) BuildSorceryCostForType(
        SorceryType type,
        int level,
        string? prerequisites = null)
    {
        string prereq = prerequisites ?? string.Empty;
        return type switch
        {
            SorceryType.Cruac => (
                $"{level} Vitae",
                $$"""[{"type":"InternalVitae","value":{{level}},"isConsumed":true}]"""),
            SorceryType.Theban => (
                "1 Willpower + Sacrament",
                BuildThebanRequirementsJson(prereq)),
            SorceryType.Necromancy => (
                "1 Vitae + focus",
                """[{"type":"MaterialFocus","value":1,"isConsumed":false},{"type":"InternalVitae","value":1,"isConsumed":true}]"""),
            _ => (
                "1 Vitae",
                DefaultRiteRequirementsJson),
        };
    }

    /// <summary>
    /// Builds Theban <c>RequirementsJson</c> with Willpower + PhysicalSacrament, copying sacrament narrative into <c>displayHint</c> when present.
    /// </summary>
    internal static string BuildThebanRequirementsJson(string? prerequisites)
    {
        string hint = ExtractThebanSacramentDisplayHint(prerequisites);
        object sacramentEntry = string.IsNullOrWhiteSpace(hint)
            ? new { type = "PhysicalSacrament", value = 1, isConsumed = true }
            : new { type = "PhysicalSacrament", value = 1, isConsumed = true, displayHint = hint };
        object[] rows =
        [
            new { type = "Willpower", value = 1, isConsumed = true },
            sacramentEntry,
        ];
        return JsonSerializer.Serialize(
            rows,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }

    internal static string ExtractThebanSacramentDisplayHint(string? prerequisites)
    {
        if (string.IsNullOrWhiteSpace(prerequisites))
        {
            return string.Empty;
        }

        const string prefix = "Sacrament:";
        int idx = prerequisites.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
        {
            return prerequisites.Trim();
        }

        return prerequisites[(idx + prefix.Length)..].Trim();
    }

    internal static string? BuildSorceryPoolJson(int disciplineId)
    {
        var traits = new List<object>
        {
            new { Type = 2, AttributeId = (int?)null, SkillId = (int?)null, DisciplineId = disciplineId, MinimumLevel = (int?)null },
            new { Type = 0, AttributeId = 0, SkillId = (int?)null, DisciplineId = (int?)null, MinimumLevel = (int?)null },
            new { Type = 1, AttributeId = (int?)null, SkillId = 5, DisciplineId = (int?)null, MinimumLevel = (int?)null },
        };
        return JsonSerializer.Serialize(new { Traits = traits });
    }

    internal static SorceryRiteDefinition BuildSorceryRiteFromCatalogEntry(
        SorceryRiteCatalogEntry entry,
        int? requiredCovenantId,
        int? requiredClanId,
        int disciplineId)
    {
        string? poolJson = BuildSorceryPoolJson(disciplineId);
        (string costDesc, string reqJson) = BuildSorceryCostForType(entry.SorceryType, entry.Rating, entry.Prerequisites);
        return new SorceryRiteDefinition
        {
            Name = entry.Name,
            Description = entry.Effect,
            Level = entry.Rating,
            SorceryType = entry.SorceryType,
            XpCost = entry.Rating,
            TargetSuccesses = entry.TargetSuccesses,
            PoolDefinitionJson = poolJson,
            ActivationCostDescription = costDesc,
            RequiredCovenantId = requiredCovenantId,
            RequiredClanId = requiredClanId,
            RequirementsJson = reqJson,
            Prerequisites = entry.Prerequisites,
            Effect = entry.Effect,
            RequiresElder = entry.RequiresElder,
        };
    }

    internal static async Task EnsureDisciplineExistsAsync(ApplicationDbContext context, string name, string description)
    {
        if (await context.Disciplines.AnyAsync(d => d.Name == name))
        {
            return;
        }

        context.Disciplines.Add(new Discipline { Name = name, Description = description });
        await context.SaveChangesAsync();
    }
}
