using Microsoft.EntityFrameworkCore;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Application service for managing character merit purchases, including XP deduction and ledger recording.
/// </summary>
public class CharacterMeritService(
    ApplicationDbContext dbContext,
    IBeatLedgerService beatLedger) : ICharacterMeritService
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly IBeatLedgerService _beatLedger = beatLedger;

    /// <inheritdoc />
    public async Task<List<Merit>> GetAvailableMeritsAsync()
    {
        return await _dbContext.Merits.OrderBy(m => m.Name).AsNoTracking().ToListAsync();
    }

    /// <inheritdoc />
    public async Task<CharacterMerit> AddMeritAsync(Character character, int meritId, string? specification, int rating, int xpCost)
    {
        character.ExperiencePoints -= xpCost;

        // Rating is set directly (not via Upgrade()) because no XP deduction applies at creation time.
        CharacterMerit cm = new()
        {
            CharacterId = character.Id,
            MeritId = meritId,
            Specification = specification,
            Rating = rating,
        };
        _dbContext.CharacterMerits.Add(cm);

        await _beatLedger.RecordXpSpendAsync(
            character.Id,
            character.CampaignId,
            xpCost,
            XpExpense.Merit,
            $"Purchased Merit (Id={meritId}, rating={rating})",
            null);

        await _dbContext.SaveChangesAsync();
        return cm;
    }
}
