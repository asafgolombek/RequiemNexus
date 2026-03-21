using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Application.Services;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.Models.Enums;
using RequiemNexus.Data.RealTime;
using RequiemNexus.Domain.Contracts;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Models;
using Xunit;

namespace RequiemNexus.Application.Tests;

/// <summary>
/// Integration-style tests for <see cref="SocialManeuveringService"/> (authorization + persistence).
/// </summary>
public class SocialManeuveringServiceTests
{
    private const string _defaultAttributes = "{\"Strength\":2,\"Dexterity\":2,\"Stamina\":2,\"Intelligence\":2,\"Wits\":2,\"Resolve\":3,\"Presence\":2,\"Manipulation\":2,\"Composure\":2}";

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
        // Shared root so disposing a factory-created context does not tear down the in-memory store
        // for other contexts using the same options (matches production IDbContextFactory behavior).
        var root = new InMemoryDatabaseRoot();
        return new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName, root)
            .Options;
    }

    private static SocialManeuveringService CreateService(
        DbContextOptions<ApplicationDbContext> options,
        IAuthorizationHelper? authHelper = null,
        IDiceService? diceService = null,
        ISessionPublisher? sessionPublisher = null,
        IConditionService? conditionService = null)
    {
        var auth = authHelper ?? CreatePermissiveAuthMock().Object;
        var dice = diceService ?? CreateDiceMock(1).Object;
        var logger = new Mock<ILogger<SocialManeuveringService>>().Object;
        var publisher = sessionPublisher ?? CreateSessionPublisherMock().Object;
        var conditions = conditionService ?? CreateConditionNoOpMock().Object;
        return new SocialManeuveringService(
            new TestApplicationDbContextFactory(options),
            auth,
            dice,
            publisher,
            conditions,
            logger);
    }

    private static Mock<IConditionService> CreateConditionNoOpMock()
    {
        var mock = new Mock<IConditionService>();
        mock.Setup(c => c.ApplyConditionAsync(
                It.IsAny<int>(),
                It.IsAny<ConditionType>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string>()))
            .ReturnsAsync((int cid, ConditionType t, string? cn, string? desc, string uid) => new CharacterCondition
            {
                Id = 1,
                CharacterId = cid,
                ConditionType = t,
                CustomName = cn,
                Description = desc,
                AppliedByUserId = uid,
            });
        return mock;
    }

    private static Mock<ISessionPublisher> CreateSessionPublisherMock()
    {
        var client = new Mock<ISessionClient>();
        client.Setup(c => c.ReceiveSocialManeuverUpdate(It.IsAny<SocialManeuverUpdateDto>()))
            .Returns(Task.CompletedTask);
        var pub = new Mock<ISessionPublisher>();
        pub.Setup(p => p.Group(It.IsAny<int>())).Returns(client.Object);
        return pub;
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

    private static Mock<IDiceService> CreateDiceMock(int successes)
    {
        var mock = new Mock<IDiceService>();
        mock.Setup(d => d.Roll(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int?>()))
            .Returns(new RollResult { Successes = successes });
        return mock;
    }

    private static async Task SeedCampaignCharacterAndNpcAsync(ApplicationDbContext ctx)
    {
        ctx.Campaigns.Add(new Campaign { Id = 1, Name = "Chronicle", StoryTellerId = "st-user" });
        ctx.Clans.Add(new Clan { Id = 1, Name = "Gangrel" });
        ctx.Characters.Add(new Character
        {
            Id = 1,
            Name = "PC",
            ApplicationUserId = "player",
            ClanId = 1,
            CampaignId = 1,
            Humanity = 7,
        });
        ctx.ChronicleNpcs.Add(new ChronicleNpc
        {
            Id = 1,
            CampaignId = 1,
            Name = "NPC",
            AttributesJson = _defaultAttributes,
        });
        await ctx.SaveChangesAsync();
    }

    [Fact]
    public async Task CreateAsync_ComputesInitialDoors_FromNpcAttributes()
    {
        var options = CreateOptions(nameof(CreateAsync_ComputesInitialDoors_FromNpcAttributes));
        using var ctx = new ApplicationDbContext(options);
        await SeedCampaignCharacterAndNpcAsync(ctx);
        var service = CreateService(options);

        SocialManeuver m = await service.CreateAsync(
            1,
            1,
            1,
            "Get the macguffin",
            goalWouldBeBreakingPoint: false,
            goalPreventsAspiration: false,
            actsAgainstVirtueOrMask: false,
            "st-user");

        Assert.Equal(2, m.InitialDoors);
        Assert.Equal(2, m.RemainingDoors);
        Assert.Equal(ManeuverStatus.Active, m.Status);
    }

    [Fact]
    public async Task CreateAsync_AfterBurnt_Throws()
    {
        var options = CreateOptions(nameof(CreateAsync_AfterBurnt_Throws));
        using var ctx = new ApplicationDbContext(options);
        await SeedCampaignCharacterAndNpcAsync(ctx);
        ctx.SocialManeuvers.Add(new SocialManeuver
        {
            CampaignId = 1,
            InitiatorCharacterId = 1,
            TargetChronicleNpcId = 1,
            GoalDescription = "prior",
            InitialDoors = 2,
            RemainingDoors = 2,
            Status = ManeuverStatus.Burnt,
        });
        await ctx.SaveChangesAsync();

        var service = CreateService(options);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(
            1,
            1,
            1,
            "retry",
            false,
            false,
            false,
            "st-user"));
    }

    [Fact]
    public async Task RollOpenDoorAsync_ReducesRemainingDoors_OnSuccess()
    {
        var options = CreateOptions(nameof(RollOpenDoorAsync_ReducesRemainingDoors_OnSuccess));
        using var ctx = new ApplicationDbContext(options);
        await SeedCampaignCharacterAndNpcAsync(ctx);
        var service = CreateService(options, diceService: CreateDiceMock(3).Object);

        SocialManeuver created = await service.CreateAsync(
            1, 1, 1, "goal", false, false, false, "st-user");

        (SocialManeuver updated, RollResult roll, int opened) = await service.RollOpenDoorAsync(created.Id, 5, "player");

        Assert.Equal(3, roll.Successes);
        Assert.Equal(1, opened);
        Assert.Equal(1, updated.RemainingDoors);
    }

    [Fact]
    public async Task RollOpenDoorAsync_NonOwnerNonSt_Throws()
    {
        var options = CreateOptions(nameof(RollOpenDoorAsync_NonOwnerNonSt_Throws));
        using var ctx = new ApplicationDbContext(options);
        await SeedCampaignCharacterAndNpcAsync(ctx);
        var auth = new Mock<IAuthorizationHelper>();
        auth.Setup(a => a.RequireStorytellerAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        auth.Setup(a => a.RequireCharacterAccessAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var service = CreateService(options, authHelper: auth.Object, diceService: CreateDiceMock(1).Object);

        SocialManeuver created = await service.CreateAsync(
            1, 1, 1, "goal", false, false, false, "st-user");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.RollOpenDoorAsync(created.Id, 5, "stranger"));
    }

    [Fact]
    public async Task SetImpressionAsync_Hostile_SetsHostileSince()
    {
        var options = CreateOptions(nameof(SetImpressionAsync_Hostile_SetsHostileSince));
        using var ctx = new ApplicationDbContext(options);
        await SeedCampaignCharacterAndNpcAsync(ctx);
        var service = CreateService(options);

        SocialManeuver created = await service.CreateAsync(
            1, 1, 1, "goal", false, false, false, "st-user");

        await service.SetImpressionAsync(created.Id, ImpressionLevel.Hostile, "st-user");

        SocialManeuver? reloaded = await ctx.SocialManeuvers.AsNoTracking().FirstOrDefaultAsync(m => m.Id == created.Id);
        Assert.NotNull(reloaded);
        Assert.Equal(ImpressionLevel.Hostile, reloaded.CurrentImpression);
        Assert.NotNull(reloaded.HostileSince);
    }

    [Fact]
    public async Task LoadManeuver_HostileWeek_FailsManeuver()
    {
        var options = CreateOptions(nameof(LoadManeuver_HostileWeek_FailsManeuver));
        using var ctx = new ApplicationDbContext(options);
        await SeedCampaignCharacterAndNpcAsync(ctx);
        var service = CreateService(options);

        SocialManeuver created = await service.CreateAsync(
            1, 1, 1, "goal", false, false, false, "st-user");

        SocialManeuver tracked = await ctx.SocialManeuvers.FirstAsync(m => m.Id == created.Id);
        tracked.CurrentImpression = ImpressionLevel.Hostile;
        tracked.HostileSince = DateTimeOffset.UtcNow.AddDays(-8);
        await ctx.SaveChangesAsync();

        await service.SetRemainingDoorsNarrativeAsync(created.Id, 2, "st-user");

        SocialManeuver? reloaded = await ctx.SocialManeuvers.AsNoTracking().FirstAsync(m => m.Id == created.Id);
        Assert.Equal(ManeuverStatus.Failed, reloaded.Status);
    }

    [Fact]
    public async Task CreateAsync_PublishesSocialManeuverUpdate()
    {
        var options = CreateOptions(nameof(CreateAsync_PublishesSocialManeuverUpdate));
        using var ctx = new ApplicationDbContext(options);
        await SeedCampaignCharacterAndNpcAsync(ctx);
        var client = new Mock<ISessionClient>();
        client.Setup(c => c.ReceiveSocialManeuverUpdate(It.IsAny<SocialManeuverUpdateDto>()))
            .Returns(Task.CompletedTask);
        var pub = new Mock<ISessionPublisher>();
        pub.Setup(p => p.Group(It.IsAny<int>())).Returns(client.Object);

        SocialManeuveringService service = CreateService(options, sessionPublisher: pub.Object);

        SocialManeuver m = await service.CreateAsync(
            1,
            1,
            1,
            "goal",
            false,
            false,
            false,
            "st-user");

        client.Verify(
            c => c.ReceiveSocialManeuverUpdate(It.Is<SocialManeuverUpdateDto>(d => d.ManeuverId == m.Id && d.CampaignId == 1)),
            Times.Once);
    }

    [Fact]
    public async Task ListForCampaignAsync_IncludesInitiatorAndTargetNpcNames()
    {
        var options = CreateOptions(nameof(ListForCampaignAsync_IncludesInitiatorAndTargetNpcNames));
        using var ctx = new ApplicationDbContext(options);
        await SeedCampaignCharacterAndNpcAsync(ctx);
        var service = CreateService(options);
        await service.CreateAsync(1, 1, 1, "goal", false, false, false, "st-user");

        IReadOnlyList<SocialManeuver> list = await service.ListForCampaignAsync(1, "st-user");

        Assert.Single(list);
        Assert.Equal("PC", list[0].InitiatorCharacter?.Name);
        Assert.Equal("NPC", list[0].TargetNpc?.Name);
    }

    [Fact]
    public async Task BankInvestigationSuccessesAsync_AtThreshold_CreatesClue()
    {
        var options = CreateOptions(nameof(BankInvestigationSuccessesAsync_AtThreshold_CreatesClue));
        using var ctx = new ApplicationDbContext(options);
        await SeedCampaignCharacterAndNpcAsync(ctx);
        var service = CreateService(options);

        SocialManeuver created = await service.CreateAsync(
            1, 1, 1, "goal", false, false, false, "st-user");

        await service.BankInvestigationSuccessesAsync(created.Id, 3, "player");

        List<ManeuverClue> clues = await ctx.ManeuverClues.Where(c => c.SocialManeuverId == created.Id).ToListAsync();
        Assert.Single(clues);
        Assert.False(clues[0].IsSpent);
        SocialManeuver? m = await ctx.SocialManeuvers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == created.Id);
        Assert.NotNull(m);
        Assert.Equal(0, m.InvestigationProgressTowardNextClue);
    }

    [Fact]
    public async Task SpendManeuverClueAsync_SetsBenefitAndSpent()
    {
        var options = CreateOptions(nameof(SpendManeuverClueAsync_SetsBenefitAndSpent));
        using var ctx = new ApplicationDbContext(options);
        await SeedCampaignCharacterAndNpcAsync(ctx);
        var service = CreateService(options);

        SocialManeuver created = await service.CreateAsync(
            1, 1, 1, "goal", false, false, false, "st-user");

        ctx.ManeuverClues.Add(new ManeuverClue
        {
            SocialManeuverId = created.Id,
            SourceDescription = "ST grant",
            LeverageKind = ClueLeverageKind.Soft,
        });
        await ctx.SaveChangesAsync();

        int clueId = (await ctx.ManeuverClues.FirstAsync()).Id;

        await service.SpendManeuverClueAsync(clueId, "+1 impression shift (approved)", "player");

        ManeuverClue? reloaded = await ctx.ManeuverClues.AsNoTracking().FirstAsync(c => c.Id == clueId);
        Assert.True(reloaded.IsSpent);
        Assert.Contains("impression", reloaded.Benefit, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RollOpenDoorAsync_ExceptionalSuccess_CallsConditionService()
    {
        var options = CreateOptions(nameof(RollOpenDoorAsync_ExceptionalSuccess_CallsConditionService));
        using var ctx = new ApplicationDbContext(options);
        await SeedCampaignCharacterAndNpcAsync(ctx);
        var conditions = CreateConditionNoOpMock();
        var service = CreateService(options, diceService: CreateDiceMock(5).Object, conditionService: conditions.Object);

        SocialManeuver created = await service.CreateAsync(
            1,
            1,
            1,
            "goal",
            goalWouldBeBreakingPoint: true,
            goalPreventsAspiration: false,
            actsAgainstVirtueOrMask: false,
            "st-user");

        await service.RollOpenDoorAsync(created.Id, 8, "player");

        conditions.Verify(
            c => c.ApplyConditionAsync(
                1,
                ConditionType.Inspired,
                null,
                It.IsAny<string?>(),
                "player"),
            Times.Once);
    }
}
