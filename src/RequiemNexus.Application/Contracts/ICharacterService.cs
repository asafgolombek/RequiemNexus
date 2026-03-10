using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Contracts;

public interface ICharacterService
{
    Task<List<Character>> GetCharactersByUserIdAsync(string userId);

    /// <summary>Returns archived characters owned by the given user.</summary>
    Task<List<Character>> GetArchivedCharactersAsync(string userId);

    Task<Character?> GetCharacterByIdAsync(int id, string userId);

    Task DeleteCharacterAsync(int id);

    Task<Character> EmbraceCharacterAsync(Character newCharacter);

    // Persistence
    Task SaveAsync(Character character);

    // Beat & XP mutations
    Task AddBeatAsync(Character character);

    Task RemoveBeatAsync(Character character);

    Task AddXPAsync(Character character);

    Task RemoveXPAsync(Character character);

    // Equipment
    Task<List<Equipment>> GetAvailableEquipmentAsync();

    Task<CharacterEquipment> AddEquipmentAsync(int characterId, int equipmentId, int quantity);

    Task RemoveEquipmentAsync(int characterEquipmentId);

    // Merits & Disciplines
    Task<List<Merit>> GetAvailableMeritsAsync();

    Task<CharacterMerit> AddMeritAsync(Character character, int meritId, string? specification, int rating, int xpCost);

    Task<List<Discipline>> GetAvailableDisciplinesAsync();

    Task<CharacterDiscipline> AddDisciplineAsync(Character character, int disciplineId, int rating, int xpCost);

    // Retirement (campaign-scoped)

    /// <summary>Retires the character from campaign play. Owner or campaign ST may call this.</summary>
    Task RetireCharacterAsync(int characterId, string userId);

    /// <summary>Un-retires the character, returning it to active campaign play. Owner or campaign ST may call this.</summary>
    Task UnretireCharacterAsync(int characterId, string userId);

    // Archiving (global)

    /// <summary>Archives the character globally, hiding it from the active character list. Owner only.</summary>
    Task ArchiveCharacterAsync(int characterId, string userId);

    /// <summary>Un-archives the character, restoring it to the active list. Owner only.</summary>
    Task UnarchiveCharacterAsync(int characterId, string userId);

    // Dice Macros

    /// <summary>Returns all dice macros saved for the given character.</summary>
    Task<List<DiceMacro>> GetDiceMacrosAsync(int characterId);

    /// <summary>Creates a new dice macro for the character. Owner only.</summary>
    Task<DiceMacro> CreateDiceMacroAsync(int characterId, string name, int dicePool, string description, string userId);

    /// <summary>Deletes a dice macro. Owner only.</summary>
    Task DeleteDiceMacroAsync(int macroId, string userId);

    /// <summary>
    /// Returns the fully-loaded character together with an access-level flag.
    /// <list type="bullet">
    ///   <item>Returns <c>(Character, true)</c> when <paramref name="requestingUserId"/> is the character's owner.</item>
    ///   <item>Returns <c>(Character, false)</c> when the user is a member of the character's campaign (Storyteller or fellow player) — read-only access.</item>
    ///   <item>Returns <c>null</c> when the user has no access.</item>
    /// </list>
    /// </summary>
    Task<(Character Character, bool IsOwner)?> GetCharacterWithAccessCheckAsync(int characterId, string requestingUserId);
}
