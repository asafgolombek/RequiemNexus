using Microsoft.EntityFrameworkCore;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Application service for managing character discipline purchases, including XP deduction and ledger recording.
/// </summary>
public class CharacterDisciplineService(
    ApplicationDbContext dbContext,
    IBeatLedgerService beatLedger) : ICharacterDisciplineService
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly IBeatLedgerService _beatLedger = beatLedger;

    /// <inheritdoc />
    public async Task<List<Discipline>> GetAvailableDisciplinesAsync()
    {
        return await _dbContext.Disciplines.OrderBy(d => d.Name).AsNoTracking().ToListAsync();
    }

    /// <inheritdoc />
    public async Task<CharacterDiscipline> AddDisciplineAsync(Character character, int disciplineId, int rating, int xpCost)
    {
        character.ExperiencePoints -= xpCost;

        // Rating is set directly (not via Upgrade()) because no XP deduction applies at creation time.
        CharacterDiscipline cd = new()
        {
            CharacterId = character.Id,
            DisciplineId = disciplineId,
            Rating = rating,
        };
        _dbContext.CharacterDisciplines.Add(cd);

        await _beatLedger.RecordXpSpendAsync(
            character.Id,
            character.CampaignId,
            xpCost,
            XpExpense.Discipline,
            $"Purchased Discipline (Id={disciplineId}, rating={rating})",
            null);

        await _dbContext.SaveChangesAsync();
        return cd;
    }
}
