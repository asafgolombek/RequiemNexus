using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.Events;
using RequiemNexus.Application.Models;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Application.Services;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Contracts;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Models;
using Xunit;

namespace RequiemNexus.Application.Tests;

public class HuntingServiceTests
{
    private static Mock<IAuthorizationHelper> CreateAuthMock()
    {
        var auth = new Mock<IAuthorizationHelper>();
        auth.Setup(a => a.RequireCharacterAccessAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        return auth;
    }

    private static async Task<(ApplicationDbContext Ctx, IAsyncDisposable Teardown)> CreateSqliteContextAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        var ctx = new ApplicationDbContext(options);
        await ctx.Database.EnsureCreatedAsync();
        IAsyncDisposable teardown = new SqliteTeardown(connection, ctx);
        return (ctx, teardown);
    }

    private sealed class SqliteTeardown(SqliteConnection connection, ApplicationDbContext context) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }

    private static async Task SeedUserCampaignCharacterAsync(
        ApplicationDbContext ctx,
        PredatorType? predatorType = PredatorType.Alleycat,
        int? campaignId = 1)
    {
        ctx.Users.Add(
            new ApplicationUser
            {
                Id = "u1",
                UserName = "u1",
                NormalizedUserName = "U1",
                Email = "u1@test",
                NormalizedEmail = "U1@TEST",
                EmailConfirmed = true,
            });
        if (campaignId is int cid)
        {
            ctx.Campaigns.Add(
                new Campaign
                {
                    Id = cid,
                    Name = "Chronicle",
                    StoryTellerId = "u1",
                });
        }

        ctx.Characters.Add(
            new Character
            {
                Id = 1,
                ApplicationUserId = "u1",
                Name = "Kindred",
                CampaignId = campaignId,
                MaxHealth = 5,
                CurrentHealth = 5,
                MaxWillpower = 5,
                CurrentWillpower = 5,
                MaxVitae = 10,
                CurrentVitae = 3,
                BloodPotency = 2,
                PredatorType = predatorType,
            });
        await ctx.SaveChangesAsync();
    }

    private static HuntingService CreateSut(
        ApplicationDbContext ctx,
        ITraitResolver traitResolver,
        IDiceService dice,
        IVitaeService? vitae = null)
    {
        var session = new Mock<ISessionService>();
        session.Setup(
                s => s.PublishDiceRollAsync(
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<int?>(),
                    It.IsAny<string>(),
                    It.IsAny<RollResult>()))
            .Returns(Task.CompletedTask);

        return new HuntingService(
            ctx,
            CreateAuthMock().Object,
            traitResolver,
            dice,
            vitae ?? new VitaeService(
                ctx,
                CreateAuthMock().Object,
                new Mock<IDomainEventDispatcher>().Object,
                new Mock<ILogger<VitaeService>>().Object),
            session.Object,
            new Mock<ILogger<HuntingService>>().Object);
    }

    private static async Task SeedAlleycatDefinitionAsync(ApplicationDbContext ctx)
    {
        ctx.HuntingPoolDefinitions.Add(
            new HuntingPoolDefinition
            {
                Id = 1,
                PredatorType = PredatorType.Alleycat,
                PoolDefinitionJson = """{"traits":[{"type":"Attribute","attributeId":"Strength"},{"type":"Skill","skillId":"Brawl"}]}""",
                BaseVitaeGain = 0,
                PerSuccessVitaeGain = 1,
                NarrativeDescription = "Test narrative.",
            });
        await ctx.SaveChangesAsync();
    }

    [Fact]
    public async Task ExecuteHuntAsync_Succeeds_GainsVitae_WritesRecord_PublishesDice()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown)
        {
            await SeedUserCampaignCharacterAsync(ctx);
            await SeedAlleycatDefinitionAsync(ctx);

            var trait = new Mock<ITraitResolver>();
            trait.Setup(t => t.ResolvePoolAsync(It.IsAny<Character>(), It.IsAny<PoolDefinition>())).ReturnsAsync(4);

            var dice = new Mock<IDiceService>();
            dice.Setup(d => d.Roll(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int?>()))
                .Returns(new RollResult { Successes = 2, DiceRolled = [] });

            HuntingService sut = CreateSut(ctx, trait.Object, dice.Object);

            Result<HuntResult> result = await sut.ExecuteHuntAsync(1, "u1");

            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value!.Successes);
            Assert.Equal(2, result.Value.VitaeGained);
            Assert.Equal(ResonanceOutcome.Fleeting, result.Value.Resonance);
            Assert.Contains("Alleycat", result.Value.PoolDescription);
            Assert.Contains("pool 4 dice", result.Value.PoolDescription);
            Assert.True(await ctx.HuntingRecords.AnyAsync(r => r.CharacterId == 1 && r.Successes == 2 && r.VitaeGained == 2));
            Assert.Equal(
                5,
                await ctx.Characters.AsNoTracking().Where(c => c.Id == 1).Select(c => c.CurrentVitae).FirstAsync());
        }
    }

    [Fact]
    public async Task ExecuteHuntAsync_PredatorTypeNull_ReturnsFailure()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown)
        {
            await SeedUserCampaignCharacterAsync(ctx, predatorType: null);
            await SeedAlleycatDefinitionAsync(ctx);

            HuntingService sut = CreateSut(ctx, Mock.Of<ITraitResolver>(), Mock.Of<IDiceService>());
            Result<HuntResult> result = await sut.ExecuteHuntAsync(1, "u1");

            Assert.False(result.IsSuccess);
            Assert.Equal("Predator Type not set.", result.Error);
        }
    }

    [Fact]
    public async Task ExecuteHuntAsync_NoPoolDefinition_ReturnsFailure()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown)
        {
            await SeedUserCampaignCharacterAsync(ctx);

            HuntingService sut = CreateSut(ctx, Mock.Of<ITraitResolver>(), Mock.Of<IDiceService>());
            Result<HuntResult> result = await sut.ExecuteHuntAsync(1, "u1");

            Assert.False(result.IsSuccess);
            Assert.Equal("Hunting pool definition not found.", result.Error);
        }
    }

    [Fact]
    public async Task ExecuteHuntAsync_TerritoryBonus_IncreasesPool()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown)
        {
            await SeedUserCampaignCharacterAsync(ctx);
            await SeedAlleycatDefinitionAsync(ctx);
            ctx.FeedingTerritories.Add(
                new FeedingTerritory { Id = 10, CampaignId = 1, Name = "Dock", Rating = 3 });
            await ctx.SaveChangesAsync();

            var trait = new Mock<ITraitResolver>();
            trait.Setup(t => t.ResolvePoolAsync(It.IsAny<Character>(), It.IsAny<PoolDefinition>())).ReturnsAsync(2);

            int capturedPool = -1;
            var dice = new Mock<IDiceService>();
            dice.Setup(d => d.Roll(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int?>()))
                .Returns((int pool, bool _, bool _, bool _, bool _, int? _) =>
                {
                    capturedPool = pool;
                    return new RollResult { Successes = 1, DiceRolled = [] };
                });

            HuntingService sut = CreateSut(ctx, trait.Object, dice.Object);
            Result<HuntResult> result = await sut.ExecuteHuntAsync(1, "u1", territoryId: 10);

            Assert.True(result.IsSuccess);
            Assert.Equal(5, capturedPool);
            Assert.Contains("+3 territory bonus", result.Value!.PoolDescription);
            Assert.True(result.Value.TerritoryBonusApplied);
            Assert.Equal(1, result.Value.VitaeGained);
            Assert.Equal(
                4,
                await ctx.Characters.AsNoTracking().Where(c => c.Id == 1).Select(c => c.CurrentVitae).FirstAsync());
        }
    }

    [Fact]
    public async Task ExecuteHuntAsync_TerritoryWrongCampaign_ReturnsFailure()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown)
        {
            await SeedUserCampaignCharacterAsync(ctx, campaignId: 1);
            ctx.Campaigns.Add(new Campaign { Id = 2, Name = "Other", StoryTellerId = "u1" });
            await ctx.SaveChangesAsync();
            await SeedAlleycatDefinitionAsync(ctx);
            ctx.FeedingTerritories.Add(new FeedingTerritory { Id = 20, CampaignId = 2, Name = "Elsewhere", Rating = 2 });
            await ctx.SaveChangesAsync();

            HuntingService sut = CreateSut(ctx, Mock.Of<ITraitResolver>(), Mock.Of<IDiceService>());
            Result<HuntResult> result = await sut.ExecuteHuntAsync(1, "u1", territoryId: 20);

            Assert.False(result.IsSuccess);
            Assert.Equal("Territory does not belong to this campaign.", result.Error);
        }
    }

    [Fact]
    public async Task ExecuteHuntAsync_NoCampaign_WithTerritory_ReturnsFailure()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown)
        {
            await SeedUserCampaignCharacterAsync(ctx, campaignId: null);
            await SeedAlleycatDefinitionAsync(ctx);

            HuntingService sut = CreateSut(ctx, Mock.Of<ITraitResolver>(), Mock.Of<IDiceService>());
            // Fails before territory lookup: character must belong to a campaign to use a territory.
            Result<HuntResult> result = await sut.ExecuteHuntAsync(1, "u1", territoryId: 30);

            Assert.False(result.IsSuccess);
            Assert.Equal("Territory does not belong to this campaign.", result.Error);
        }
    }

    [Fact]
    public async Task ExecuteHuntAsync_ZeroSuccesses_DoesNotCallGainVitae_WritesRecord()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown)
        {
            await SeedUserCampaignCharacterAsync(ctx);
            await SeedAlleycatDefinitionAsync(ctx);

            var trait = new Mock<ITraitResolver>();
            trait.Setup(t => t.ResolvePoolAsync(It.IsAny<Character>(), It.IsAny<PoolDefinition>())).ReturnsAsync(1);

            var dice = new Mock<IDiceService>();
            dice.Setup(d => d.Roll(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int?>()))
                .Returns(new RollResult { Successes = 0, DiceRolled = [] });

            var vitae = new Mock<IVitaeService>();

            HuntingService sut = CreateSut(ctx, trait.Object, dice.Object, vitae.Object);
            Result<HuntResult> result = await sut.ExecuteHuntAsync(1, "u1");

            Assert.True(result.IsSuccess);
            Assert.Equal(0, result.Value!.VitaeGained);
            Assert.Equal(ResonanceOutcome.None, result.Value.Resonance);
            vitae.Verify(
                v => v.GainVitaeAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
            Assert.True(await ctx.HuntingRecords.AnyAsync(r => r.VitaeGained == 0 && r.Successes == 0));
        }
    }

    [Fact]
    public async Task ExecuteHuntAsync_ResolverZero_ClampsToOneDie()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown)
        {
            await SeedUserCampaignCharacterAsync(ctx);
            await SeedAlleycatDefinitionAsync(ctx);

            var trait = new Mock<ITraitResolver>();
            trait.Setup(t => t.ResolvePoolAsync(It.IsAny<Character>(), It.IsAny<PoolDefinition>())).ReturnsAsync(0);

            int capturedPool = -1;
            var dice = new Mock<IDiceService>();
            dice.Setup(d => d.Roll(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int?>()))
                .Returns((int pool, bool _, bool _, bool _, bool _, int? _) =>
                {
                    capturedPool = pool;
                    return new RollResult { Successes = 0, DiceRolled = [] };
                });

            HuntingService sut = CreateSut(ctx, trait.Object, dice.Object);
            Result<HuntResult> result = await sut.ExecuteHuntAsync(1, "u1");

            Assert.True(result.IsSuccess);
            Assert.Equal(1, capturedPool);
        }
    }

    [Fact]
    public async Task ExecuteHuntAsync_Unauthorized_Throws()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown)
        {
            await SeedUserCampaignCharacterAsync(ctx);
            await SeedAlleycatDefinitionAsync(ctx);

            var auth = new Mock<IAuthorizationHelper>();
            auth.Setup(a => a.RequireCharacterAccessAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new UnauthorizedAccessException("no"));

            var session = new Mock<ISessionService>();
            var sut = new HuntingService(
                ctx,
                auth.Object,
                Mock.Of<ITraitResolver>(),
                Mock.Of<IDiceService>(),
                Mock.Of<IVitaeService>(),
                session.Object,
                Mock.Of<ILogger<HuntingService>>());

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.ExecuteHuntAsync(1, "other"));
        }
    }

}
