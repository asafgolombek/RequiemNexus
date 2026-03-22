using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RequiemNexus.Application.Services;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.Models.Enums;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Models;
using Xunit;

namespace RequiemNexus.Application.Tests;

/// <summary>
/// Phase 11: equipped <see cref="CharacterAsset"/> rows and catalog modifiers.
/// </summary>
public class ModifierServiceTests
{
    private static ApplicationDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ApplicationDbContext(options);
    }

    private static Character CreateBareCharacter(int id = 1, string userId = "u1") =>
        new()
        {
            Id = id,
            ApplicationUserId = userId,
            Name = "Test",
            MaxHealth = 7,
            CurrentHealth = 7,
            MaxWillpower = 4,
            CurrentWillpower = 4,
            MaxVitae = 10,
            CurrentVitae = 10,
        };

    [Fact]
    public async Task GetModifiersForCharacterAsync_BrokenEquippedGear_EmitsNoEquipmentBonuses()
    {
        string db = nameof(GetModifiersForCharacterAsync_BrokenEquippedGear_EmitsNoEquipmentBonuses);
        await using var ctx = CreateContext(db);
        ctx.Characters.Add(CreateBareCharacter());
        ctx.CharacterAttributes.Add(new CharacterAttribute
        {
            CharacterId = 1,
            Name = nameof(RequiemNexus.Domain.AttributeId.Strength),
            Rating = 3,
        });
        var kit = new EquipmentAsset
        {
            Id = 10,
            Name = "Kit",
            Kind = AssetKind.General,
            Slug = "test:kit",
            AssistsSkillName = "Larceny",
            DiceBonusMax = 2,
        };
        ctx.Assets.Add(kit);
        ctx.CharacterAssets.Add(new CharacterAsset
        {
            Id = 1,
            CharacterId = 1,
            AssetId = 10,
            IsEquipped = true,
            CurrentStructure = 0,
        });
        await ctx.SaveChangesAsync();

        var service = new ModifierService(ctx, NullLogger<ModifierService>.Instance);
        IReadOnlyList<PassiveModifier> mods = await service.GetModifiersForCharacterAsync(1);

        Assert.DoesNotContain(
            mods,
            m => m.Source.SourceType == ModifierSourceType.Equipment && m.Target == ModifierTarget.SkillPool);
    }

    [Fact]
    public async Task GetModifiersForCharacterAsync_UnderStrengthWeapon_AppliesCombatSkillPenalty()
    {
        string db = nameof(GetModifiersForCharacterAsync_UnderStrengthWeapon_AppliesCombatSkillPenalty);
        await using var ctx = CreateContext(db);
        ctx.Characters.Add(CreateBareCharacter());
        ctx.CharacterAttributes.Add(new CharacterAttribute
        {
            CharacterId = 1,
            Name = nameof(RequiemNexus.Domain.AttributeId.Strength),
            Rating = 2,
        });
        var weapon = new WeaponAsset
        {
            Id = 20,
            Name = "Heavy",
            Kind = AssetKind.Weapon,
            Slug = "test:heavy",
            Damage = 1,
            StrengthRequirement = 4,
            IsRangedWeapon = false,
            UsesBrawlForAttacks = false,
        };
        ctx.Assets.Add(weapon);
        ctx.CharacterAssets.Add(new CharacterAsset
        {
            Id = 1,
            CharacterId = 1,
            AssetId = 20,
            IsEquipped = true,
            CurrentStructure = 3,
        });
        await ctx.SaveChangesAsync();

        var service = new ModifierService(ctx, NullLogger<ModifierService>.Instance);
        IReadOnlyList<PassiveModifier> mods = await service.GetModifiersForCharacterAsync(1);

        Assert.Contains(mods, m => m.Target == ModifierTarget.Brawl && m.Value == -1);
        Assert.Contains(mods, m => m.Target == ModifierTarget.Weaponry && m.Value == -1);
        Assert.Contains(mods, m => m.Target == ModifierTarget.Firearms && m.Value == -1);
    }
}
