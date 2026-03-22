using RequiemNexus.Domain.Models;
using RequiemNexus.Domain.Services;
using Xunit;

namespace RequiemNexus.Domain.Tests;

/// <summary>
/// Tests for <see cref="EquipmentCarryRules"/>.
/// </summary>
public sealed class EquipmentCarryRulesTests
{
    [Fact]
    public void ValidateState_AllowsEmpty()
    {
        Result<bool> r = EquipmentCarryRules.ValidateState([]);
        Assert.True(r.IsSuccess);
    }

    [Fact]
    public void ValidateState_RejectsServiceEquipped()
    {
        EquipmentCarryRowSnapshot[] rows =
        [
            new(1, IsServiceAsset: true, IsArmorAsset: false, IsWeaponAsset: false, 1, IsEquipped: true, null),
        ];

        Result<bool> r = EquipmentCarryRules.ValidateState(rows);
        Assert.False(r.IsSuccess);
        Assert.Contains("Services cannot", r.Error ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidateState_RejectsWornAndBackpack()
    {
        EquipmentCarryRowSnapshot[] rows =
        [
            new(1, false, true, false, 1, IsEquipped: true, BackpackSlotIndex: 0),
        ];

        Result<bool> r = EquipmentCarryRules.ValidateState(rows);
        Assert.False(r.IsSuccess);
    }

    [Fact]
    public void ValidateState_RejectsTwoArmorEquipped()
    {
        EquipmentCarryRowSnapshot[] rows =
        [
            new(1, false, true, false, 1, true, null),
            new(2, false, true, false, 1, true, null),
        ];

        Result<bool> r = EquipmentCarryRules.ValidateState(rows);
        Assert.False(r.IsSuccess);
        Assert.Contains("one suit of armor", r.Error ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidateState_RejectsWeaponPointsOverCap()
    {
        EquipmentCarryRowSnapshot[] rows =
        [
            new(1, false, false, true, 2, true, null),
            new(2, false, false, true, 2, true, null),
        ];

        Result<bool> r = EquipmentCarryRules.ValidateState(rows);
        Assert.False(r.IsSuccess);
        Assert.Contains("weapon slots", r.Error ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidateState_AllowsTwoOnePointWeapons()
    {
        EquipmentCarryRowSnapshot[] rows =
        [
            new(1, false, false, true, 1, true, null),
            new(2, false, false, true, 1, true, null),
        ];

        Result<bool> r = EquipmentCarryRules.ValidateState(rows);
        Assert.True(r.IsSuccess);
    }

    [Fact]
    public void ValidateState_AllowsOneTwoPointWeapon()
    {
        EquipmentCarryRowSnapshot[] rows =
        [
            new(1, false, false, true, 2, true, null),
        ];

        Result<bool> r = EquipmentCarryRules.ValidateState(rows);
        Assert.True(r.IsSuccess);
    }

    [Fact]
    public void ValidateState_RejectsDuplicateBackpackSlot()
    {
        EquipmentCarryRowSnapshot[] rows =
        [
            new(1, false, false, false, 1, false, 0),
            new(2, false, false, false, 1, false, 0),
        ];

        Result<bool> r = EquipmentCarryRules.ValidateState(rows);
        Assert.False(r.IsSuccess);
    }

    [Fact]
    public void ValidateState_RejectsBackpackIndexOutOfRange()
    {
        EquipmentCarryRowSnapshot[] rows =
        [
            new(1, false, false, false, 1, false, 10),
        ];

        Result<bool> r = EquipmentCarryRules.ValidateState(rows);
        Assert.False(r.IsSuccess);
    }
}
