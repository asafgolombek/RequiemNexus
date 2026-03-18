using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Manages equipment inventory for characters.
/// </summary>
public interface ICharacterEquipmentService
{
    /// <summary>Returns all equipment items available in the catalogue, ordered by name.</summary>
    Task<List<Equipment>> GetAvailableEquipmentAsync();

    /// <summary>Adds an equipment item to the character's inventory.</summary>
    /// <param name="characterId">The character receiving the equipment.</param>
    /// <param name="equipmentId">The catalogue item to add.</param>
    /// <param name="quantity">How many units to add.</param>
    /// <param name="userId">The authenticated user (must own the character or be Storyteller).</param>
    Task<CharacterEquipment> AddEquipmentAsync(int characterId, int equipmentId, int quantity, string userId);

    /// <summary>Removes an equipment entry from the character's inventory.</summary>
    /// <param name="characterEquipmentId">The specific inventory row to remove.</param>
    /// <param name="userId">The authenticated user (must own the character or be Storyteller).</param>
    Task RemoveEquipmentAsync(int characterEquipmentId, string userId);
}
