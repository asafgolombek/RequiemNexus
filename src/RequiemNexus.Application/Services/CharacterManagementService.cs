using Microsoft.EntityFrameworkCore;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain;
using RequiemNexus.Domain.Contracts;
using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Application.Services;

public class CharacterManagementService(
    ApplicationDbContext dbContext,
    ICharacterCreationRules creationRules,
    IBeatLedgerService beatLedger) : ICharacterService
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly ICharacterCreationRules _creationRules = creationRules;
    private readonly IBeatLedgerService _beatLedger = beatLedger;

    public async Task<List<Character>> GetCharactersByUserIdAsync(string userId)
    {
        return await _dbContext.Characters
            .Include(c => c.Clan)
            .Where(c => c.ApplicationUserId == userId && !c.IsArchived)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <summary>Returns the archived characters owned by the given user.</summary>
    public async Task<List<Character>> GetArchivedCharactersAsync(string userId)
    {
        return await _dbContext.Characters
            .Include(c => c.Clan)
            .Where(c => c.ApplicationUserId == userId && c.IsArchived)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Character?> GetCharacterByIdAsync(int id, string userId)
    {
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

    public async Task DeleteCharacterAsync(int id)
    {
        Character? entity = await _dbContext.Characters.FindAsync(id);
        if (entity != null)
        {
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
    }

    public async Task RemoveBeatAsync(Character character)
    {
        if (character.Beats > 0)
        {
            character.Beats--;
            await _dbContext.SaveChangesAsync();
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
        }
    }

    public async Task<List<Equipment>> GetAvailableEquipmentAsync()
    {
        return await _dbContext.Equipment.OrderBy(e => e.Name).ToListAsync();
    }

    public async Task<CharacterEquipment> AddEquipmentAsync(int characterId, int equipmentId, int quantity)
    {
        var ce = new CharacterEquipment
        {
            CharacterId = characterId,
            EquipmentId = equipmentId,
            Quantity = quantity,
        };
        _dbContext.CharacterEquipments.Add(ce);
        await _dbContext.SaveChangesAsync();
        return ce;
    }

    public async Task RemoveEquipmentAsync(int characterEquipmentId)
    {
        var ce = await _dbContext.CharacterEquipments.FindAsync(characterEquipmentId);
        if (ce != null)
        {
            _dbContext.CharacterEquipments.Remove(ce);
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task<List<Merit>> GetAvailableMeritsAsync()
    {
        return await _dbContext.Merits.OrderBy(m => m.Name).ToListAsync();
    }

    public async Task<CharacterMerit> AddMeritAsync(Character character, int meritId, string? specification, int rating, int xpCost)
    {
        character.ExperiencePoints -= xpCost;

        CharacterMerit cm = new()
        {
            CharacterId = character.Id,
            MeritId = meritId,
            Specification = specification,
        };
        typeof(CharacterMerit).GetProperty("Rating")?.SetValue(cm, rating);
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

    public async Task<List<Discipline>> GetAvailableDisciplinesAsync()
    {
        return await _dbContext.Disciplines.OrderBy(d => d.Name).ToListAsync();
    }

    public async Task<CharacterDiscipline> AddDisciplineAsync(Character character, int disciplineId, int rating, int xpCost)
    {
        character.ExperiencePoints -= xpCost;

        CharacterDiscipline cd = new()
        {
            CharacterId = character.Id,
            DisciplineId = disciplineId,
        };
        typeof(CharacterDiscipline).GetProperty("Rating")?.SetValue(cd, rating);
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
    }

    /// <inheritdoc />
    public async Task<List<DiceMacro>> GetDiceMacrosAsync(int characterId)
    {
        return await _dbContext.DiceMacros
            .Where(m => m.CharacterId == characterId)
            .OrderBy(m => m.Name)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<DiceMacro> CreateDiceMacroAsync(int characterId, string name, int dicePool, string description, string userId)
    {
        Character character = await _dbContext.Characters.FindAsync(characterId)
            ?? throw new InvalidOperationException($"Character {characterId} not found.");

        if (character.ApplicationUserId != userId)
        {
            throw new UnauthorizedAccessException("Only the character owner may create dice macros.");
        }

        DiceMacro macro = new()
        {
            CharacterId = characterId,
            Name = name,
            DicePool = dicePool,
            Description = description,
        };

        _dbContext.DiceMacros.Add(macro);
        await _dbContext.SaveChangesAsync();
        return macro;
    }

    /// <inheritdoc />
    public async Task DeleteDiceMacroAsync(int macroId, string userId)
    {
        DiceMacro macro = await _dbContext.DiceMacros
            .Include(m => m.Character)
            .FirstOrDefaultAsync(m => m.Id == macroId)
            ?? throw new InvalidOperationException($"Macro {macroId} not found.");

        if (macro.Character?.ApplicationUserId != userId)
        {
            throw new UnauthorizedAccessException("Only the character owner may delete dice macros.");
        }

        _dbContext.DiceMacros.Remove(macro);
        await _dbContext.SaveChangesAsync();
    }
}
