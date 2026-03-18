using Microsoft.EntityFrameworkCore;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Application service for managing character merit purchases, including XP deduction and ledger recording.
/// Enforces covenant gating and status-per-org rules for covenant-specific merits.
/// </summary>
public class CharacterMeritService(
    ApplicationDbContext dbContext,
    IBeatLedgerService beatLedger) : ICharacterMeritService
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly IBeatLedgerService _beatLedger = beatLedger;

    /// <inheritdoc />
    public async Task<List<Merit>> GetAvailableMeritsAsync(Character character)
    {
        var covenantGatedMeritIds = await _dbContext.CovenantDefinitionMerits
            .Select(cdm => cdm.MeritId)
            .Distinct()
            .ToListAsync();

        var covenantMeritIdsByCovenant = await _dbContext.CovenantDefinitionMerits
            .Where(cdm => character.CovenantId != null && cdm.CovenantDefinitionId == character.CovenantId)
            .Select(cdm => cdm.MeritId)
            .ToListAsync();

        var allMerits = await _dbContext.Merits
            .OrderBy(m => m.Name)
            .AsNoTracking()
            .ToListAsync();

        return allMerits
            .Where(m =>
            {
                if (!covenantGatedMeritIds.Contains(m.Id))
                {
                    return true;
                }

                // null or Approved = active member; Pending = awaiting Storyteller approval
                var isApprovedMember = character.CovenantId.HasValue
                    && character.CovenantJoinStatus != Data.Models.Enums.CovenantJoinStatus.Pending;
                return isApprovedMember && covenantMeritIdsByCovenant.Contains(m.Id);
            })
            .ToList();
    }

    /// <inheritdoc />
    public async Task<CharacterMerit> AddMeritAsync(Character character, int meritId, string? specification, int rating, int xpCost)
    {
        var merit = await _dbContext.Merits.AsNoTracking().FirstOrDefaultAsync(m => m.Id == meritId)
            ?? throw new InvalidOperationException($"Merit with Id {meritId} not found.");

        var covenantLink = await _dbContext.CovenantDefinitionMerits
            .AsNoTracking()
            .FirstOrDefaultAsync(cdm => cdm.MeritId == meritId);

        if (covenantLink != null)
        {
            if (character.CovenantId != covenantLink.CovenantDefinitionId)
            {
                throw new InvalidOperationException(
                    $"This merit is only available to members of the linked covenant. Your character is not an approved member.");
            }

            if (character.CovenantJoinStatus == Data.Models.Enums.CovenantJoinStatus.Pending)
            {
                throw new InvalidOperationException(
                    "You must be an approved covenant member to purchase covenant-gated merits.");
            }

            if (IsStatusMerit(merit.Name))
            {
                var existingStatusMerits = await _dbContext.CharacterMerits
                    .Where(cm => cm.CharacterId == character.Id)
                    .Join(
                        _dbContext.Merits,
                        cm => cm.MeritId,
                        m => m.Id,
                        (cm, m) => m)
                    .Where(m => IsStatusMerit(m.Name) && m.Id != meritId)
                    .AnyAsync();

                if (existingStatusMerits)
                {
                    throw new InvalidOperationException(
                        "A character may hold Status in at most one organization. You already have a covenant Status merit.");
                }
            }
        }

        character.ExperiencePoints -= xpCost;

        var cm = new CharacterMerit
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
            "Purchased Merit (Id=" + meritId + ", rating=" + rating + ")",
            null);

        await _dbContext.SaveChangesAsync();
        return cm;
    }

    private static bool IsStatusMerit(string meritName) =>
        meritName.StartsWith("Status (", StringComparison.OrdinalIgnoreCase);
}
