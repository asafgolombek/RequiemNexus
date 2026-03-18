using RequiemNexus.Application.DTOs;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Manages devotion purchases and validation for characters.
/// </summary>
public interface IDevotionService
{
    /// <summary>Returns all devotions from the catalogue, ordered by name.</summary>
    Task<List<DevotionDefinition>> GetAllDevotionsAsync();

    /// <summary>
    /// Returns devotions the character meets the discipline prerequisites for, but does not yet possess.
    /// </summary>
    Task<List<DevotionDefinition>> GetEligibleDevotionsAsync(Character character);

    /// <summary>
    /// Purchases a devotion for the character. Validates prerequisites and deducts XP.
    /// </summary>
    Task<CharacterDevotion> PurchaseDevotionAsync(Character character, int devotionDefinitionId, string? userId);

    /// <summary>
    /// Checks if a character meets the prerequisites for a specific devotion.
    /// </summary>
    bool MeetsPrerequisites(Character character, DevotionDefinition devotion);
}
