using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Manages saved dice macros for characters. All mutations require character ownership.
/// </summary>
public interface IDiceMacroService
{
    /// <summary>Returns all dice macros saved for the given character, ordered by name.</summary>
    /// <param name="characterId">The character whose macros to return.</param>
    Task<List<DiceMacro>> GetDiceMacrosAsync(int characterId);

    /// <summary>Creates a new dice macro for the character. Owner only.</summary>
    /// <param name="characterId">The character to create the macro for.</param>
    /// <param name="name">Display name for the macro.</param>
    /// <param name="dicePool">Number of dice in the pool.</param>
    /// <param name="description">Optional narrative description.</param>
    /// <param name="userId">The requesting user (must be the character owner).</param>
    Task<DiceMacro> CreateDiceMacroAsync(int characterId, string name, int dicePool, string description, string userId);

    /// <summary>Deletes a dice macro. Owner only.</summary>
    /// <param name="macroId">The macro to delete.</param>
    /// <param name="userId">The requesting user (must be the character owner).</param>
    Task DeleteDiceMacroAsync(int macroId, string userId);
}
