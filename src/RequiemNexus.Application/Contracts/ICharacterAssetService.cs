using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Manages character inventory rows against the asset catalog (Phase 11 TPT).
/// </summary>
public interface ICharacterAssetService
{
    /// <summary>Returns catalog assets shown in player pickers (listed only).</summary>
    Task<List<Asset>> GetListedCatalogAsync();

    /// <summary>Adds an asset row to inventory (direct grant; procurement rules are separate).</summary>
    Task<CharacterAsset> AddCharacterAssetAsync(int characterId, int assetId, int quantity, string userId);

    /// <summary>Removes an inventory row.</summary>
    Task RemoveCharacterAssetAsync(int characterAssetId, string userId);

    /// <summary>Sets equipped flag for mechanical contribution.</summary>
    Task SetEquippedAsync(int characterAssetId, bool isEquipped, string userId);

    /// <summary>Assigns a backpack slot (0–9) or clears when null. Clears equipped when moving into the backpack.</summary>
    Task SetBackpackSlotAsync(int characterAssetId, int? slotIndex, string userId);

    /// <summary>Updates structure track; null leaves unchanged.</summary>
    Task SetCurrentStructureAsync(int characterAssetId, int? currentStructure, string userId);
}
