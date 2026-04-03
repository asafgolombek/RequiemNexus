using Microsoft.EntityFrameworkCore;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Models;
using RequiemNexus.Domain.Services;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Wound-penalty dice derived from the character health track (Phase 14).
/// </summary>
public sealed class WoundTrackModifierProvider(ApplicationDbContext dbContext) : IModifierProvider
{
    private readonly ApplicationDbContext _dbContext = dbContext;

    /// <inheritdoc />
    public int Order => 30;

    /// <inheritdoc />
    public ModifierSourceType SourceType => ModifierSourceType.WoundTrack;

    /// <inheritdoc />
    public async Task<IReadOnlyList<PassiveModifier>> GetModifiersAsync(int characterId, CancellationToken cancellationToken = default)
    {
        var physAttribs = await _dbContext.CharacterAttributes
            .AsNoTracking()
            .Where(a => a.CharacterId == characterId
                     && (a.Name == nameof(AttributeId.Stamina)))
            .Select(a => new { a.Name, a.Rating })
            .ToDictionaryAsync(a => a.Name, cancellationToken);

        int staminaRating = physAttribs.TryGetValue(nameof(AttributeId.Stamina), out var sta) ? sta.Rating : 0;
        if (staminaRating <= 0)
        {
            staminaRating = 1;
        }

        var characterRow = await _dbContext.Characters
            .AsNoTracking()
            .Where(c => c.Id == characterId)
            .Select(c => new { c.Size, c.HealthDamage })
            .FirstOrDefaultAsync(cancellationToken);

        int sizeRating = characterRow?.Size ?? 0;
        if (sizeRating <= 0)
        {
            sizeRating = 5;
        }

        string healthDamage = characterRow?.HealthDamage ?? string.Empty;
        int maxHealthTrack = Math.Max(1, sizeRating + staminaRating);
        int woundPenaltyDice = WoundPenaltyResolver.GetWoundPenaltyDice(healthDamage, maxHealthTrack);
        if (woundPenaltyDice == 0)
        {
            return [];
        }

        PassiveModifier mod = new(
            ModifierTarget.WoundPenalty,
            woundPenaltyDice,
            ModifierType.Static,
            "Wound penalty",
            new ModifierSource(ModifierSourceType.WoundTrack, characterId));

        return [mod];
    }
}
