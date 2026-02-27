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
        // Calculate derived stats based on Vampire: The Requiem rules - SRP logic correctly placed inside the business layer.
        newCharacter.MaxHealth = newCharacter.Size + newCharacter.Stamina;
        newCharacter.CurrentHealth = newCharacter.MaxHealth;
        
        newCharacter.MaxWillpower = newCharacter.Resolve + newCharacter.Composure;
        newCharacter.CurrentWillpower = newCharacter.MaxWillpower;
        
        // Assuming neonate starts with BP 1, which gives 10 Vitae max. Start with 10.
        newCharacter.BloodPotency = 1;
        newCharacter.MaxVitae = 10;
        newCharacter.CurrentVitae = 10;

        _dbContext.Characters.Add(newCharacter);
        await _dbContext.SaveChangesAsync();
        
        return newCharacter;
    }
}
