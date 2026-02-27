using Microsoft.EntityFrameworkCore;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
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

    public async Task<Character?> GetCharacterByIdAsync(int id)
    {
        return await _dbContext.Characters
            .Include(c => c.Clan)
            .Include(c => c.Merits)
            .ThenInclude(m => m.Merit)
            .Include(c => c.Disciplines)
            .ThenInclude(d => d.Discipline)
            .Include(c => c.CharacterEquipments)
            .ThenInclude(ce => ce.Equipment)
            .FirstOrDefaultAsync(c => c.Id == id);
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
        var (maxHealth, currentHealth) = RequiemNexus.Domain.CharacterCreationRules.CalculateInitialHealth(newCharacter.Size, newCharacter.Stamina);
        newCharacter.MaxHealth = maxHealth;
        newCharacter.CurrentHealth = currentHealth;
        
        var (maxWillpower, currentWillpower) = RequiemNexus.Domain.CharacterCreationRules.CalculateInitialWillpower(newCharacter.Resolve, newCharacter.Composure);
        newCharacter.MaxWillpower = maxWillpower;
        newCharacter.CurrentWillpower = currentWillpower;
        
        var (bp, maxVitae, currentVitae) = RequiemNexus.Domain.CharacterCreationRules.CalculateInitialBloodPotencyAndVitae();
        newCharacter.BloodPotency = bp;
        newCharacter.MaxVitae = maxVitae;
        newCharacter.CurrentVitae = currentVitae;

        _dbContext.Characters.Add(newCharacter);
        await _dbContext.SaveChangesAsync();
        
        return newCharacter;
    }
}
