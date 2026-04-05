using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.SeedData;

namespace RequiemNexus.Data.Seeding;

/// <summary>
/// Second-pass sync: applies Phase 19 acquisition metadata from <c>Disciplines.json</c> onto non-homebrew rows.
/// </summary>
public sealed class DisciplineAcquisitionMetadataSeeder : ISeeder
{
    /// <inheritdoc />
    public int Order => 90;

    /// <inheritdoc />
    public async Task SeedAsync(ApplicationDbContext context, ILogger logger)
    {
        using JsonDocument? doc = SeedDataLoader.TryLoadJson("Disciplines.json", logger);
        if (doc == null)
        {
            return;
        }

        Dictionary<string, int> covenantByName = await context.CovenantDefinitions
            .AsNoTracking()
            .ToDictionaryAsync(c => c.Name, c => c.Id, StringComparer.OrdinalIgnoreCase);

        Dictionary<string, int> bloodlineByName = await context.BloodlineDefinitions
            .AsNoTracking()
            .ToDictionaryAsync(b => b.Name, b => b.Id, StringComparer.OrdinalIgnoreCase);

        List<Discipline> disciplineRows = await context.Disciplines
            .Include(d => d.Powers)
            .Where(d => !d.IsHomebrew)
            .ToListAsync();
        Dictionary<string, Discipline> disciplineByName = disciplineRows
            .GroupBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        foreach (JsonElement el in doc.RootElement.EnumerateArray())
        {
            string name = el.TryGetProperty("name", out var n) ? n.GetString() ?? string.Empty : string.Empty;
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            if (!disciplineByName.TryGetValue(name, out Discipline? discipline))
            {
                continue;
            }

            discipline.CanLearnIndependently = ReadBool(el, "canLearnIndependently");
            discipline.RequiresMentorBloodToLearn = ReadBool(el, "requiresMentorBloodToLearn");
            discipline.IsCovenantDiscipline = ReadBool(el, "isCovenantDiscipline");
            discipline.IsBloodlineDiscipline = ReadBool(el, "isBloodlineDiscipline");
            discipline.IsNecromancy = ReadBool(el, "isNecromancy");

            discipline.CovenantId = el.TryGetProperty("covenantName", out var cov) &&
                cov.ValueKind == JsonValueKind.String &&
                covenantByName.TryGetValue(cov.GetString()!, out int cId)
                    ? cId
                    : null;

            discipline.BloodlineId = el.TryGetProperty("bloodlineName", out var bl) &&
                bl.ValueKind == JsonValueKind.String &&
                bloodlineByName.TryGetValue(bl.GetString()!, out int bId)
                    ? bId
                    : null;

            if (el.TryGetProperty("powers", out var powers))
            {
                foreach (JsonElement p in powers.EnumerateArray())
                {
                    string powerName = p.TryGetProperty("name", out var pn) ? pn.GetString() ?? string.Empty : string.Empty;
                    if (string.IsNullOrWhiteSpace(powerName))
                    {
                        continue;
                    }

                    DisciplinePower? power = discipline.Powers
                        .FirstOrDefault(pw => string.Equals(pw.Name, powerName, StringComparison.OrdinalIgnoreCase));
                    if (power == null)
                    {
                        continue;
                    }

                    string? poolJson = p.TryGetProperty("poolDefinitionJson", out var pj) &&
                        pj.ValueKind != JsonValueKind.Null
                            ? pj.GetString()
                            : null;
                    power.PoolDefinitionJson = NormalizePoolDefinitionJson(poolJson, discipline.Id);
                }
            }
        }

        if (context.ChangeTracker.HasChanges())
        {
            await context.SaveChangesAsync();
        }
    }

    private static bool ReadBool(JsonElement el, string propertyName) =>
        el.TryGetProperty(propertyName, out var p) && p.ValueKind == JsonValueKind.True;

    private static string? NormalizePoolDefinitionJson(string? json, int disciplineId)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            JsonNode? root = JsonNode.Parse(json);
            if (root?["traits"] is not JsonArray arr)
            {
                return json;
            }

            bool changed = false;
            foreach (JsonNode? node in arr)
            {
                if (node is JsonObject obj
                    && obj["type"] is JsonValue typeVal
                    && string.Equals(typeVal.GetValue<string>(), "Discipline", StringComparison.OrdinalIgnoreCase)
                    && obj["disciplineId"] is JsonValue did
                    && did.TryGetValue<int>(out int dInt)
                    && dInt == 0)
                {
                    obj["disciplineId"] = disciplineId;
                    changed = true;
                }
            }

            return changed ? root.ToJsonString() : json;
        }
        catch (JsonException)
        {
            return json;
        }
    }
}
