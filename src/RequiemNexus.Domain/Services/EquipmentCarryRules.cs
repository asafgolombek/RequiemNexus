using RequiemNexus.Domain.Models;

namespace RequiemNexus.Domain.Services;

/// <summary>
/// Validates worn armor, weapon slot budget, backpack capacity, and mutual exclusion (Phase 11).
/// </summary>
public static class EquipmentCarryRules
{
    /// <summary>Maximum distinct backpack slots per character.</summary>
    public const int MaxBackpackSlots = 10;

    /// <summary>Maximum total weapon slot points when weapons are equipped.</summary>
    public const int MaxWeaponSlotPoints = 2;

    /// <summary>Maximum equipped armor rows.</summary>
    public const int MaxEquippedArmor = 1;

    /// <summary>Valid backpack indices are 0 through <see cref="MaxBackpackSlots"/> - 1.</summary>
    public static bool IsValidBackpackSlotIndex(int index) => index >= 0 && index < MaxBackpackSlots;

    /// <summary>
    /// Validates a full carry state for one character.
    /// </summary>
    /// <param name="rows">All inventory rows for the character.</param>
    /// <returns>Success when all rules pass.</returns>
    public static Result<bool> ValidateState(IReadOnlyList<EquipmentCarryRowSnapshot> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        var backpackUsed = new HashSet<int>();
        foreach (EquipmentCarryRowSnapshot row in rows)
        {
            if (row.IsEquipped && row.BackpackSlotIndex.HasValue)
            {
                return Result<bool>.Failure("An item cannot be worn and in the backpack at the same time.");
            }

            if (row.IsServiceAsset && row.IsEquipped)
            {
                return Result<bool>.Failure("Services cannot be worn or equipped.");
            }

            if (row.BackpackSlotIndex is int bp)
            {
                if (!IsValidBackpackSlotIndex(bp))
                {
                    return Result<bool>.Failure($"Backpack slot must be between 1 and {MaxBackpackSlots}.");
                }

                if (!backpackUsed.Add(bp))
                {
                    return Result<bool>.Failure("Each backpack slot can hold at most one item.");
                }
            }
        }

        int equippedArmor = rows.Count(r => r.IsArmorAsset && r.IsEquipped);
        if (equippedArmor > MaxEquippedArmor)
        {
            return Result<bool>.Failure("You can wear only one suit of armor at a time.");
        }

        int weaponPoints = rows
            .Where(r => r.IsWeaponAsset && r.IsEquipped)
            .Sum(r => Math.Max(1, r.WeaponSlotPoints));

        if (weaponPoints > MaxWeaponSlotPoints)
        {
            return Result<bool>.Failure(
                $"Equipped weapons exceed the carry limit ({MaxWeaponSlotPoints} weapon slots).");
        }

        return Result<bool>.Success(true);
    }
}
