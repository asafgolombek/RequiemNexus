using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data;
using RequiemNexus.Data.Models.Enums;
using RequiemNexus.Domain.Models;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Aggregates passive modifiers from all active sources (Coils, Devotions, Covenant benefits).
/// Modifiers are never applied permanently; derived values are computed on demand.
/// </summary>
public class ModifierService(ApplicationDbContext dbContext, ILogger<ModifierService> logger) : IModifierService
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    /// <inheritdoc />
    public async Task<IReadOnlyList<PassiveModifier>> GetModifiersForCharacterAsync(int characterId)
    {
        var modifiers = new List<PassiveModifier>();

        // Aggregate from approved CharacterCoils (Ordo Dracul)
        var approvedCoils = await dbContext.CharacterCoils
            .AsNoTracking()
            .Include(cc => cc.CoilDefinition)
            .Where(cc => cc.CharacterId == characterId && cc.Status == CoilLearnStatus.Approved)
            .ToListAsync();

        foreach (var cc in approvedCoils)
        {
            if (cc.CoilDefinition?.ModifiersJson is not { } json || string.IsNullOrEmpty(json))
            {
                continue;
            }

            try
            {
                var coilModifiers = JsonSerializer.Deserialize<List<PassiveModifier>>(json, _jsonOptions);
                if (coilModifiers != null)
                {
                    modifiers.AddRange(coilModifiers);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Malformed ModifiersJson on CoilDefinition {CoilId}; modifier skipped.",
                    cc.CoilDefinition.Id);
            }
        }

        return modifiers.AsReadOnly();
    }
}
