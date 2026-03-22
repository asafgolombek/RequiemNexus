namespace RequiemNexus.Domain.Models;

/// <summary>
/// In-memory row used to validate worn gear and backpack placement (Phase 11 carry rules).
/// </summary>
/// <param name="CharacterAssetId">Inventory row id.</param>
/// <param name="IsServiceAsset">True when the catalog asset is a service (not wearable).</param>
/// <param name="IsArmorAsset">True when the catalog asset is armor.</param>
/// <param name="IsWeaponAsset">True when the catalog asset is a weapon.</param>
/// <param name="WeaponSlotPoints">Slot cost when equipped; ignored when not a weapon.</param>
/// <param name="IsEquipped">Contributes worn slots and derived stats when true.</param>
/// <param name="BackpackSlotIndex">Zero-based backpack slot, or null when not in the backpack.</param>
public sealed record EquipmentCarryRowSnapshot(
    int CharacterAssetId,
    bool IsServiceAsset,
    bool IsArmorAsset,
    bool IsWeaponAsset,
    int WeaponSlotPoints,
    bool IsEquipped,
    int? BackpackSlotIndex);
