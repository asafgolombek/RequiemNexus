using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.Models.Enums;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Models;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Passive modifiers deserialized from approved <see cref="CharacterCoil"/> <c>CoilDefinition.ModifiersJson</c>.
/// </summary>
public sealed class CoilModifierProvider(
    ApplicationDbContext dbContext,
    ILogger<CoilModifierProvider> logger) : IModifierProvider
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly ILogger<CoilModifierProvider> _logger = logger;

    /// <inheritdoc />
    public int Order => 20;

    /// <inheritdoc />
    public ModifierSourceType SourceType => ModifierSourceType.Coil;

    /// <inheritdoc />
    public async Task<IReadOnlyList<PassiveModifier>> GetModifiersAsync(int characterId, CancellationToken cancellationToken = default)
    {
        var approvedCoils = await _dbContext.CharacterCoils
            .AsNoTracking()
            .Include(cc => cc.CoilDefinition)
            .Where(cc => cc.CharacterId == characterId && cc.Status == CoilLearnStatus.Approved)
            .ToListAsync(cancellationToken);

        var modifiers = new List<PassiveModifier>();

        foreach (CharacterCoil cc in approvedCoils)
        {
            if (cc.CoilDefinition?.ModifiersJson is not { } json || string.IsNullOrEmpty(json))
            {
                continue;
            }

            try
            {
                List<PassiveModifier>? coilModifiers = JsonSerializer.Deserialize<List<PassiveModifier>>(json, PassiveModifierJsonSerializerOptions.Options);
                if (coilModifiers != null)
                {
                    modifiers.AddRange(coilModifiers);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Malformed ModifiersJson on CoilDefinition {CoilId}; modifier skipped.",
                    cc.CoilDefinition.Id);
            }
        }

        return modifiers;
    }
}
