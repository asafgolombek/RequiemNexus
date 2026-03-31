using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.DTOs;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Application.Services;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.Models.Enums;
using RequiemNexus.Data.RealTime;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Models;
using Xunit;

namespace RequiemNexus.Application.Tests;

/// <summary>
/// Tests for <see cref="EncounterParticipantService"/> passive Predatory Aura hooks (Phase 18).
/// </summary>
public class EncounterParticipantServiceTests
{
    private static DbContextOptions<ApplicationDbContext> CreateOptions(string dbName)
    {
        var root = new InMemoryDatabaseRoot();
        return new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName, root)
            .Options;
    }

    private static Mock<IAuthorizationHelper> CreatePermissiveAuthMock()
    {
        var mock = new Mock<IAuthorizationHelper>();
        mock.Setup(a => a.RequireStorytellerAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        return mock;
    }

    private static Mock<ISessionService> CreateSessionMock()
    {
        var mock = new Mock<ISessionService>();
        mock.Setup(s => s.UpdateInitiativeAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<IEnumerable<InitiativeEntryDto>>()))
            .Returns(Task.CompletedTask);
        return mock;
    }

    private static EncounterParticipantService CreateSut(
        ApplicationDbContext ctx,
        Mock<IPredatoryAuraService> auraMock,
        IAuthorizationHelper? auth = null) =>
        new(
            ctx,
            auth ?? CreatePermissiveAuthMock().Object,
            CreateSessionMock().Object,
            auraMock.Object);

    private static async Task SeedEncounterWithVampiresAsync(ApplicationDbContext ctx, bool secondIsMortal = false)
    {
        ctx.Campaigns.Add(new Campaign { Id = 1, Name = "Chronicle", StoryTellerId = "st" });
        ctx.Clans.Add(new Clan { Id = 1, Name = "Gangrel" });
        ctx.CombatEncounters.Add(new CombatEncounter
        {
            Id = 10,
            CampaignId = 1,
            Name = "Brawl",
            IsActive = true,
            IsDraft = false,
        });

        ctx.Characters.Add(CreateKindred(1, "Alpha", CreatureType.Vampire));
        ctx.Characters.Add(CreateKindred(2, "Beta", secondIsMortal ? CreatureType.Mortal : CreatureType.Vampire));
        await ctx.SaveChangesAsync();
    }

    private static Character CreateKindred(int id, string name, CreatureType type) =>
        new()
        {
            Id = id,
            Name = name,
            ApplicationUserId = $"user-{id}",
            ClanId = 1,
            CampaignId = 1,
            CreatureType = type,
            Attributes =
            [
                new CharacterAttribute { Name = "Wits", Rating = 2, Category = TraitCategory.Mental },
                new CharacterAttribute { Name = "Composure", Rating = 2, Category = TraitCategory.Social },
            ],
        };

    [Fact]
    public async Task BulkAddOnlinePlayers_VampirePair_TriggersPassiveAuraOnceForSecondArrival()
    {
        string db = nameof(BulkAddOnlinePlayers_VampirePair_TriggersPassiveAuraOnceForSecondArrival);
        var options = CreateOptions(db);
        await using var ctx = new ApplicationDbContext(options);
        await SeedEncounterWithVampiresAsync(ctx);

        var auraMock = new Mock<IPredatoryAuraService>();
        auraMock
            .Setup(a => a.ResolvePassiveContestAsync(1, 2, 1, "st", 10))
            .ReturnsAsync(Result<PredatoryAuraContestResultDto?>.Success(null));

        EncounterParticipantService sut = CreateSut(ctx, auraMock);

        await sut.BulkAddOnlinePlayersAsync(10, [1, 2], "st");

        auraMock.Verify(
            a => a.ResolvePassiveContestAsync(1, 2, 1, "st", 10),
            Times.Once);
        auraMock.Verify(
            a => a.ResolvePassiveContestAsync(1, 1, It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int?>()),
            Times.Never);
    }

    [Fact]
    public async Task AddCharacterToEncounter_VampireJoinsVampire_TriggersPassiveAura()
    {
        string db = nameof(AddCharacterToEncounter_VampireJoinsVampire_TriggersPassiveAura);
        var options = CreateOptions(db);
        await using var ctx = new ApplicationDbContext(options);
        await SeedEncounterWithVampiresAsync(ctx);

        ctx.InitiativeEntries.Add(new InitiativeEntry
        {
            EncounterId = 10,
            CharacterId = 1,
            InitiativeMod = 4,
            RollResult = 5,
            Total = 9,
            Order = 1,
        });
        await ctx.SaveChangesAsync();

        var auraMock = new Mock<IPredatoryAuraService>();
        auraMock
            .Setup(a => a.ResolvePassiveContestAsync(1, 2, 1, "st", 10))
            .ReturnsAsync(Result<PredatoryAuraContestResultDto?>.Success(null));

        EncounterParticipantService sut = CreateSut(ctx, auraMock);

        await sut.AddCharacterToEncounterAsync(10, 2, 3, 4, "st");

        auraMock.Verify(
            a => a.ResolvePassiveContestAsync(1, 2, 1, "st", 10),
            Times.Once);
    }

    [Fact]
    public async Task AddCharacterToEncounter_MortalJoinsVampire_SkipsPassiveAura()
    {
        string db = nameof(AddCharacterToEncounter_MortalJoinsVampire_SkipsPassiveAura);
        var options = CreateOptions(db);
        await using var ctx = new ApplicationDbContext(options);
        await SeedEncounterWithVampiresAsync(ctx, secondIsMortal: true);

        ctx.InitiativeEntries.Add(new InitiativeEntry
        {
            EncounterId = 10,
            CharacterId = 1,
            InitiativeMod = 4,
            RollResult = 5,
            Total = 9,
            Order = 1,
        });
        await ctx.SaveChangesAsync();

        var auraMock = new Mock<IPredatoryAuraService>();

        EncounterParticipantService sut = CreateSut(ctx, auraMock);

        await sut.AddCharacterToEncounterAsync(10, 2, 3, 4, "st");

        auraMock.Verify(
            a => a.ResolvePassiveContestAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int?>()),
            Times.Never);
    }

    [Fact]
    public async Task BulkAddOnlinePlayers_RepeatedBulkForSameIds_DoesNotCallAuraAgain()
    {
        string db = nameof(BulkAddOnlinePlayers_RepeatedBulkForSameIds_DoesNotCallAuraAgain);
        var options = CreateOptions(db);
        await using var ctx = new ApplicationDbContext(options);
        await SeedEncounterWithVampiresAsync(ctx);

        var auraMock = new Mock<IPredatoryAuraService>();
        auraMock
            .Setup(a => a.ResolvePassiveContestAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int?>()))
            .ReturnsAsync(Result<PredatoryAuraContestResultDto?>.Success(null));

        EncounterParticipantService sut = CreateSut(ctx, auraMock);

        await sut.BulkAddOnlinePlayersAsync(10, [1, 2], "st");
        await sut.BulkAddOnlinePlayersAsync(10, [1, 2], "st");

        auraMock.Verify(
            a => a.ResolvePassiveContestAsync(1, 2, 1, "st", 10),
            Times.Once);
    }
}
