using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.Services;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
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

    private static ApplicationDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ApplicationDbContext(options);
    }

    private static SocialManeuveringService CreateService(
        ApplicationDbContext ctx,
        IAuthorizationHelper? authHelper = null,
        IDiceService? diceService = null)
    {
        var auth = authHelper ?? CreatePermissiveAuthMock().Object;
        var dice = diceService ?? CreateDiceMock(1).Object;
        var logger = new Mock<ILogger<SocialManeuveringService>>().Object;
        return new SocialManeuveringService(ctx, auth, dice, logger);
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
        using var ctx = CreateContext(nameof(CreateAsync_ComputesInitialDoors_FromNpcAttributes));
        await SeedCampaignCharacterAndNpcAsync(ctx);
        var service = CreateService(ctx);

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
        using var ctx = CreateContext(nameof(CreateAsync_AfterBurnt_Throws));
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

        var service = CreateService(ctx);

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
        using var ctx = CreateContext(nameof(RollOpenDoorAsync_ReducesRemainingDoors_OnSuccess));
        await SeedCampaignCharacterAndNpcAsync(ctx);
        var service = CreateService(ctx, diceService: CreateDiceMock(3).Object);

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
        using var ctx = CreateContext(nameof(RollOpenDoorAsync_NonOwnerNonSt_Throws));
        await SeedCampaignCharacterAndNpcAsync(ctx);
        var auth = new Mock<IAuthorizationHelper>();
        auth.Setup(a => a.RequireStorytellerAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        auth.Setup(a => a.RequireCharacterAccessAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var service = CreateService(ctx, authHelper: auth.Object, diceService: CreateDiceMock(1).Object);

        SocialManeuver created = await service.CreateAsync(
            1, 1, 1, "goal", false, false, false, "st-user");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.RollOpenDoorAsync(created.Id, 5, "stranger"));
    }

    [Fact]
    public async Task SetImpressionAsync_Hostile_SetsHostileSince()
    {
        using var ctx = CreateContext(nameof(SetImpressionAsync_Hostile_SetsHostileSince));
        await SeedCampaignCharacterAndNpcAsync(ctx);
        var service = CreateService(ctx);

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
        using var ctx = CreateContext(nameof(LoadManeuver_HostileWeek_FailsManeuver));
        await SeedCampaignCharacterAndNpcAsync(ctx);
        var service = CreateService(ctx);

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
}
