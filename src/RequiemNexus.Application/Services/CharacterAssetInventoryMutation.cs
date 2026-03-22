using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Models;
using RequiemNexus.Domain.Services;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Applies in-memory proposals to tracked inventory rows before validation and save.
/// </summary>
internal static class CharacterAssetInventoryMutation
{
    internal static void ApplyEquippedProposal(IReadOnlyList<CharacterAsset> all, int characterAssetId, bool isEquipped)
    {
        foreach (CharacterAsset ca in all)
        {
            if (ca.Id != characterAssetId)
            {
                continue;
            }

            ca.IsEquipped = isEquipped;
            if (isEquipped)
            {
                ca.BackpackSlotIndex = null;
            }
        }
    }

    internal static void ApplyBackpackProposal(IReadOnlyList<CharacterAsset> all, int characterAssetId, int? slotIndex)
    {
        CharacterAsset? target = all.FirstOrDefault(ca => ca.Id == characterAssetId)
            ?? throw new InvalidOperationException($"Character asset {characterAssetId} was not found.");

        if (slotIndex.HasValue)
        {
            foreach (CharacterAsset ca in all)
            {
                if (ca.Id != characterAssetId && ca.BackpackSlotIndex == slotIndex)
                {
                    ca.BackpackSlotIndex = null;
                }
            }

            target.IsEquipped = false;
            target.BackpackSlotIndex = slotIndex;
        }
        else
        {
            target.BackpackSlotIndex = null;
        }
    }

    internal static Result<bool> ValidateInventory(IReadOnlyList<CharacterAsset> rows)
    {
        List<EquipmentCarryRowSnapshot> snapshots = [];
        foreach (CharacterAsset ca in rows)
        {
            if (ca.Asset == null)
            {
                return Result<bool>.Failure($"Inventory row {ca.Id} is missing catalog data.");
            }

            snapshots.Add(CharacterAssetCarrySnapshotMapper.ToSnapshot(ca));
        }

        return EquipmentCarryRules.ValidateState(snapshots);
    }
}
