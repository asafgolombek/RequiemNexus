using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.DTOs;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Contracts;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Models;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Applies beat and XP adjustments with Masquerade ownership checks, beat ledger, and session broadcast.
/// </summary>
public class CharacterProgressionService(
    ApplicationDbContext dbContext,
    ICharacterCreationRules creationRules,
    IBeatLedgerService beatLedger,
    IAuthorizationHelper authHelper,
    ISessionService sessionService) : ICharacterProgressionService
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly ICharacterCreationRules _creationRules = creationRules;
    private readonly IBeatLedgerService _beatLedger = beatLedger;
    private readonly IAuthorizationHelper _authHelper = authHelper;
    private readonly ISessionService _sessionService = sessionService;

    /// <inheritdoc />
    public async Task<CharacterProgressionSnapshotDto> AddBeatAsync(int characterId, string userId)
    {
        await _authHelper.RequireCharacterOwnerAsync(characterId, userId, "add Beats");

        Character character = await _dbContext.Characters.FindAsync(characterId)
            ?? throw new InvalidOperationException($"Character {characterId} not found.");

        character.Beats++;

        await _beatLedger.RecordBeatAsync(
            character.Id,
            character.CampaignId,
            BeatSource.ManualAdjustment,
            "Beat added",
            userId);

        if (_creationRules.TryConvertBeats(character.Beats, out int newBeats, out int xpGained))
        {
            character.Beats = newBeats;
            character.ExperiencePoints += xpGained;
            character.TotalExperiencePoints += xpGained;

            await _beatLedger.RecordXpCreditAsync(
                character.Id,
                character.CampaignId,
                xpGained,
                XpSource.BeatConversion,
                $"Converted 5 Beats to {xpGained} XP",
                null);
        }

        await _dbContext.SaveChangesAsync();
        await _sessionService.BroadcastCharacterUpdateAsync(character.Id);
        return CharacterProgressionSnapshotDto.FromCharacter(character);
    }

    /// <inheritdoc />
    public async Task<CharacterProgressionSnapshotDto> RemoveBeatAsync(int characterId, string userId)
    {
        await _authHelper.RequireCharacterOwnerAsync(characterId, userId, "remove Beats");

        Character character = await _dbContext.Characters.FindAsync(characterId)
            ?? throw new InvalidOperationException($"Character {characterId} not found.");

        if (character.Beats > 0)
        {
            character.Beats--;
            await _dbContext.SaveChangesAsync();
            await _sessionService.BroadcastCharacterUpdateAsync(character.Id);
        }

        return CharacterProgressionSnapshotDto.FromCharacter(character);
    }

    /// <inheritdoc />
    public async Task<CharacterProgressionSnapshotDto> AddXPAsync(int characterId, string userId)
    {
        await _authHelper.RequireCharacterOwnerAsync(characterId, userId, "add XP");

        Character character = await _dbContext.Characters.FindAsync(characterId)
            ?? throw new InvalidOperationException($"Character {characterId} not found.");

        character.ExperiencePoints++;
        character.TotalExperiencePoints++;

        await _beatLedger.RecordXpCreditAsync(
            character.Id,
            character.CampaignId,
            1,
            XpSource.ManualAdjustment,
            "XP added manually",
            userId);

        await _dbContext.SaveChangesAsync();
        await _sessionService.BroadcastCharacterUpdateAsync(character.Id);
        return CharacterProgressionSnapshotDto.FromCharacter(character);
    }

    /// <inheritdoc />
    public async Task<CharacterProgressionSnapshotDto> RemoveXPAsync(int characterId, string userId)
    {
        await _authHelper.RequireCharacterOwnerAsync(characterId, userId, "remove XP");

        Character character = await _dbContext.Characters.FindAsync(characterId)
            ?? throw new InvalidOperationException($"Character {characterId} not found.");

        if (character.ExperiencePoints > 0)
        {
            character.ExperiencePoints--;
            if (character.TotalExperiencePoints > 0)
            {
                character.TotalExperiencePoints--;
            }

            await _beatLedger.RecordXpSpendAsync(
                character.Id,
                character.CampaignId,
                1,
                XpExpense.ManualAdjustment,
                "XP removed manually",
                userId);

            await _dbContext.SaveChangesAsync();
            await _sessionService.BroadcastCharacterUpdateAsync(character.Id);
        }

        return CharacterProgressionSnapshotDto.FromCharacter(character);
    }
}
