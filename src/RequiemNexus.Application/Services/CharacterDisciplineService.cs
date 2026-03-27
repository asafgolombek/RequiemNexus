using Microsoft.EntityFrameworkCore;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Contracts;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Application service for managing character discipline purchases, including XP deduction and ledger recording.
/// </summary>
public class CharacterDisciplineService(
    ApplicationDbContext dbContext,
    IBeatLedgerService beatLedger,
    IExperienceCostRules experienceCostRules) : ICharacterDisciplineService
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly IBeatLedgerService _beatLedger = beatLedger;
    private readonly IExperienceCostRules _experienceCostRules = experienceCostRules;

    /// <inheritdoc />
    public async Task<List<Discipline>> GetAvailableDisciplinesAsync()
    {
        return await _dbContext.Disciplines.OrderBy(d => d.Name).AsNoTracking().ToListAsync();
    }

    /// <inheritdoc />
    public async Task<CharacterDiscipline> AddDisciplineAsync(Character character, int disciplineId, int rating, string? userId)
    {
        bool isInClan = character.IsDisciplineInClan(disciplineId);
        int xpCost = _experienceCostRules.CalculateDisciplineUpgradeCost(0, rating, isInClan);

        if (character.ExperiencePoints < xpCost)
        {
            throw new InvalidOperationException($"Insufficient XP. Required: {xpCost}, Available: {character.ExperiencePoints}");
        }

        character.ExperiencePoints -= xpCost;

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
            RequiemNexus.Domain.Enums.XpExpense.Discipline,
            $"Purchased Discipline (Id={disciplineId}, rating={rating}, in-clan={isInClan})",
            userId);

        await _dbContext.SaveChangesAsync();
        return cd;
    }

    /// <inheritdoc />
    public async Task<bool> TryUpgradeDisciplineAsync(Character character, int characterDisciplineId, int newRating, string? userId)
    {
        CharacterDiscipline? cd = character.Disciplines.FirstOrDefault(d => d.Id == characterDisciplineId);
        if (cd == null || newRating <= cd.Rating)
        {
            return false;
        }

        bool isInClan = character.IsDisciplineInClan(cd.DisciplineId);
        int xpCost = _experienceCostRules.CalculateDisciplineUpgradeCost(cd.Rating, newRating, isInClan);

        if (character.ExperiencePoints >= xpCost)
        {
            character.ExperiencePoints -= xpCost;
            cd.Rating = newRating;

            await _beatLedger.RecordXpSpendAsync(
                character.Id,
                character.CampaignId,
                xpCost,
                RequiemNexus.Domain.Enums.XpExpense.Discipline,
                $"Upgraded Discipline {cd.Discipline?.Name ?? "Id=" + cd.DisciplineId} to {newRating} (in-clan={isInClan})",
                userId);

            await _dbContext.SaveChangesAsync();
            return true;
        }

        return false;
    }
}
