using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.Services;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.Models.Enums;
using RequiemNexus.Domain.Contracts;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Models;
using Xunit;

namespace RequiemNexus.Application.Tests;

/// <summary>
/// Tests for <see cref="AttackService"/> — encounter authorization, weapon row validation, and dice aggregation.
/// </summary>
public class AttackServiceTests
{
    private sealed class TestDbContextFactory : IDbContextFactory<ApplicationDbContext>
    {
        private readonly DbContextOptions<ApplicationDbContext> _options;

        public TestDbContextFactory(DbContextOptions<ApplicationDbContext> options) => _options = options;

        public ApplicationDbContext CreateDbContext() => new(_options);

        public ValueTask<ApplicationDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new ApplicationDbContext(_options));
    }

    private static DbContextOptions<ApplicationDbContext> CreateOptions(string dbName) =>
        new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(dbName).Options;

    private static async Task<(ApplicationDbContext Ctx, AttackService Service, Mock<IDiceService> Dice)> CreateSutAsync(
        string dbName,
        Action<ApplicationDbContext>? seed = null)
    {
        DbContextOptions<ApplicationDbContext> options = CreateOptions(dbName);
        var ctx = new ApplicationDbContext(options);
        seed?.Invoke(ctx);
        await ctx.SaveChangesAsync();

        var auth = new AuthorizationHelper(new TestDbContextFactory(options), NullLogger<AuthorizationHelper>.Instance);
        var traitMock = new Mock<ITraitResolver>();
        traitMock.Setup(t => t.ResolvePoolAsync(It.IsAny<Character>(), It.IsAny<PoolDefinition>()))
            .ReturnsAsync(4);

        var diceMock = new Mock<IDiceService>();
        var attackResult = new RollResult { Successes = 3, DiceRolled = [8, 8, 7, 4] };
        var weaponResult = new RollResult { Successes = 2, DiceRolled = [9, 8, 5] };
        var call = 0;
        diceMock.Setup(d => d.Roll(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int?>()))
            .Returns(() =>
            {
                call++;
                return call == 1 ? attackResult : weaponResult;
            });

        var charMock = new Mock<ICharacterService>();
        charMock.Setup(c => c.GetCharacterWithAccessCheckAsync(10, "st-1"))
            .ReturnsAsync((new Character { Id = 10, CampaignId = 1, Name = "Attacker" }, false));

        var service = new AttackService(
            ctx,
            auth,
            charMock.Object,
            traitMock.Object,
            diceMock.Object,
            NullLogger<AttackService>.Instance);

        return (ctx, service, diceMock);
    }

    private static void SeedStandardEncounter(ApplicationDbContext ctx)
    {
        ctx.Campaigns.Add(new Campaign { Id = 1, Name = "Chronicle", StoryTellerId = "st-1" });
        ctx.Characters.Add(new Character
        {
            Id = 10,
            CampaignId = 1,
            ApplicationUserId = "player-1",
            Name = "Attacker",
            MaxHealth = 7,
            CurrentHealth = 7,
        });
        ctx.CombatEncounters.Add(new CombatEncounter
        {
            Id = 100,
            CampaignId = 1,
            Name = "Fight",
            IsActive = true,
            IsDraft = false,
        });
        ctx.InitiativeEntries.Add(new InitiativeEntry
        {
            Id = 1000,
            EncounterId = 100,
            CharacterId = 10,
            InitiativeMod = 0,
            RollResult = 0,
            Total = 0,
            Order = 1,
        });
    }

    [Fact]
    public async Task ResolveMeleeAttackAsync_Unarmed_RollsAttackOnly_WeaponSuccessesZero()
    {
        string db = nameof(ResolveMeleeAttackAsync_Unarmed_RollsAttackOnly_WeaponSuccessesZero);
        var (_, service, _) = await CreateSutAsync(db, ctx =>
        {
            SeedStandardEncounter(ctx);
        });

        var pool = new PoolDefinition(
            [new TraitReference(TraitType.Attribute, RequiemNexus.Domain.AttributeId.Strength, null, null)]);
        AttackResult result = await service.ResolveMeleeAttackAsync(
            "st-1",
            100,
            10,
            defenderDefense: 1,
            pool,
            weaponCharacterAssetId: null,
            DamageSource.Bashing);

        Assert.Equal(3, result.AttackSuccesses);
        Assert.Equal(1, result.DefenseApplied);
        Assert.Equal(2, result.NetAttackSuccesses);
        Assert.Equal(0, result.WeaponDamageSuccesses);
        Assert.Equal(2, result.TotalDamageInstances);
    }

    [Fact]
    public async Task ResolveMeleeAttackAsync_EquippedWeapon_RollsWeaponPoolFromProfile()
    {
        string db = nameof(ResolveMeleeAttackAsync_EquippedWeapon_RollsWeaponPoolFromProfile);
        var (_, service, diceMock) = await CreateSutAsync(db, ctx =>
        {
            SeedStandardEncounter(ctx);
            var blade = new WeaponAsset
            {
                Id = 50,
                Name = "Blade",
                Kind = AssetKind.Weapon,
                Slug = "test:blade",
                Damage = 3,
                StrengthRequirement = 1,
                IsRangedWeapon = false,
                UsesBrawlForAttacks = false,
            };
            ctx.Assets.Add(blade);
            ctx.CharacterAssets.Add(new CharacterAsset
            {
                Id = 200,
                CharacterId = 10,
                AssetId = 50,
                IsEquipped = true,
                CurrentStructure = 3,
            });
        });

        var pool = new PoolDefinition(
            [new TraitReference(TraitType.Attribute, RequiemNexus.Domain.AttributeId.Strength, null, null)]);
        AttackResult result = await service.ResolveMeleeAttackAsync(
            "st-1",
            100,
            10,
            defenderDefense: 0,
            pool,
            weaponCharacterAssetId: 200,
            DamageSource.Weapon);

        diceMock.Verify(d => d.Roll(3, It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int?>()), Times.Once);
        Assert.Equal(2, result.WeaponDamageSuccesses);
        Assert.Equal(5, result.TotalDamageInstances);
    }

    [Fact]
    public async Task ResolveMeleeAttackAsync_WeaponWrongOwner_Throws()
    {
        string db = nameof(ResolveMeleeAttackAsync_WeaponWrongOwner_Throws);
        var (_, service, _) = await CreateSutAsync(db, ctx =>
        {
            SeedStandardEncounter(ctx);
            var blade = new WeaponAsset
            {
                Id = 50,
                Name = "Blade",
                Kind = AssetKind.Weapon,
                Slug = "test:blade",
                Damage = 2,
                StrengthRequirement = 1,
                IsRangedWeapon = false,
                UsesBrawlForAttacks = false,
            };
            ctx.Assets.Add(blade);
            ctx.CharacterAssets.Add(new CharacterAsset
            {
                Id = 200,
                CharacterId = 99,
                AssetId = 50,
                IsEquipped = true,
                CurrentStructure = 3,
            });
        });

        var pool = new PoolDefinition(
            [new TraitReference(TraitType.Attribute, RequiemNexus.Domain.AttributeId.Strength, null, null)]);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ResolveMeleeAttackAsync(
            "st-1",
            100,
            10,
            0,
            pool,
            200,
            DamageSource.Weapon));
    }

    [Fact]
    public async Task ResolveMeleeAttackAsync_WeaponNotEquipped_Throws()
    {
        string db = nameof(ResolveMeleeAttackAsync_WeaponNotEquipped_Throws);
        var (_, service, _) = await CreateSutAsync(db, ctx =>
        {
            SeedStandardEncounter(ctx);
            var blade = new WeaponAsset
            {
                Id = 50,
                Name = "Blade",
                Kind = AssetKind.Weapon,
                Slug = "test:blade",
                Damage = 2,
                StrengthRequirement = 1,
                IsRangedWeapon = false,
                UsesBrawlForAttacks = false,
            };
            ctx.Assets.Add(blade);
            ctx.CharacterAssets.Add(new CharacterAsset
            {
                Id = 200,
                CharacterId = 10,
                AssetId = 50,
                IsEquipped = false,
                CurrentStructure = 3,
            });
        });

        var pool = new PoolDefinition(
            [new TraitReference(TraitType.Attribute, RequiemNexus.Domain.AttributeId.Strength, null, null)]);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ResolveMeleeAttackAsync(
            "st-1",
            100,
            10,
            0,
            pool,
            200,
            DamageSource.Weapon));
    }
}
