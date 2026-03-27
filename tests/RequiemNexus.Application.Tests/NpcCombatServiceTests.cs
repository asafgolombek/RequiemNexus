using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Application.Services;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.RealTime;
using RequiemNexus.Domain.Contracts;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Models;
using Xunit;

namespace RequiemNexus.Application.Tests;

/// <summary>
/// Tests for <see cref="NpcCombatService"/> NPC health track and launched/paused encounter rules.
/// </summary>
public class NpcCombatServiceTests
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

    private static async Task<(NpcCombatService Service, ApplicationDbContext Ctx)> CreateSutAsync(
        string dbName,
        Action<ApplicationDbContext> seed)
    {
        DbContextOptions<ApplicationDbContext> options = CreateOptions(dbName);
        var ctx = new ApplicationDbContext(options);
        seed(ctx);
        await ctx.SaveChangesAsync();

        var auth = new AuthorizationHelper(new TestDbContextFactory(options), NullLogger<AuthorizationHelper>.Instance);
        var sessionMock = new Mock<ISessionService>();
        sessionMock
            .Setup(s => s.UpdateInitiativeAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<IEnumerable<InitiativeEntryDto>>()))
            .Returns(Task.CompletedTask);

        var diceMock = new Mock<IDiceService>();
        var service = new NpcCombatService(
            ctx,
            NullLogger<NpcCombatService>.Instance,
            auth,
            sessionMock.Object,
            diceMock.Object);

        return (service, ctx);
    }

    [Fact]
    public async Task SetNpcHealthDamageAsync_PausedEncounter_UpdatesTrack()
    {
        string db = nameof(SetNpcHealthDamageAsync_PausedEncounter_UpdatesTrack);
        (NpcCombatService service, ApplicationDbContext ctx) = await CreateSutAsync(db, c =>
        {
            c.Campaigns.Add(new Campaign { Id = 1, Name = "Saga", StoryTellerId = "st-1" });
            c.CombatEncounters.Add(new CombatEncounter
            {
                Id = 50,
                CampaignId = 1,
                Name = "Fight",
                IsDraft = false,
                IsActive = false,
                IsPaused = true,
            });
            c.InitiativeEntries.Add(new InitiativeEntry
            {
                Id = 500,
                EncounterId = 50,
                NpcName = "Hunter",
                CharacterId = null,
                NpcHealthBoxes = 5,
                NpcHealthDamage = string.Empty,
                InitiativeMod = 0,
                RollResult = 5,
                Total = 5,
                Order = 1,
            });
        });

        await service.SetNpcHealthDamageAsync(500, "/    ", "st-1");

        InitiativeEntry? row = await ctx.InitiativeEntries.FindAsync(500);
        Assert.NotNull(row);
        Assert.Equal("/    ", row.NpcHealthDamage);
    }

    [Fact]
    public async Task ApplyNpcDamageBatchAsync_FillsLeftmostEmptySlots_OnNormalizedTrack()
    {
        string db = nameof(ApplyNpcDamageBatchAsync_FillsLeftmostEmptySlots_OnNormalizedTrack);
        (NpcCombatService service, ApplicationDbContext ctx) = await CreateSutAsync(db, c =>
        {
            c.Campaigns.Add(new Campaign { Id = 1, Name = "Saga", StoryTellerId = "st-1" });
            c.CombatEncounters.Add(new CombatEncounter
            {
                Id = 60,
                CampaignId = 1,
                Name = "Fight",
                IsDraft = false,
                IsActive = true,
                IsPaused = false,
            });
            c.InitiativeEntries.Add(new InitiativeEntry
            {
                Id = 600,
                EncounterId = 60,
                NpcName = "Ghoul",
                CharacterId = null,
                NpcHealthBoxes = 7,
                NpcHealthDamage = "///",
                InitiativeMod = 0,
                RollResult = 3,
                Total = 3,
                Order = 1,
            });
        });

        await service.ApplyNpcDamageBatchAsync(600, HealthDamageKind.Lethal, 2, "st-1");

        InitiativeEntry? row = await ctx.InitiativeEntries.FindAsync(600);
        Assert.NotNull(row);
        Assert.Equal("///XX  ", row.NpcHealthDamage);
    }

    [Fact]
    public async Task ApplyNpcDamageBatchAsync_LegacyShortTrack_NormalizesThenFills()
    {
        string db = nameof(ApplyNpcDamageBatchAsync_LegacyShortTrack_NormalizesThenFills);
        (NpcCombatService service, ApplicationDbContext ctx) = await CreateSutAsync(db, c =>
        {
            c.Campaigns.Add(new Campaign { Id = 1, Name = "Saga", StoryTellerId = "st-1" });
            c.CombatEncounters.Add(new CombatEncounter
            {
                Id = 70,
                CampaignId = 1,
                Name = "Fight",
                IsDraft = false,
                IsActive = true,
                IsPaused = false,
            });
            c.InitiativeEntries.Add(new InitiativeEntry
            {
                Id = 700,
                EncounterId = 70,
                NpcName = "Thrall",
                CharacterId = null,
                NpcHealthBoxes = 5,
                NpcHealthDamage = "/",
                InitiativeMod = 0,
                RollResult = 1,
                Total = 1,
                Order = 1,
            });
        });

        await service.ApplyNpcDamageBatchAsync(700, HealthDamageKind.Bashing, 2, "st-1");

        InitiativeEntry? row = await ctx.InitiativeEntries.FindAsync(700);
        Assert.NotNull(row);
        Assert.Equal("/    ", NpcHealthDamageTrack.Normalize("/", 5));
        Assert.Equal("///  ", row!.NpcHealthDamage);
    }
}
