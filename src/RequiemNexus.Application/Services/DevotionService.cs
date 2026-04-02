using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Application service for purchasing and validating devotions.
/// </summary>
public class DevotionService(
    ApplicationDbContext dbContext,
    IBeatLedgerService beatLedger,
    IReferenceDataCache referenceData,
    ILogger<DevotionService> logger) : IDevotionService
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly IBeatLedgerService _beatLedger = beatLedger;
    private readonly IReferenceDataCache _referenceData = referenceData;
    private readonly ILogger<DevotionService> _logger = logger;

    /// <inheritdoc />
    public Task<List<DevotionDefinition>> GetAllDevotionsAsync()
    {
        List<DevotionDefinition> list = _referenceData.DevotionDefinitions
            .OrderBy(d => d.Name)
            .ToList();
        return Task.FromResult(list);
    }

    /// <inheritdoc />
    public Task<List<DevotionDefinition>> GetEligibleDevotionsAsync(Character character)
    {
        var ownedDevotionIds = character.Devotions.Select(d => d.DevotionDefinitionId).ToHashSet();

        List<DevotionDefinition> allDevotions = _referenceData.DevotionDefinitions.ToList();

        List<DevotionDefinition> result = allDevotions
            .Where(d => !ownedDevotionIds.Contains(d.Id) && MeetsPrerequisites(character, d))
            .OrderBy(d => d.Name)
            .ToList();
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public async Task<CharacterDevotion> PurchaseDevotionAsync(Character character, int devotionDefinitionId, string? userId)
    {
        DevotionDefinition? devotion = _referenceData.DevotionDefinitions.FirstOrDefault(d => d.Id == devotionDefinitionId)
            ?? throw new InvalidOperationException($"Devotion {devotionDefinitionId} not found.");

        if (character.Devotions.Any(d => d.DevotionDefinitionId == devotionDefinitionId))
        {
            throw new InvalidOperationException("Character already possesses this devotion.");
        }

        if (!MeetsPrerequisites(character, devotion))
        {
            throw new InvalidOperationException("Character does not meet prerequisites for this devotion.");
        }

        if (character.ExperiencePoints < devotion.XpCost)
        {
            throw new InvalidOperationException($"Insufficient XP. Required: {devotion.XpCost}, Available: {character.ExperiencePoints}");
        }

        character.ExperiencePoints -= devotion.XpCost;

        var cd = new CharacterDevotion
        {
            CharacterId = character.Id,
            DevotionDefinitionId = devotionDefinitionId,
        };
        _dbContext.CharacterDevotions.Add(cd);

        await _beatLedger.RecordXpSpendAsync(
            character.Id,
            character.CampaignId,
            devotion.XpCost,
            XpExpense.Devotion,
            $"Purchased Devotion: {devotion.Name}",
            userId);

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Character {CharacterId} purchased devotion {DevotionName}", character.Id, devotion.Name);

        return cd;
    }

    /// <inheritdoc />
    public bool MeetsPrerequisites(Character character, DevotionDefinition devotion)
    {
        // Check Bloodline prerequisite
        if (devotion.RequiredBloodlineId.HasValue)
        {
            bool hasBloodline = character.Bloodlines.Any(b => b.BloodlineDefinitionId == devotion.RequiredBloodlineId.Value && b.Status == RequiemNexus.Data.Models.Enums.BloodlineStatus.Active);
            if (!hasBloodline)
            {
                return false;
            }
        }

        // Check Discipline prerequisites
        if (!devotion.Prerequisites.Any())
        {
            return true;
        }

        // Prerequisite logic: satisfy ALL prerequisites within AT LEAST ONE OrGroupId.
        var groups = devotion.Prerequisites.GroupBy(p => p.OrGroupId);

        foreach (var group in groups)
        {
            bool groupSatisfied = true;
            foreach (var prereq in group)
            {
                int rating = character.GetDisciplineRating(prereq.DisciplineId);
                if (rating < prereq.MinimumLevel)
                {
                    groupSatisfied = false;
                    break;
                }
            }

            if (groupSatisfied)
            {
                return true;
            }
        }

        return false;
    }
}
