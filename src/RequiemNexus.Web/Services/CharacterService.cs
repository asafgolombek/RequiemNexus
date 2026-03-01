using Microsoft.EntityFrameworkCore;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain;
using RequiemNexus.Web.Contracts;

namespace RequiemNexus.Web.Services;

public class CharacterService(ApplicationDbContext dbContext) : ICharacterService
{
    private readonly ApplicationDbContext _dbContext = dbContext;

    public async Task<List<Character>> GetCharactersByUserIdAsync(string userId)
    {
        return await _dbContext.Characters
            .Include(c => c.Clan)
            .Where(c => c.ApplicationUserId == userId)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Character?> GetCharacterByIdAsync(int id, string userId)
    {
        return await _dbContext.Characters
            .Include(c => c.Clan)
            .Include(c => c.Campaign)
            .Include(c => c.Merits).ThenInclude(m => m.Merit)
            .Include(c => c.Disciplines).ThenInclude(d => d.Discipline)
            .Include(c => c.CharacterEquipments).ThenInclude(ce => ce.Equipment)
            .FirstOrDefaultAsync(c => c.Id == id && c.ApplicationUserId == userId);
    }

    public async Task DeleteCharacterAsync(int id)
    {
        var entity = await _dbContext.Characters.FindAsync(id);
        if (entity != null)
        {
            _dbContext.Characters.Remove(entity);
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task<Character> EmbraceCharacterAsync(Character newCharacter)
    {
        // Delegate derived stat calculations to the pure Domain layer
        var (maxHealth, currentHealth) = CharacterCreationRules.CalculateInitialHealth(newCharacter.Size, newCharacter.Stamina);
        newCharacter.MaxHealth = maxHealth;
        newCharacter.CurrentHealth = currentHealth;

        var (maxWillpower, currentWillpower) = CharacterCreationRules.CalculateInitialWillpower(newCharacter.Resolve, newCharacter.Composure);
        newCharacter.MaxWillpower = maxWillpower;
        newCharacter.CurrentWillpower = currentWillpower;

        var (bp, maxVitae, currentVitae) = CharacterCreationRules.CalculateInitialBloodPotencyAndVitae();
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
        if (CharacterCreationRules.TryConvertBeats(character.Beats, out int newBeats, out int xpGained))
        {
            character.Beats = newBeats;
            character.ExperiencePoints += xpGained;
            character.TotalExperiencePoints += xpGained;
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
            Quantity = quantity
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
}
