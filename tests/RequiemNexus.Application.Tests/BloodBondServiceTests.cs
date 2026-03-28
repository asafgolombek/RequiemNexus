using System.Diagnostics.Metrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.DTOs;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Application.Services;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.RealTime;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Models;
using RequiemNexus.Domain.Services;
using Xunit;

namespace RequiemNexus.Application.Tests;

/// <summary>
/// Integration-style tests for <see cref="BloodBondService"/> (feeding, fading, <c>SourceTag</c> isolation).
/// </summary>
public class BloodBondServiceTests
{
    private static readonly Lazy<IMeterFactory> _meterFactory = new(() =>
    {
        ServiceCollection services = new();
        services.AddMetrics();
        return services.BuildServiceProvider().GetRequiredService<IMeterFactory>();
    });

    private sealed class TestApplicationDbContextFactory : IDbContextFactory<ApplicationDbContext>
    {
        private readonly DbContextOptions<ApplicationDbContext> _options;

        public TestApplicationDbContextFactory(DbContextOptions<ApplicationDbContext> options) => _options = options;

        public ApplicationDbContext CreateDbContext() => new(_options);

        public ValueTask<ApplicationDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new ApplicationDbContext(_options));
    }

    private static DbContextOptions<ApplicationDbContext> CreateOptions(string dbName)
    {
        var root = new InMemoryDatabaseRoot();
        return new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName, root)
            .Options;
    }

    private static BloodBondQueryService CreateQueryService(
        DbContextOptions<ApplicationDbContext> options,
        IAuthorizationHelper? auth = null)
    {
        return new BloodBondQueryService(
            new TestApplicationDbContextFactory(options),
            auth ?? CreatePermissiveAuthMock().Object,
            new ConditionRules());
    }

    private static BloodBondService CreateService(
        DbContextOptions<ApplicationDbContext> options,
        IAuthorizationHelper? auth = null,
        Mock<ISessionService>? sessionMock = null,
        IBeatLedgerService? beatLedger = null)
    {
        var session = sessionMock?.Object ?? CreateSessionMock().Object;
        var ledger = beatLedger ?? CreateBeatLedgerMock().Object;
        return new BloodBondService(
            new TestApplicationDbContextFactory(options),
            auth ?? CreatePermissiveAuthMock().Object,
            new ConditionRules(),
            ledger,
            new CharacterCreationRules(),
            new RelationshipWebMetrics(_meterFactory.Value),
            session,
            NullLogger<BloodBondService>.Instance);
    }

    private static Mock<IBeatLedgerService> CreateBeatLedgerMock()
    {
        var mock = new Mock<IBeatLedgerService>();
        mock.Setup(b => b.RecordBeatAsync(It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<BeatSource>(), It.IsAny<string>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
        mock.Setup(b => b.RecordXpCreditAsync(It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int>(), It.IsAny<XpSource>(), It.IsAny<string>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
        return mock;
    }

    private static Mock<ISessionService> CreateSessionMock()
    {
        var mock = new Mock<ISessionService>();
        mock.Setup(s => s.BroadcastCharacterUpdateAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
        mock.Setup(s => s.BroadcastRelationshipUpdateAsync(It.IsAny<int>(), It.IsAny<RelationshipUpdateDto>()))
            .Returns(Task.CompletedTask);
        mock.Setup(s => s.NotifyConditionToastAsync(It.IsAny<string>(), It.IsAny<ConditionNotificationDto>()))
            .Returns(Task.CompletedTask);
        return mock;
    }

    private static Mock<IAuthorizationHelper> CreatePermissiveAuthMock()
    {
        var mock = new Mock<IAuthorizationHelper>();
        mock.Setup(a => a.RequireStorytellerAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        mock.Setup(a => a.RequireCharacterAccessAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        return mock;
    }

    private static async Task SeedCampaignWithKindredAsync(ApplicationDbContext ctx)
    {
        ctx.Campaigns.Add(new Campaign { Id = 1, Name = "C1", StoryTellerId = "st" });
        ctx.Clans.Add(new Clan { Id = 1, Name = "Gangrel" });
        ctx.Characters.Add(new Character
        {
            Id = 1,
            Name = "Regnant",
            ApplicationUserId = "u1",
            ClanId = 1,
            CampaignId = 1,
            BloodPotency = 3,
            Beats = 0,
        });
        ctx.Characters.Add(new Character
        {
            Id = 2,
            Name = "Thrall",
            ApplicationUserId = "u2",
            ClanId = 1,
            CampaignId = 1,
            BloodPotency = 2,
            Beats = 0,
        });
        ctx.ChronicleNpcs.Add(new ChronicleNpc
        {
            Id = 1,
            CampaignId = 1,
            Name = "Story NPC",
            PublicDescription = "x",
            StorytellerNotes = string.Empty,
        });
        await ctx.SaveChangesAsync();
    }

    [Fact]
    public async Task RecordFeeding_FirstCreatesStage1_WithAddictedTagged()
    {
        DbContextOptions<ApplicationDbContext> options = CreateOptions(nameof(RecordFeeding_FirstCreatesStage1_WithAddictedTagged));
        await using var ctx = new ApplicationDbContext(options);
        await SeedCampaignWithKindredAsync(ctx);
        BloodBondService sut = CreateService(options);

        Result<BloodBondDto> result = await sut.RecordFeedingAsync(
            new RecordFeedingRequest(1, 2, RegnantCharacterId: 1, RegnantNpcId: null, RegnantDisplayName: null, Notes: null),
            "st");

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.Stage);
        ctx.ChangeTracker.Clear();
        BloodBond bond = await ctx.BloodBonds.AsNoTracking().SingleAsync();
        Assert.Equal(1, bond.Stage);
        CharacterCondition cond = await ctx.CharacterConditions.AsNoTracking().SingleAsync();
        Assert.Equal(ConditionType.Addicted, cond.ConditionType);
        Assert.Equal($"bloodbond:{bond.Id}", cond.SourceTag);
        Assert.False(cond.AwardsBeat);
    }

    [Fact]
    public async Task RecordFeeding_EscalatesThroughStages_AndRefeedAtThreeOnlyUpdatesTimestamp()
    {
        DbContextOptions<ApplicationDbContext> options = CreateOptions(nameof(RecordFeeding_EscalatesThroughStages_AndRefeedAtThreeOnlyUpdatesTimestamp));
        await using var ctx = new ApplicationDbContext(options);
        await SeedCampaignWithKindredAsync(ctx);
        var sessionMock = CreateSessionMock();
        BloodBondService sut = CreateService(options, sessionMock: sessionMock);

        await sut.RecordFeedingAsync(
            new RecordFeedingRequest(1, 2, 1, null, null, null),
            "st");
        await sut.RecordFeedingAsync(
            new RecordFeedingRequest(1, 2, 1, null, null, null),
            "st");

        ctx.ChangeTracker.Clear();
        DateTime mid = (await ctx.BloodBonds.AsNoTracking().SingleAsync()).LastFedAt!.Value;

        await Task.Delay(5);
        await sut.RecordFeedingAsync(
            new RecordFeedingRequest(1, 2, 1, null, null, null),
            "st");

        ctx.ChangeTracker.Clear();
        BloodBond bond = await ctx.BloodBonds.AsNoTracking().SingleAsync();
        Assert.Equal(3, bond.Stage);
        Assert.True(bond.LastFedAt > mid);

        sessionMock.Invocations.Clear();
        DateTime beforeRefresh = bond.LastFedAt!.Value;
        await Task.Delay(5);
        await sut.RecordFeedingAsync(
            new RecordFeedingRequest(1, 2, 1, null, null, null),
            "st");

        sessionMock.Verify(
            s => s.BroadcastRelationshipUpdateAsync(It.IsAny<int>(), It.IsAny<RelationshipUpdateDto>()),
            Times.Never);

        ctx.ChangeTracker.Clear();
        int boundRows = await ctx.CharacterConditions.AsNoTracking().CountAsync(c =>
            c.CharacterId == 2 && c.ConditionType == ConditionType.Bound && !c.IsResolved);
        Assert.Equal(1, boundRows);
        Assert.True((await ctx.BloodBonds.AsNoTracking().SingleAsync()).LastFedAt > beforeRefresh);
    }

    [Fact]
    public async Task RecordFeeding_Fails_WhenThrallAndRegnantPcAreSameCharacter()
    {
        DbContextOptions<ApplicationDbContext> options = CreateOptions(nameof(RecordFeeding_Fails_WhenThrallAndRegnantPcAreSameCharacter));
        await using var ctx = new ApplicationDbContext(options);
        await SeedCampaignWithKindredAsync(ctx);
        BloodBondService sut = CreateService(options);

        Result<BloodBondDto> result = await sut.RecordFeedingAsync(
            new RecordFeedingRequest(1, 2, RegnantCharacterId: 2, RegnantNpcId: null, RegnantDisplayName: null, Notes: null),
            "st");

        Assert.False(result.IsSuccess);
        Assert.Contains("themselves", result.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, await ctx.BloodBonds.AsNoTracking().CountAsync());
    }

    [Fact]
    public async Task RecordFeeding_DisplayNameCollision_IsSingleBondEscalated()
    {
        DbContextOptions<ApplicationDbContext> options = CreateOptions(nameof(RecordFeeding_DisplayNameCollision_IsSingleBondEscalated));
        await using var ctx = new ApplicationDbContext(options);
        await SeedCampaignWithKindredAsync(ctx);
        BloodBondService sut = CreateService(options);

        await sut.RecordFeedingAsync(
            new RecordFeedingRequest(1, 2, null, null, "Mira", null),
            "st");
        await sut.RecordFeedingAsync(
            new RecordFeedingRequest(1, 2, null, null, "  mira  ", null),
            "st");

        ctx.ChangeTracker.Clear();
        Assert.Equal(1, await ctx.BloodBonds.AsNoTracking().CountAsync());
        Assert.Equal(2, (await ctx.BloodBonds.AsNoTracking().SingleAsync()).Stage);
    }

    [Fact]
    public async Task FadeBond_FromStage3_ResolvesBound_AwardsBeat_AndAppliesSwooned()
    {
        DbContextOptions<ApplicationDbContext> options = CreateOptions(nameof(FadeBond_FromStage3_ResolvesBound_AwardsBeat_AndAppliesSwooned));
        await using var ctx = new ApplicationDbContext(options);
        await SeedCampaignWithKindredAsync(ctx);
        BloodBondService sut = CreateService(options);

        await sut.RecordFeedingAsync(new RecordFeedingRequest(1, 2, 1, null, null, null), "st");
        await sut.RecordFeedingAsync(new RecordFeedingRequest(1, 2, 1, null, null, null), "st");
        await sut.RecordFeedingAsync(new RecordFeedingRequest(1, 2, 1, null, null, null), "st");

        ctx.ChangeTracker.Clear();
        int bondId = (await ctx.BloodBonds.AsNoTracking().SingleAsync()).Id;
        Character thrallBefore = await ctx.Characters.AsNoTracking().FirstAsync(c => c.Id == 2);
        int beatsBefore = thrallBefore.Beats;

        Result<Unit> fade = await sut.FadeBondAsync(bondId, "st");
        Assert.True(fade.IsSuccess);

        ctx.ChangeTracker.Clear();
        Character thrallAfter = await ctx.Characters.AsNoTracking().FirstAsync(c => c.Id == 2);
        Assert.True(thrallAfter.Beats > beatsBefore);

        BloodBond bond = await ctx.BloodBonds.AsNoTracking().SingleAsync();
        Assert.Equal(2, bond.Stage);
        Assert.Contains(
            await ctx.CharacterConditions.AsNoTracking().Where(c => c.CharacterId == 2 && !c.IsResolved).ToListAsync(),
            c => c.ConditionType == ConditionType.Swooned && c.SourceTag == $"bloodbond:{bondId}");
    }

    [Fact]
    public async Task FadeBond_FromStage2_ResolvesOnlyBondSwooned_LeavesUntaggedSwooned()
    {
        DbContextOptions<ApplicationDbContext> options = CreateOptions(nameof(FadeBond_FromStage2_ResolvesOnlyBondSwooned_LeavesUntaggedSwooned));
        await using var ctx = new ApplicationDbContext(options);
        await SeedCampaignWithKindredAsync(ctx);
        BloodBondService sut = CreateService(options);

        await sut.RecordFeedingAsync(new RecordFeedingRequest(1, 2, 1, null, null, null), "st");
        await sut.RecordFeedingAsync(new RecordFeedingRequest(1, 2, 1, null, null, null), "st");

        ctx.ChangeTracker.Clear();
        int bondId = (await ctx.BloodBonds.AsNoTracking().SingleAsync()).Id;
        ctx.CharacterConditions.Add(new CharacterCondition
        {
            CharacterId = 2,
            ConditionType = ConditionType.Swooned,
            AppliedAt = DateTime.UtcNow,
            AwardsBeat = true,
            AppliedByUserId = "st",
            SourceTag = null,
        });
        await ctx.SaveChangesAsync();

        await sut.FadeBondAsync(bondId, "st");

        ctx.ChangeTracker.Clear();
        int activeUntagged = await ctx.CharacterConditions.AsNoTracking().CountAsync(c =>
            c.CharacterId == 2 && c.ConditionType == ConditionType.Swooned && !c.IsResolved && c.SourceTag == null);
        Assert.Equal(1, activeUntagged);
    }

    [Fact]
    public async Task RecordFeeding_Throws_WhenStorytellerDenied()
    {
        DbContextOptions<ApplicationDbContext> options = CreateOptions(nameof(RecordFeeding_Throws_WhenStorytellerDenied));
        await using var ctx = new ApplicationDbContext(options);
        await SeedCampaignWithKindredAsync(ctx);

        var auth = new Mock<IAuthorizationHelper>();
        auth.Setup(a => a.RequireStorytellerAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new UnauthorizedAccessException());

        BloodBondService sut = CreateService(options, auth: auth.Object);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            sut.RecordFeedingAsync(new RecordFeedingRequest(1, 2, 1, null, null, null), "not-st"));
    }

    [Fact]
    public async Task GetFadingAlerts_ReturnsBondsPastThreshold()
    {
        DbContextOptions<ApplicationDbContext> options = CreateOptions(nameof(GetFadingAlerts_ReturnsBondsPastThreshold));
        await using var ctx = new ApplicationDbContext(options);
        await SeedCampaignWithKindredAsync(ctx);
        DateTime old = DateTime.UtcNow - TimeSpan.FromDays(31);
        ctx.BloodBonds.Add(new BloodBond
        {
            ChronicleId = 1,
            ThrallCharacterId = 2,
            RegnantCharacterId = 1,
            RegnantKey = BloodBondRegnantKey.ForCharacter(1),
            Stage = 1,
            LastFedAt = old,
            CreatedAt = old,
        });
        await ctx.SaveChangesAsync();

        BloodBondQueryService sut = CreateQueryService(options);
        IReadOnlyList<BloodBondDto> fading = await sut.GetFadingAlertsAsync(1, "st");

        Assert.Single(fading);
        Assert.True(fading[0].IsFading);
    }
}
