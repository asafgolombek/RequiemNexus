using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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

    private static SocialManeuverLifecycleCoordinator CreateLifecycleCoordinator(
        ISessionPublisher publisher,
        IConditionService conditions) =>
        new(
            conditions,
            publisher,
            NullLogger<SocialManeuverLifecycleCoordinator>.Instance);

    private static SocialManeuveringService CreateService(
        DbContextOptions<ApplicationDbContext> options,
        IAuthorizationHelper? authHelper = null,
        ISessionPublisher? sessionPublisher = null,
        IConditionService? conditionService = null)
    {
        var auth = authHelper ?? CreatePermissiveAuthMock().Object;
        var logger = new Mock<ILogger<SocialManeuveringService>>().Object;
        var publisher = sessionPublisher ?? CreateSessionPublisherMock().Object;
        var conditions = conditionService ?? CreateConditionNoOpMock().Object;
        var lifecycle = CreateLifecycleCoordinator(publisher, conditions);
        return new SocialManeuveringService(
            new TestApplicationDbContextFactory(options),
            auth,
            lifecycle,
            logger);
    }

    private static SocialManeuverRollService CreateRollService(
        DbContextOptions<ApplicationDbContext> options,
        IAuthorizationHelper? authHelper = null,
        IDiceService? diceService = null,
        IConditionService? conditionService = null,
        ISessionPublisher? sessionPublisher = null)
    {
        var auth = authHelper ?? CreatePermissiveAuthMock().Object;
        var dice = diceService ?? CreateDiceMock(1).Object;
        var publisher = sessionPublisher ?? CreateSessionPublisherMock().Object;
        var conditions = conditionService ?? CreateConditionNoOpMock().Object;
        var lifecycle = CreateLifecycleCoordinator(publisher, conditions);
        var session = new Mock<ISessionService>();
        session
            .Setup(s => s.PublishDiceRollAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int?>(),
                It.IsAny<string>(),
                It.IsAny<RollResult>()))
            .Returns(Task.CompletedTask);
        return new SocialManeuverRollService(
            new TestApplicationDbContextFactory(options),
            auth,
            dice,
            lifecycle,
            session.Object,
            NullLogger<SocialManeuverRollService>.Instance);
    }

    private static SocialManeuverQueryService CreateQueryService(
        DbContextOptions<ApplicationDbContext> options,
        IAuthorizationHelper? authHelper = null)
    {
        var auth = authHelper ?? CreatePermissiveAuthMock().Object;
        return new SocialManeuverQueryService(
            new TestApplicationDbContextFactory(options),
            auth);
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
        pub.Setup(p => p.User(It.IsAny<string>())).Returns(client.Object);
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
            Attributes =
            [
                new CharacterAttribute { Name = "Manipulation", Rating = 4, Category = TraitCategory.Social },
                new CharacterAttribute { Name = "Presence", Rating = 4, Category = TraitCategory.Social },
                new CharacterAttribute { Name = "Intelligence", Rating = 2, Category = TraitCategory.Mental },
                new CharacterAttribute { Name = "Wits", Rating = 2, Category = TraitCategory.Mental },
            ],
            Skills =
            [
                new CharacterSkill { Name = "Persuasion", Rating = 4, Category = TraitCategory.Social },
                new CharacterSkill { Name = "Socialize", Rating = 3, Category = TraitCategory.Social },
            ],
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
        var mutService = CreateService(options);
        var rollService = CreateRollService(options, diceService: CreateDiceMock(3).Object);

        SocialManeuver created = await mutService.CreateAsync(
            1, 1, 1, "goal", false, false, false, "st-user");

        var openResult = await rollService.RollOpenDoorAsync(created.Id, 5, "player");
        Assert.True(openResult.IsSuccess);
        (SocialManeuver updated, RollResult roll, int opened) = openResult.Value!;

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
        var realAuth = new AuthorizationHelper(new TestApplicationDbContextFactory(options), NullLogger<AuthorizationHelper>.Instance);

        var mutService = CreateService(options, authHelper: realAuth);
        var rollService = CreateRollService(options, authHelper: realAuth, diceService: CreateDiceMock(1).Object);

        SocialManeuver created = await mutService.CreateAsync(
            1, 1, 1, "goal", false, false, false, "st-user");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => rollService.RollOpenDoorAsync(created.Id, 5, "stranger"));
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
        var received = new List<SocialManeuverUpdateDto>();
        client.Setup(c => c.ReceiveSocialManeuverUpdate(It.IsAny<SocialManeuverUpdateDto>()))
            .Callback<SocialManeuverUpdateDto>(received.Add)
            .Returns(Task.CompletedTask);
        var pub = new Mock<ISessionPublisher>();
        pub.Setup(p => p.Group(It.IsAny<int>())).Returns(client.Object);
        pub.Setup(p => p.User(It.IsAny<string>())).Returns(client.Object);

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

        Assert.Equal(2, received.Count);
        Assert.Contains(received, d => d.ManeuverId == m.Id && d.CampaignId == 1 && d.GoalDescription == string.Empty);
        Assert.Contains(received, d => d.ManeuverId == m.Id && d.CampaignId == 1 && d.GoalDescription == "goal");
    }

    [Fact]
    public async Task ListForCampaignAsync_IncludesInitiatorAndTargetNpcNames()
    {
        var options = CreateOptions(nameof(ListForCampaignAsync_IncludesInitiatorAndTargetNpcNames));
        using var ctx = new ApplicationDbContext(options);
        await SeedCampaignCharacterAndNpcAsync(ctx);
        var mutService = CreateService(options);
        await mutService.CreateAsync(1, 1, 1, "goal", false, false, false, "st-user");

        IReadOnlyList<SocialManeuver> list = await CreateQueryService(options).ListForCampaignAsync(1, "st-user");

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
        var mutService = CreateService(options);
        var rollService = CreateRollService(options, diceService: CreateDiceMock(5).Object, conditionService: conditions.Object);

        SocialManeuver created = await mutService.CreateAsync(
            1,
            1,
            1,
            "goal",
            goalWouldBeBreakingPoint: true,
            goalPreventsAspiration: false,
            actsAgainstVirtueOrMask: false,
            "st-user");

        var rollResult = await rollService.RollOpenDoorAsync(created.Id, 8, "player");
        Assert.True(rollResult.IsSuccess);

        conditions.Verify(
            c => c.ApplyConditionAsync(
                1,
                ConditionType.Inspired,
                null,
                It.IsAny<string?>(),
                "player"),
            Times.Once);
    }

    [Fact]
    public async Task RollOpenDoorAsync_DeclaredPoolAboveSheet_ReturnsFailure()
    {
        var options = CreateOptions(nameof(RollOpenDoorAsync_DeclaredPoolAboveSheet_ReturnsFailure));
        using var ctx = new ApplicationDbContext(options);
        await SeedCampaignCharacterAndNpcAsync(ctx);
        var mutService = CreateService(options);
        var rollService = CreateRollService(options, diceService: CreateDiceMock(1).Object);

        SocialManeuver created = await mutService.CreateAsync(
            1, 1, 1, "goal", false, false, false, "st-user");

        var result = await rollService.RollOpenDoorAsync(created.Id, 20, "player");

        Assert.False(result.IsSuccess);
        Assert.Contains("exceeds", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RollOpenDoorAsync_StorytellerMayDeclarePoolAboveSheet()
    {
        var options = CreateOptions(nameof(RollOpenDoorAsync_StorytellerMayDeclarePoolAboveSheet));
        using var ctx = new ApplicationDbContext(options);
        await SeedCampaignCharacterAndNpcAsync(ctx);
        var mutService = CreateService(options);
        var rollService = CreateRollService(options, diceService: CreateDiceMock(3).Object);

        SocialManeuver created = await mutService.CreateAsync(
            1, 1, 1, "goal", false, false, false, "st-user");

        var result = await rollService.RollOpenDoorAsync(created.Id, 25, "st-user");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task AddInterceptor_Success_ReturnsInterceptorRow()
    {
        var options = CreateOptions(nameof(AddInterceptor_Success_ReturnsInterceptorRow));
        using var ctx = new ApplicationDbContext(options);
        await SeedCampaignCharacterAndNpcAsync(ctx);
        await SeedInterceptorCharacterAsync(ctx, characterId: 2, userId: "player-b");
        var service = CreateService(options);

        SocialManeuver m = await service.CreateAsync(
            1, 1, 1, "goal", false, false, false, "st-user");

        ManeuverInterceptor row = await service.AddInterceptorAsync(m.Id, 2, "st-user");

        Assert.True(row.Id > 0);
        Assert.Equal(m.Id, row.SocialManeuverId);
        Assert.Equal(2, row.InterceptorCharacterId);
        Assert.True(row.IsActive);
        Assert.Equal(0, row.Successes);
    }

    [Fact]
    public async Task AddInterceptor_NotStoryteller_ThrowsUnauthorizedAccessException()
    {
        var options = CreateOptions(nameof(AddInterceptor_NotStoryteller_ThrowsUnauthorizedAccessException));
        using var ctx = new ApplicationDbContext(options);
        await SeedCampaignCharacterAndNpcAsync(ctx);
        await SeedInterceptorCharacterAsync(ctx, 2, "player-b");
        var realAuth = new AuthorizationHelper(new TestApplicationDbContextFactory(options), NullLogger<AuthorizationHelper>.Instance);
        var service = CreateService(options, authHelper: realAuth);

        SocialManeuver m = await service.CreateAsync(
            1, 1, 1, "goal", false, false, false, "st-user");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.AddInterceptorAsync(m.Id, 2, "stranger"));
    }

    [Fact]
    public async Task AddInterceptor_WrongChronicle_ThrowsInvalidOperationException()
    {
        var options = CreateOptions(nameof(AddInterceptor_WrongChronicle_ThrowsInvalidOperationException));
        using var ctx = new ApplicationDbContext(options);
        await SeedCampaignCharacterAndNpcAsync(ctx);
        ctx.Campaigns.Add(new Campaign { Id = 2, Name = "Other", StoryTellerId = "st-user" });
        ctx.Characters.Add(new Character
        {
            Id = 3,
            Name = "Outsider",
            ApplicationUserId = "other",
            ClanId = 1,
            CampaignId = 2,
            Humanity = 7,
        });
        await ctx.SaveChangesAsync();

        var service = CreateService(options);
        SocialManeuver m = await service.CreateAsync(
            1, 1, 1, "goal", false, false, false, "st-user");

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddInterceptorAsync(m.Id, 3, "st-user"));
    }

    [Fact]
    public async Task AddInterceptor_DuplicateCharacter_ThrowsInvalidOperationException()
    {
        var options = CreateOptions(nameof(AddInterceptor_DuplicateCharacter_ThrowsInvalidOperationException));
        using var ctx = new ApplicationDbContext(options);
        await SeedCampaignCharacterAndNpcAsync(ctx);
        await SeedInterceptorCharacterAsync(ctx, 2, "player-b");
        var service = CreateService(options);
        SocialManeuver m = await service.CreateAsync(
            1, 1, 1, "goal", false, false, false, "st-user");

        await service.AddInterceptorAsync(m.Id, 2, "st-user");

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddInterceptorAsync(m.Id, 2, "st-user"));
    }

    [Fact]
    public async Task AddInterceptor_ClosedManeuver_ThrowsInvalidOperationException()
    {
        var options = CreateOptions(nameof(AddInterceptor_ClosedManeuver_ThrowsInvalidOperationException));
        using var ctx = new ApplicationDbContext(options);
        await SeedCampaignCharacterAndNpcAsync(ctx);
        await SeedInterceptorCharacterAsync(ctx, 2, "player-b");
        var service = CreateService(options);
        SocialManeuver m = await service.CreateAsync(
            1, 1, 1, "goal", false, false, false, "st-user");

        SocialManeuver tracked = await ctx.SocialManeuvers.FirstAsync(x => x.Id == m.Id);
        tracked.Status = ManeuverStatus.Succeeded;
        await ctx.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.AddInterceptorAsync(m.Id, 2, "st-user"));
    }

    [Fact]
    public async Task RecordInterceptorRoll_AtPoolMax_Succeeds()
    {
        var options = CreateOptions(nameof(RecordInterceptorRoll_AtPoolMax_Succeeds));
        using var ctx = new ApplicationDbContext(options);
        await SeedCampaignCharacterAndNpcAsync(ctx);
        await SeedInterceptorCharacterAsync(ctx, 2, "player-b");
        var service = CreateService(options);
        SocialManeuver m = await service.CreateAsync(
            1, 1, 1, "goal", false, false, false, "st-user");

        ManeuverInterceptor row = await service.AddInterceptorAsync(m.Id, 2, "st-user");

        await service.RecordInterceptorRollAsync(row.Id, 8, "st-user");

        ManeuverInterceptor? reloaded = await ctx.ManeuverInterceptors.AsNoTracking().FirstOrDefaultAsync(i => i.Id == row.Id);
        Assert.NotNull(reloaded);
        Assert.Equal(8, reloaded.Successes);
    }

    [Fact]
    public async Task RecordInterceptorRoll_ExceedingManipulationPersuasion_ThrowsInvalidOperationException()
    {
        var options = CreateOptions(nameof(RecordInterceptorRoll_ExceedingManipulationPersuasion_ThrowsInvalidOperationException));
        using var ctx = new ApplicationDbContext(options);
        await SeedCampaignCharacterAndNpcAsync(ctx);
        await SeedInterceptorCharacterAsync(ctx, 2, "player-b");
        var service = CreateService(options);
        SocialManeuver m = await service.CreateAsync(
            1, 1, 1, "goal", false, false, false, "st-user");

        ManeuverInterceptor row = await service.AddInterceptorAsync(m.Id, 2, "st-user");

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RecordInterceptorRollAsync(row.Id, 9, "st-user"));
    }

    [Fact]
    public async Task RecordInterceptorRoll_NotStoryteller_ThrowsUnauthorizedAccessException()
    {
        var options = CreateOptions(nameof(RecordInterceptorRoll_NotStoryteller_ThrowsUnauthorizedAccessException));
        using var ctx = new ApplicationDbContext(options);
        await SeedCampaignCharacterAndNpcAsync(ctx);
        await SeedInterceptorCharacterAsync(ctx, 2, "player-b");
        var realAuth = new AuthorizationHelper(new TestApplicationDbContextFactory(options), NullLogger<AuthorizationHelper>.Instance);
        var service = CreateService(options, authHelper: realAuth);
        SocialManeuver m = await service.CreateAsync(
            1, 1, 1, "goal", false, false, false, "st-user");

        ManeuverInterceptor row = await service.AddInterceptorAsync(m.Id, 2, "st-user");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.RecordInterceptorRollAsync(row.Id, 3, "stranger"));
    }

    private static async Task SeedInterceptorCharacterAsync(ApplicationDbContext ctx, int characterId, string userId)
    {
        ctx.Characters.Add(new Character
        {
            Id = characterId,
            Name = "Interceptor",
            ApplicationUserId = userId,
            ClanId = 1,
            CampaignId = 1,
            Humanity = 7,
            Attributes =
            [
                new CharacterAttribute { Name = "Manipulation", Rating = 4, Category = TraitCategory.Social },
                new CharacterAttribute { Name = "Presence", Rating = 2, Category = TraitCategory.Social },
            ],
            Skills =
            [
                new CharacterSkill { Name = "Persuasion", Rating = 4, Category = TraitCategory.Social },
            ],
        });
        await ctx.SaveChangesAsync();
    }
}
