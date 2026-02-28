using RequiemNexus.Data.Models;

namespace RequiemNexus.Web.Contracts;

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
}
