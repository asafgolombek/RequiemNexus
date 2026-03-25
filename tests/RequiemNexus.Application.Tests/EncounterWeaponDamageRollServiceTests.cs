using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RequiemNexus.Application.DTOs;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Application.Services;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.Models.Enums;
using RequiemNexus.Domain.Contracts;
using RequiemNexus.Domain.Models;
using Xunit;

namespace RequiemNexus.Application.Tests;

/// <summary>
/// Tests for <see cref="EncounterWeaponDamageRollService"/> — ownership, encounter state, and session publish.
/// </summary>
public class EncounterWeaponDamageRollServiceTests
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

    private static void SeedActiveEncounter(ApplicationDbContext ctx)
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

    private static async Task<(EncounterWeaponDamageRollService Service, Mock<ISessionService> Session)> CreateSutAsync(
        string dbName,
        Action<ApplicationDbContext>? seed = null)
    {
        DbContextOptions<ApplicationDbContext> options = CreateOptions(dbName);
        var ctx = new ApplicationDbContext(options);
        seed?.Invoke(ctx);
        await ctx.SaveChangesAsync();

        var auth = new AuthorizationHelper(new TestDbContextFactory(options), NullLogger<AuthorizationHelper>.Instance);
        var diceMock = new Mock<IDiceService>();
        diceMock.Setup(d => d.Roll(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int?>()))
            .Returns(new RollResult { Successes = 2, DiceRolled = [8, 9] });

        var sessionMock = new Mock<ISessionService>();
        var service = new EncounterWeaponDamageRollService(
            ctx,
            auth,
            diceMock.Object,
            sessionMock.Object,
            NullLogger<EncounterWeaponDamageRollService>.Instance);

        return (service, sessionMock);
    }

    [Fact]
    public async Task RollAndPublishAsync_Owner_Unarmed_PublishesAndReturnsOutcome()
    {
        string db = nameof(RollAndPublishAsync_Owner_Unarmed_PublishesAndReturnsOutcome);
        (EncounterWeaponDamageRollService service, Mock<ISessionService> sessionMock) = await CreateSutAsync(db, SeedActiveEncounter);

        EncounterWeaponDamageRollOutcomeDto result = await service.RollAndPublishAsync("player-1", 1, 100, 10, null);

        Assert.Equal(2, result.Successes);
        Assert.Contains("unarmed", result.PoolDescription, StringComparison.OrdinalIgnoreCase);
        sessionMock.Verify(
            s => s.PublishDiceRollAsync(
                "player-1",
                1,
                10,
                It.IsAny<string>(),
                It.IsAny<RollResult>()),
            Times.Once);
    }

    [Fact]
    public async Task RollAndPublishAsync_NotOwner_ThrowsUnauthorized()
    {
        string db = nameof(RollAndPublishAsync_NotOwner_ThrowsUnauthorized);
        (EncounterWeaponDamageRollService service, _) = await CreateSutAsync(db, SeedActiveEncounter);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.RollAndPublishAsync("intruder", 1, 100, 10, null));
    }

    [Fact]
    public async Task RollAndPublishAsync_CharacterNotInEncounter_Throws()
    {
        string db = nameof(RollAndPublishAsync_CharacterNotInEncounter_Throws);
        (EncounterWeaponDamageRollService service, _) = await CreateSutAsync(db, ctx =>
        {
            SeedActiveEncounter(ctx);
            ctx.Characters.Add(new Character
            {
                Id = 11,
                CampaignId = 1,
                ApplicationUserId = "player-2",
                Name = "Other",
                MaxHealth = 7,
                CurrentHealth = 7,
            });
        });

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RollAndPublishAsync("player-2", 1, 100, 11, null));

        Assert.Contains("not part of this encounter", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RollAndPublishAsync_WrongChronicleId_Throws()
    {
        string db = nameof(RollAndPublishAsync_WrongChronicleId_Throws);
        (EncounterWeaponDamageRollService service, _) = await CreateSutAsync(db, SeedActiveEncounter);

        InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RollAndPublishAsync("player-1", 99, 100, 10, null));

        Assert.Contains("does not belong", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RollAndPublishAsync_WeaponNotEquipped_Throws()
    {
        string db = nameof(RollAndPublishAsync_WeaponNotEquipped_Throws);
        (EncounterWeaponDamageRollService service, _) = await CreateSutAsync(db, ctx =>
        {
            SeedActiveEncounter(ctx);
            ctx.Assets.Add(new WeaponAsset
            {
                Id = 50,
                Name = "Blade",
                Kind = AssetKind.Weapon,
                Damage = 2,
            });
            ctx.CharacterAssets.Add(new CharacterAsset
            {
                Id = 500,
                CharacterId = 10,
                AssetId = 50,
                IsEquipped = false,
                Quantity = 1,
            });
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RollAndPublishAsync("player-1", 1, 100, 10, 500));
    }
}
