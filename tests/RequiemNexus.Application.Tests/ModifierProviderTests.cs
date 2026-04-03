using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RequiemNexus.Application.Services;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.Models.Enums;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Models;
using RequiemNexus.Domain.Services;
using Xunit;

namespace RequiemNexus.Application.Tests;

/// <summary>
/// Unit coverage for individual <see cref="Contracts.IModifierProvider"/> implementations.
/// </summary>
public class ModifierProviderTests
{
    private static ApplicationDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task ConditionModifierProvider_Shaken_AppliesAllPoolsPenalty()
    {
        string db = nameof(ConditionModifierProvider_Shaken_AppliesAllPoolsPenalty);
        await using var ctx = CreateContext(db);
        ctx.Characters.Add(new Character { Id = 1, ApplicationUserId = "u1", Name = "C" });
        ctx.CharacterConditions.Add(new CharacterCondition
        {
            CharacterId = 1,
            ConditionType = ConditionType.Shaken,
            IsResolved = false,
        });
        await ctx.SaveChangesAsync();

        var sut = new ConditionModifierProvider(ctx, new ConditionRules(), NullLogger<ConditionModifierProvider>.Instance);
        IReadOnlyList<PassiveModifier> mods = await sut.GetModifiersAsync(1);

        PassiveModifier m = Assert.Single(mods);
        Assert.Equal(ModifierTarget.AllDicePools, m.Target);
        Assert.Equal(-2, m.Value);
        Assert.Equal(ModifierSourceType.Condition, m.Source.SourceType);
    }

    [Fact]
    public async Task CoilModifierProvider_ApprovedCoil_DeserializesModifiersJson()
    {
        string db = nameof(CoilModifierProvider_ApprovedCoil_DeserializesModifiersJson);
        await using var ctx = CreateContext(db);
        ctx.Characters.Add(new Character { Id = 1, ApplicationUserId = "u1", Name = "C" });
        var scale = new ScaleDefinition { Id = 1, Name = "Test Scale", MysteryName = "M", Description = "d" };
        ctx.ScaleDefinitions.Add(scale);
        const string modifiersJson =
            """[{"target":"Speed","value":2,"modifierType":"Static","condition":"coil passive","source":{"sourceType":"Coil","sourceId":1}}]""";
        var coilDef = new CoilDefinition
        {
            Id = 1,
            Name = "Tier",
            Description = "d",
            Level = 1,
            ScaleId = 1,
            ModifiersJson = modifiersJson,
        };
        ctx.CoilDefinitions.Add(coilDef);
        ctx.CharacterCoils.Add(new CharacterCoil
        {
            CharacterId = 1,
            CoilDefinitionId = 1,
            Status = CoilLearnStatus.Approved,
            AppliedAt = DateTime.UtcNow,
        });
        await ctx.SaveChangesAsync();

        var sut = new CoilModifierProvider(ctx, NullLogger<CoilModifierProvider>.Instance);
        IReadOnlyList<PassiveModifier> mods = await sut.GetModifiersAsync(1);

        PassiveModifier m = Assert.Single(mods);
        Assert.Equal(ModifierTarget.Speed, m.Target);
        Assert.Equal(2, m.Value);
    }

    [Fact]
    public async Task WoundTrackModifierProvider_DamagedTrack_ReturnsWoundPenalty()
    {
        string db = nameof(WoundTrackModifierProvider_DamagedTrack_ReturnsWoundPenalty);
        await using var ctx = CreateContext(db);
        ctx.Characters.Add(new Character
        {
            Id = 1,
            ApplicationUserId = "u1",
            Name = "C",
            Size = 5,
            HealthDamage = "XXXXX  ",
        });
        ctx.CharacterAttributes.Add(new CharacterAttribute
        {
            CharacterId = 1,
            Name = nameof(AttributeId.Stamina),
            Rating = 2,
        });
        await ctx.SaveChangesAsync();

        var sut = new WoundTrackModifierProvider(ctx);
        IReadOnlyList<PassiveModifier> mods = await sut.GetModifiersAsync(1);

        PassiveModifier m = Assert.Single(mods);
        Assert.Equal(ModifierTarget.WoundPenalty, m.Target);
        Assert.Equal(-1, m.Value);
        Assert.Equal(ModifierSourceType.WoundTrack, m.Source.SourceType);
    }

    [Fact]
    public async Task EquipmentModifierProvider_NoEquippedAssets_ReturnsEmpty()
    {
        string db = nameof(EquipmentModifierProvider_NoEquippedAssets_ReturnsEmpty);
        await using var ctx = CreateContext(db);
        ctx.Characters.Add(new Character { Id = 1, ApplicationUserId = "u1", Name = "C" });
        await ctx.SaveChangesAsync();

        var sut = new EquipmentModifierProvider(ctx, NullLogger<EquipmentModifierProvider>.Instance);
        IReadOnlyList<PassiveModifier> mods = await sut.GetModifiersAsync(1);

        Assert.Empty(mods);
    }
}
