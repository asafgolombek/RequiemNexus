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
using RequiemNexus.Data.Models.Enums;
using RequiemNexus.Domain.Contracts;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Models;
using RequiemNexus.Domain.Services;
using Xunit;

namespace RequiemNexus.Application.Tests;

/// <summary>
/// Integration-style tests for <see cref="PredatoryAuraService"/>.
/// </summary>
public class PredatoryAuraServiceTests
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

    private static PredatoryAuraService CreateService(
        DbContextOptions<ApplicationDbContext> options,
        IAuthorizationHelper? authHelper = null,
        IDiceService? diceService = null,
        Mock<ISessionService>? sessionMock = null)
    {
        var session = sessionMock?.Object ?? CreateSessionMock().Object;
        var dice = diceService ?? CreateDiceSequenceMock(2, 1).Object;
        return new PredatoryAuraService(
            new TestApplicationDbContextFactory(options),
            authHelper ?? CreatePermissiveAuthMock().Object,
            dice,
            new ConditionRules(),
            new RelationshipWebMetrics(_meterFactory.Value),
            session,
            NullLogger<PredatoryAuraService>.Instance);
    }

    private static Mock<IDiceService> CreateDiceSequenceMock(int firstSuccesses, int secondSuccesses)
    {
        var mock = new Mock<IDiceService>();
        mock.SetupSequence(d => d.Roll(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int?>()))
            .Returns(new RollResult { Successes = firstSuccesses })
            .Returns(new RollResult { Successes = secondSuccesses });
        return mock;
    }

    private static Mock<ISessionService> CreateSessionMock()
    {
        var mock = new Mock<ISessionService>();
        mock.Setup(s => s.BroadcastCharacterUpdateAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
        mock.Setup(s => s.BroadcastRelationshipUpdateAsync(It.IsAny<int>(), It.IsAny<RequiemNexus.Data.RealTime.RelationshipUpdateDto>()))
            .Returns(Task.CompletedTask);
        mock.Setup(s => s.NotifyConditionToastAsync(It.IsAny<string>(), It.IsAny<RequiemNexus.Data.RealTime.ConditionNotificationDto>()))
            .Returns(Task.CompletedTask);
        mock.Setup(s => s.PublishDiceRollAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int?>(),
                It.IsAny<string>(),
                It.IsAny<RollResult>()))
            .Returns(Task.CompletedTask);
        return mock;
    }

    private static Mock<IAuthorizationHelper> CreatePermissiveAuthMock()
    {
        var mock = new Mock<IAuthorizationHelper>();
        mock.Setup(a => a.RequireStorytellerAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        mock.Setup(a => a.RequireCharacterOwnerAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        return mock;
    }

    private static async Task SeedChronicleWithThreeKindredAsync(ApplicationDbContext ctx)
    {
        ctx.Campaigns.Add(new Campaign { Id = 1, Name = "C1", StoryTellerId = "st" });
        ctx.Campaigns.Add(new Campaign { Id = 2, Name = "C2", StoryTellerId = "st" });
        ctx.Clans.Add(new Clan { Id = 1, Name = "Gangrel" });
        ctx.Characters.Add(new Character
        {
            Id = 1,
            Name = "Attacker",
            ApplicationUserId = "u1",
            ClanId = 1,
            CampaignId = 1,
            BloodPotency = 3,
            Beats = 0,
        });
        ctx.Characters.Add(new Character
        {
            Id = 2,
            Name = "Defender",
            ApplicationUserId = "u2",
            ClanId = 1,
            CampaignId = 1,
            BloodPotency = 2,
            Beats = 0,
        });
        ctx.Characters.Add(new Character
        {
            Id = 3,
            Name = "Other",
            ApplicationUserId = "u3",
            ClanId = 1,
            CampaignId = 1,
            BloodPotency = 2,
            Beats = 0,
        });
        ctx.Characters.Add(new Character
        {
            Id = 4,
            Name = "OtherChronicle",
            ApplicationUserId = "u4",
            ClanId = 1,
            CampaignId = 2,
            BloodPotency = 2,
            Beats = 0,
        });
        await ctx.SaveChangesAsync();
    }

    [Fact]
    public async Task ResolveLashOut_AttackerWins_DefenderGetsShaken()
    {
        DbContextOptions<ApplicationDbContext> options = CreateOptions(nameof(ResolveLashOut_AttackerWins_DefenderGetsShaken));
        await using var ctx = new ApplicationDbContext(options);
        await SeedChronicleWithThreeKindredAsync(ctx);

        Mock<IDiceService> dice = CreateDiceSequenceMock(3, 1);
        PredatoryAuraService sut = CreateService(options, diceService: dice.Object);

        Result<PredatoryAuraContestResultDto> result = await sut.ResolveLashOutAsync(1, 1, 2, "u1");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(PredatoryAuraOutcome.AttackerWins, result.Value!.Outcome);
        Assert.Equal(1, result.Value.WinnerCharacterId);
        Assert.Equal("Shaken", result.Value.OutcomeApplied);
        Assert.Equal("Shaken", result.Value.AppliedConditionToLoser);

        await using var verify = new ApplicationDbContext(options);
        CharacterCondition? cond = await verify.CharacterConditions
            .FirstOrDefaultAsync(c => c.CharacterId == 2 && c.ConditionType == ConditionType.Shaken);
        Assert.NotNull(cond);
        Assert.StartsWith("predatoryaura:", cond!.SourceTag ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ResolveLashOut_DefenderWins_AttackerGetsShaken()
    {
        DbContextOptions<ApplicationDbContext> options = CreateOptions(nameof(ResolveLashOut_DefenderWins_AttackerGetsShaken));
        await using var ctx = new ApplicationDbContext(options);
        await SeedChronicleWithThreeKindredAsync(ctx);

        Mock<IDiceService> dice = CreateDiceSequenceMock(1, 4);
        PredatoryAuraService sut = CreateService(options, diceService: dice.Object);

        Result<PredatoryAuraContestResultDto> result = await sut.ResolveLashOutAsync(1, 1, 2, "u1");

        Assert.True(result.IsSuccess);
        Assert.Equal(PredatoryAuraOutcome.DefenderWins, result.Value!.Outcome);
        Assert.Equal(2, result.Value.WinnerCharacterId);
        Assert.Equal(2, result.Value.DefenderCharacterId);

        await using var verify = new ApplicationDbContext(options);
        CharacterCondition? cond = await verify.CharacterConditions
            .FirstOrDefaultAsync(c => c.CharacterId == 1 && c.ConditionType == ConditionType.Shaken);
        Assert.NotNull(cond);
    }

    [Fact]
    public async Task ResolveLashOut_TieBrokenByHigherBloodPotency_AttackerWins()
    {
        DbContextOptions<ApplicationDbContext> options = CreateOptions(nameof(ResolveLashOut_TieBrokenByHigherBloodPotency_AttackerWins));
        await using var ctx = new ApplicationDbContext(options);
        await SeedChronicleWithThreeKindredAsync(ctx);

        Mock<IDiceService> dice = CreateDiceSequenceMock(2, 2);
        PredatoryAuraService sut = CreateService(options, diceService: dice.Object);

        Result<PredatoryAuraContestResultDto> result = await sut.ResolveLashOutAsync(1, 1, 2, "u1");

        Assert.True(result.IsSuccess);
        Assert.Equal(PredatoryAuraOutcome.AttackerWins, result.Value!.Outcome);
        Assert.Equal(1, result.Value.WinnerCharacterId);

        await using var verify = new ApplicationDbContext(options);
        Assert.True(await verify.CharacterConditions.AnyAsync(c => c.CharacterId == 2 && c.ConditionType == ConditionType.Shaken));
    }

    [Fact]
    public async Task ResolveLashOut_TrueDraw_NoShaken_NoWinner()
    {
        DbContextOptions<ApplicationDbContext> options = CreateOptions(nameof(ResolveLashOut_TrueDraw_NoShaken_NoWinner));
        await using var ctx = new ApplicationDbContext(options);
        await SeedChronicleWithThreeKindredAsync(ctx);

        Character attacker = ctx.Characters.First(c => c.Id == 1);
        attacker.BloodPotency = 2;
        await ctx.SaveChangesAsync();

        Mock<IDiceService> dice = CreateDiceSequenceMock(2, 2);
        PredatoryAuraService sut = CreateService(options, diceService: dice.Object);

        Result<PredatoryAuraContestResultDto> result = await sut.ResolveLashOutAsync(1, 1, 2, "u1");

        Assert.True(result.IsSuccess);
        Assert.Equal(PredatoryAuraOutcome.Draw, result.Value!.Outcome);
        Assert.Null(result.Value.WinnerCharacterId);
        Assert.Equal("Draw", result.Value.OutcomeApplied);
        Assert.Null(result.Value.AppliedConditionToLoser);

        await using var verify = new ApplicationDbContext(options);
        Assert.False(await verify.CharacterConditions.AnyAsync(c => c.SourceTag != null && c.SourceTag.StartsWith("predatoryaura:", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task ResolveLashOut_CrossChronicle_ReturnsFailure()
    {
        DbContextOptions<ApplicationDbContext> options = CreateOptions(nameof(ResolveLashOut_CrossChronicle_ReturnsFailure));
        await using var ctx = new ApplicationDbContext(options);
        await SeedChronicleWithThreeKindredAsync(ctx);

        PredatoryAuraService sut = CreateService(options);

        Result<PredatoryAuraContestResultDto> result = await sut.ResolveLashOutAsync(1, 1, 4, "st");

        Assert.False(result.IsSuccess);
        Assert.Contains("chronicle", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ResolveLashOut_DefenderOwnerWithoutAttacker_ReturnsFailure()
    {
        DbContextOptions<ApplicationDbContext> options = CreateOptions(nameof(ResolveLashOut_DefenderOwnerWithoutAttacker_ReturnsFailure));
        await using var ctx = new ApplicationDbContext(options);
        await SeedChronicleWithThreeKindredAsync(ctx);

        PredatoryAuraService sut = CreateService(options);

        Result<PredatoryAuraContestResultDto> result = await sut.ResolveLashOutAsync(1, 1, 2, "u2");

        Assert.False(result.IsSuccess);
        Assert.Contains("attacking character's owner", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ResolveLashOut_NonParticipant_ThrowsUnauthorized()
    {
        DbContextOptions<ApplicationDbContext> options = CreateOptions(nameof(ResolveLashOut_NonParticipant_ThrowsUnauthorized));
        await using var ctx = new ApplicationDbContext(options);
        await SeedChronicleWithThreeKindredAsync(ctx);

        var auth = new Mock<IAuthorizationHelper>();
        auth.Setup(a => a.RequireCharacterOwnerAsync(1, "stranger", It.IsAny<string>()))
            .ThrowsAsync(new UnauthorizedAccessException());

        PredatoryAuraService sut = CreateService(options, authHelper: auth.Object);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            sut.ResolveLashOutAsync(1, 1, 2, "stranger"));
    }

    [Fact]
    public async Task GetRecentContests_StorytellerSeesRows()
    {
        DbContextOptions<ApplicationDbContext> options = CreateOptions(nameof(GetRecentContests_StorytellerSeesRows));
        await using var ctx = new ApplicationDbContext(options);
        await SeedChronicleWithThreeKindredAsync(ctx);

        PredatoryAuraService sut = CreateService(options);
        await sut.ResolveLashOutAsync(1, 1, 2, "u1");

        IReadOnlyList<PredatoryAuraContestSummaryDto> rows = await sut.GetRecentContestsAsync(1, "st", 10);

        Assert.Single(rows);
        Assert.Equal("Attacker", rows[0].AttackerName);
        Assert.Equal("Defender", rows[0].DefenderName);
    }

    [Fact]
    public async Task ResolvePassiveContest_EncounterDuplicate_ReturnsNullSecondTime()
    {
        DbContextOptions<ApplicationDbContext> options =
            CreateOptions(nameof(ResolvePassiveContest_EncounterDuplicate_ReturnsNullSecondTime));
        await using var ctx = new ApplicationDbContext(options);
        await SeedChronicleWithThreeKindredAsync(ctx);
        ctx.CombatEncounters.Add(new CombatEncounter
        {
            Id = 50,
            CampaignId = 1,
            Name = "Skirmish",
            IsActive = true,
            IsDraft = false,
            IsPaused = false,
            CurrentRound = 1,
        });
        await ctx.SaveChangesAsync();

        Mock<IDiceService> dice = CreateDiceSequenceMock(2, 1);
        PredatoryAuraService sut = CreateService(options, diceService: dice.Object);

        Result<PredatoryAuraContestResultDto?> first =
            await sut.ResolvePassiveContestAsync(1, 1, 2, "st", 50);

        Assert.True(first.IsSuccess);
        Assert.NotNull(first.Value);

        Result<PredatoryAuraContestResultDto?> second =
            await sut.ResolvePassiveContestAsync(1, 1, 2, "st", 50);

        Assert.True(second.IsSuccess);
        Assert.Null(second.Value);

        await using var verify = new ApplicationDbContext(options);
        Assert.Equal(1, await verify.EncounterAuraContests.CountAsync());
    }

    [Fact]
    public async Task ResolvePassiveContest_NonVampire_ReturnsFailure()
    {
        DbContextOptions<ApplicationDbContext> options = CreateOptions(nameof(ResolvePassiveContest_NonVampire_ReturnsFailure));
        await using var ctx = new ApplicationDbContext(options);
        await SeedChronicleWithThreeKindredAsync(ctx);
        Character mortal = await ctx.Characters.FirstAsync(c => c.Id == 2);
        mortal.CreatureType = CreatureType.Mortal;
        await ctx.SaveChangesAsync();

        PredatoryAuraService sut = CreateService(options);

        Result<PredatoryAuraContestResultDto?> result = await sut.ResolvePassiveContestAsync(1, 1, 2, "st", null);

        Assert.False(result.IsSuccess);
        Assert.Contains("Kindred", result.Error!, StringComparison.Ordinal);
    }
}
