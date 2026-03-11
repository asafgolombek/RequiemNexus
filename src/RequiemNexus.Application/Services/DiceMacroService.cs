using Microsoft.EntityFrameworkCore;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Application service for saved dice macros. All mutations require character ownership.
/// </summary>
public class DiceMacroService(ApplicationDbContext dbContext) : IDiceMacroService
{
    private readonly ApplicationDbContext _dbContext = dbContext;

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
