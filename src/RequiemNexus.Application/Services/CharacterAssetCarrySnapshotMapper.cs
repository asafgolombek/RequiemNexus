using RequiemNexus.Data.Models;
using RequiemNexus.Data.Models.Enums;
using RequiemNexus.Domain.Models;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Maps tracked <see cref="CharacterAsset"/> rows to domain carry snapshots for validation.
/// </summary>
public static class CharacterAssetCarrySnapshotMapper
{
    /// <summary>
    /// Builds a carry snapshot from a tracked row; <see cref="CharacterAsset.Asset"/> must be loaded.
    /// </summary>
    /// <param name="ca">Inventory row including navigation to <see cref="Asset"/>.</param>
    /// <returns>Snapshot for <see cref="EquipmentCarryRules.ValidateState"/>.</returns>
    public static EquipmentCarryRowSnapshot ToSnapshot(CharacterAsset ca)
    {
        Asset asset = ca.Asset ?? throw new InvalidOperationException($"Character asset {ca.Id} has no asset loaded.");

        bool isWeapon = asset is WeaponAsset;
        bool isArmor = asset is ArmorAsset;
        bool isService = asset.Kind == AssetKind.Service;
        int weaponPoints = asset is WeaponAsset w ? Math.Max(1, w.WeaponSlotPoints) : 1;

        return new EquipmentCarryRowSnapshot(
            ca.Id,
            isService,
            isArmor,
            isWeapon,
            weaponPoints,
            ca.IsEquipped,
            ca.BackpackSlotIndex);
    }
}
