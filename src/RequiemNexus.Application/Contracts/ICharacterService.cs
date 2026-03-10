using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Contracts;

public interface ICharacterService
{
    Task<List<Character>> GetCharactersByUserIdAsync(string userId);

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
