using Microsoft.EntityFrameworkCore;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.DTOs;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.RealTime;
using RequiemNexus.Domain;
using RequiemNexus.Domain.Contracts;
using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Application.Services;

public class CharacterManagementService(
    ApplicationDbContext dbContext,
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    ICharacterCreationRules creationRules,
    IBeatLedgerService beatLedger,
    ISessionService sessionService) : ICharacterService
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory = dbContextFactory;
    private readonly ICharacterCreationRules _creationRules = creationRules;
    private readonly IBeatLedgerService _beatLedger = beatLedger;

    /// <inheritdoc />
    public async Task<List<Character>> GetCharactersByUserIdAsync(string userId)
    {
        // Factory creates a fresh context per call so Blazor Server's concurrent prerender
        // and interactive renders do not share a single DbContext instance.
        await using ApplicationDbContext ctx = _dbContextFactory.CreateDbContext();
        return await ctx.Characters
            .Include(c => c.Clan)
            .Where(c => c.ApplicationUserId == userId && !c.IsArchived)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <summary>Returns the archived characters owned by the given user.</summary>
    public async Task<List<Character>> GetArchivedCharactersAsync(string userId)
    {
        await using ApplicationDbContext ctx = _dbContextFactory.CreateDbContext();
        return await ctx.Characters
            .Include(c => c.Clan)
            .Where(c => c.ApplicationUserId == userId && c.IsArchived)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Character?> GetCharacterByIdAsync(int id, string userId)
    {
        // Intentionally NOT AsNoTracking: this method is the edit path — the returned entity
        // is tracked by EF so that subsequent mutations (AddBeatAsync, SaveAsync, etc.) persist.
        return await _dbContext.Characters
            .Include(c => c.Clan)
            .Include(c => c.Campaign)
            .Include(c => c.Attributes)
            .Include(c => c.Skills)
            .Include(c => c.Merits).ThenInclude(m => m.Merit)
            .Include(c => c.Disciplines).ThenInclude(d => d.Discipline!).ThenInclude(d => d.Powers)
            .Include(c => c.CharacterEquipments).ThenInclude(ce => ce.Equipment)
            .FirstOrDefaultAsync(c => c.Id == id && c.ApplicationUserId == userId);
    }

    public async Task DeleteCharacterAsync(int id, string userId)
    {
        Character? entity = await _dbContext.Characters.FindAsync(id);
        if (entity != null)
        {
            if (entity.ApplicationUserId != userId)
            {
                throw new UnauthorizedAccessException("Only the character owner may delete this character.");
            }

            // Null out CampaignId first so the campaign roster stays consistent.
            entity.CampaignId = null;
            _dbContext.Characters.Remove(entity);
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task<Character> EmbraceCharacterAsync(Character newCharacter)
    {
        // Delegate derived stat calculations to the pure Domain layer
        int stamina = newCharacter.GetAttributeRating(AttributeId.Stamina);
        int resolve = newCharacter.GetAttributeRating(AttributeId.Resolve);
        int composure = newCharacter.GetAttributeRating(AttributeId.Composure);

        var (maxHealth, currentHealth) = _creationRules.CalculateInitialHealth(newCharacter.Size, stamina);
        newCharacter.MaxHealth = maxHealth;
        newCharacter.CurrentHealth = currentHealth;

        var (maxWillpower, currentWillpower) = _creationRules.CalculateInitialWillpower(resolve, composure);
        newCharacter.MaxWillpower = maxWillpower;
        newCharacter.CurrentWillpower = currentWillpower;

        var (bp, maxVitae, currentVitae) = _creationRules.CalculateInitialBloodPotencyAndVitae();
        newCharacter.BloodPotency = bp;
        newCharacter.MaxVitae = maxVitae;
        newCharacter.CurrentVitae = currentVitae;

        _dbContext.Characters.Add(newCharacter);
        await _dbContext.SaveChangesAsync();

        return newCharacter;
    }

    public async Task SaveAsync(Character character)
    {
        await _dbContext.SaveChangesAsync();
        await sessionService.BroadcastCharacterUpdateAsync(character.Id);
    }

    public async Task AddBeatAsync(Character character)
    {
        character.Beats++;

        await _beatLedger.RecordBeatAsync(
            character.Id,
            character.CampaignId,
            BeatSource.ManualAdjustment,
            "Beat added",
            null);

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
        await sessionService.BroadcastCharacterUpdateAsync(character.Id);
    }

    public async Task RemoveBeatAsync(Character character)
    {
        if (character.Beats > 0)
        {
            character.Beats--;
            await _dbContext.SaveChangesAsync();
            await sessionService.BroadcastCharacterUpdateAsync(character.Id);
        }
    }

    public async Task AddXPAsync(Character character)
    {
        character.ExperiencePoints++;
        character.TotalExperiencePoints++;

        await _beatLedger.RecordXpCreditAsync(
            character.Id,
            character.CampaignId,
            1,
            XpSource.ManualAdjustment,
            "XP added manually",
            null);

        await _dbContext.SaveChangesAsync();
        await sessionService.BroadcastCharacterUpdateAsync(character.Id);
    }

    public async Task RemoveXPAsync(Character character)
    {
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
                null);

            await _dbContext.SaveChangesAsync();
            await sessionService.BroadcastCharacterUpdateAsync(character.Id);
        }
    }

    /// <inheritdoc />
    public async Task<(Character Character, bool IsOwner)?> GetCharacterWithAccessCheckAsync(int characterId, string requestingUserId)
    {
        Character? character = await _dbContext.Characters
            .Include(c => c.Clan)
            .Include(c => c.Campaign)
            .Include(c => c.Attributes)
            .Include(c => c.Skills)
            .Include(c => c.Merits).ThenInclude(m => m.Merit)
            .Include(c => c.Disciplines).ThenInclude(d => d.Discipline!).ThenInclude(d => d.Powers)
            .Include(c => c.CharacterEquipments).ThenInclude(ce => ce.Equipment)
            .Include(c => c.Aspirations)
            .Include(c => c.Banes)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == characterId);

        if (character == null)
        {
            return null;
        }

        // Owner: full edit access.
        if (character.ApplicationUserId == requestingUserId)
        {
            return (character, true);
        }

        // Campaign member (Storyteller or fellow player): read-only access.
        if (character.CampaignId.HasValue)
        {
            bool isMember = await _dbContext.Campaigns
                .AnyAsync(c => c.Id == character.CampaignId
                    && (c.StoryTellerId == requestingUserId
                        || c.Characters.Any(ch => ch.ApplicationUserId == requestingUserId)));

            if (isMember)
            {
                return (character, false);
            }
        }

        // No access.
        return null;
    }

    /// <inheritdoc />
    public async Task RetireCharacterAsync(int characterId, string userId)
    {
        Character character = await _dbContext.Characters
            .Include(c => c.Campaign)
            .FirstOrDefaultAsync(c => c.Id == characterId)
            ?? throw new InvalidOperationException($"Character {characterId} not found.");

        bool isOwner = character.ApplicationUserId == userId;
        bool isCampaignSt = character.Campaign?.StoryTellerId == userId;

        if (!isOwner && !isCampaignSt)
        {
            throw new UnauthorizedAccessException("Only the character owner or campaign Storyteller may retire a character.");
        }

        character.IsRetired = true;
        character.RetiredAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
        await sessionService.BroadcastCharacterUpdateAsync(character.Id);
    }

    /// <inheritdoc />
    public async Task UnretireCharacterAsync(int characterId, string userId)
    {
        Character character = await _dbContext.Characters
            .Include(c => c.Campaign)
            .FirstOrDefaultAsync(c => c.Id == characterId)
            ?? throw new InvalidOperationException($"Character {characterId} not found.");

        bool isOwner = character.ApplicationUserId == userId;
        bool isCampaignSt = character.Campaign?.StoryTellerId == userId;

        if (!isOwner && !isCampaignSt)
        {
            throw new UnauthorizedAccessException("Only the character owner or campaign Storyteller may un-retire a character.");
        }

        character.IsRetired = false;
        character.RetiredAt = null;
        await _dbContext.SaveChangesAsync();
        await sessionService.BroadcastCharacterUpdateAsync(character.Id);
    }

    /// <inheritdoc />
    public async Task ArchiveCharacterAsync(int characterId, string userId)
    {
        Character character = await _dbContext.Characters.FindAsync(characterId)
            ?? throw new InvalidOperationException($"Character {characterId} not found.");

        if (character.ApplicationUserId != userId)
        {
            throw new UnauthorizedAccessException("Only the character owner may archive a character.");
        }

        character.IsArchived = true;
        character.ArchivedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
        await sessionService.BroadcastCharacterUpdateAsync(character.Id);
    }

    /// <inheritdoc />
    public async Task UnarchiveCharacterAsync(int characterId, string userId)
    {
        Character character = await _dbContext.Characters.FindAsync(characterId)
            ?? throw new InvalidOperationException($"Character {characterId} not found.");

        if (character.ApplicationUserId != userId)
        {
            throw new UnauthorizedAccessException("Only the character owner may un-archive a character.");
        }

        character.IsArchived = false;
        character.ArchivedAt = null;
        await _dbContext.SaveChangesAsync();
        await sessionService.BroadcastCharacterUpdateAsync(character.Id);
    }
}
