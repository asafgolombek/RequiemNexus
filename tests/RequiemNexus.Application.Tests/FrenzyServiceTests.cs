using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using RequiemNexus.Application.Contracts;
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

public class FrenzyServiceTests
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

    private static async Task SeedCampaignAndCharacterAsync(ApplicationDbContext ctx)
    {
        ctx.Users.Add(new ApplicationUser
        {
            Id = "u1",
            UserName = "u1",
            NormalizedUserName = "U1",
            Email = "u1@test",
            NormalizedEmail = "U1@TEST",
            EmailConfirmed = true,
        });
        ctx.Campaigns.Add(new Campaign
        {
            Id = 1,
            Name = "Chronicle",
            StoryTellerId = "u1",
        });
        ctx.Characters.Add(new Character
        {
            Id = 1,
            ApplicationUserId = "u1",
            Name = "Kindred",
            CampaignId = 1,
            MaxHealth = 5,
            CurrentHealth = 5,
            MaxWillpower = 5,
            CurrentWillpower = 5,
            MaxVitae = 10,
            CurrentVitae = 5,
            BloodPotency = 2,
        });
        await ctx.SaveChangesAsync();
    }

    private static FrenzyService CreateSut(
        ApplicationDbContext ctx,
        ITraitResolver traitResolver,
        IDiceService dice,
        IWillpowerService? willpower = null)
    {
        var session = new Mock<ISessionService>();
        session.Setup(s => s.PublishDiceRollAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int?>(),
                It.IsAny<string>(),
                It.IsAny<RollResult>()))
            .Returns(Task.CompletedTask);
        session.Setup(s => s.BroadcastCharacterUpdateAsync(It.IsAny<int>())).Returns(Task.CompletedTask);

        return new FrenzyService(
            ctx,
            CreateAuthMock().Object,
            traitResolver,
            dice,
            willpower ?? new WillpowerService(ctx, CreateAuthMock().Object, new Mock<ILogger<WillpowerService>>().Object),
            session.Object,
            new Mock<ILogger<FrenzyService>>().Object);
    }

    [Fact]
    public async Task RollFrenzySaveAsync_WithSuccesses_IsSaved_NoTilt()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown)
        {
            await SeedCampaignAndCharacterAsync(ctx);

            var trait = new Mock<ITraitResolver>();
            trait.Setup(t => t.ResolvePoolAsync(It.IsAny<Character>(), It.IsAny<PoolDefinition>()))
                .ReturnsAsync(3);

            var dice = new Mock<IDiceService>();
            dice.Setup(d => d.Roll(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int?>()))
                .Returns(new RollResult { Successes = 2, DiceRolled = [] });

            FrenzyService sut = CreateSut(ctx, trait.Object, dice.Object);
            Result<FrenzySaveResult> result = await sut.RollFrenzySaveAsync(1, "u1", FrenzyTrigger.Rage, false);

            Assert.True(result.IsSuccess);
            Assert.True(result.Value!.Saved);
            Assert.Null(result.Value.TiltApplied);
            Assert.False(result.Value.SuppressedDueToBeastAlreadyActive);
        }
    }

    [Fact]
    public async Task RollFrenzySaveAsync_RageFailure_AppliesFrenzyTilt()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown)
        {
            await SeedCampaignAndCharacterAsync(ctx);

            var trait = new Mock<ITraitResolver>();
            trait.Setup(t => t.ResolvePoolAsync(It.IsAny<Character>(), It.IsAny<PoolDefinition>()))
                .ReturnsAsync(2);

            var dice = new Mock<IDiceService>();
            dice.Setup(d => d.Roll(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int?>()))
                .Returns(new RollResult { Successes = 0, DiceRolled = [] });

            FrenzyService sut = CreateSut(ctx, trait.Object, dice.Object);
            Result<FrenzySaveResult> result = await sut.RollFrenzySaveAsync(1, "u1", FrenzyTrigger.Rage, false);

            Assert.True(result.IsSuccess);
            Assert.False(result.Value!.Saved);
            Assert.Equal(TiltType.Frenzy, result.Value.TiltApplied);
            Assert.True(await ctx.CharacterTilts.AnyAsync(t => t.CharacterId == 1 && t.IsActive && t.TiltType == TiltType.Frenzy));
        }
    }

    [Fact]
    public async Task RollFrenzySaveAsync_RotschreckFailure_AppliesRotschreckTilt()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown)
        {
            await SeedCampaignAndCharacterAsync(ctx);

            var trait = new Mock<ITraitResolver>();
            trait.Setup(t => t.ResolvePoolAsync(It.IsAny<Character>(), It.IsAny<PoolDefinition>()))
                .ReturnsAsync(2);

            var dice = new Mock<IDiceService>();
            dice.Setup(d => d.Roll(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int?>()))
                .Returns(new RollResult { Successes = 0, DiceRolled = [] });

            FrenzyService sut = CreateSut(ctx, trait.Object, dice.Object);
            Result<FrenzySaveResult> result = await sut.RollFrenzySaveAsync(1, "u1", FrenzyTrigger.Rotschreck, false);

            Assert.True(result.IsSuccess);
            Assert.Equal(TiltType.Rotschreck, result.Value!.TiltApplied);
        }
    }

    [Fact]
    public async Task RollFrenzySaveAsync_SpendWillpower_ReducesDicePool()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown)
        {
            await SeedCampaignAndCharacterAsync(ctx);

            var trait = new Mock<ITraitResolver>();
            trait.Setup(t => t.ResolvePoolAsync(It.IsAny<Character>(), It.IsAny<PoolDefinition>()))
                .ReturnsAsync(4);

            int capturedPool = -1;
            var dice = new Mock<IDiceService>();
            dice.Setup(d => d.Roll(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int?>()))
                .Returns((int pool, bool _, bool _, bool _, bool _, int? _) =>
                {
                    capturedPool = pool;
                    return new RollResult { Successes = 1, DiceRolled = [] };
                });

            var willpower = new WillpowerService(ctx, CreateAuthMock().Object, new Mock<ILogger<WillpowerService>>().Object);
            FrenzyService sut = CreateSut(ctx, trait.Object, dice.Object, willpower);

            Result<FrenzySaveResult> result = await sut.RollFrenzySaveAsync(1, "u1", FrenzyTrigger.Rage, spendWillpower: true);

            Assert.True(result.IsSuccess);
            Assert.True(result.Value!.WillpowerSpent);
            Assert.Equal(5, capturedPool);
            Character reloaded = await ctx.Characters.AsNoTracking().FirstAsync(c => c.Id == 1);
            Assert.Equal(4, reloaded.CurrentWillpower);
        }
    }

    [Fact]
    public async Task RollFrenzySaveAsync_PoolZeroAfterWillpower_UsesChanceDie()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown)
        {
            await SeedCampaignAndCharacterAsync(ctx);
            Character kin = await ctx.Characters.FirstAsync(c => c.Id == 1);
            kin.BloodPotency = 1;
            await ctx.SaveChangesAsync();

            var trait = new Mock<ITraitResolver>();
            trait.Setup(t => t.ResolvePoolAsync(It.IsAny<Character>(), It.IsAny<PoolDefinition>()))
                .ReturnsAsync(0);

            int capturedPool = -1;
            var dice = new Mock<IDiceService>();
            dice.Setup(d => d.Roll(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int?>()))
                .Returns((int pool, bool _, bool _, bool _, bool _, int? _) =>
                {
                    capturedPool = pool;
                    return new RollResult { Successes = 1, DiceRolled = [] };
                });

            var willpower = new WillpowerService(ctx, CreateAuthMock().Object, new Mock<ILogger<WillpowerService>>().Object);
            FrenzyService sut = CreateSut(ctx, trait.Object, dice.Object, willpower);

            Result<FrenzySaveResult> result = await sut.RollFrenzySaveAsync(1, "u1", FrenzyTrigger.Rage, spendWillpower: true);

            Assert.True(result.IsSuccess);
            Assert.Equal(1, capturedPool);
        }
    }

    [Fact]
    public async Task RollFrenzySaveAsync_BeastTiltActive_IsSuppressed()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown)
        {
            await SeedCampaignAndCharacterAsync(ctx);
            ctx.CharacterTilts.Add(new CharacterTilt
            {
                CharacterId = 1,
                TiltType = TiltType.Frenzy,
                IsActive = true,
                AppliedAt = DateTime.UtcNow,
            });
            await ctx.SaveChangesAsync();

            var trait = new Mock<ITraitResolver>();
            var dice = new Mock<IDiceService>();

            FrenzyService sut = CreateSut(ctx, trait.Object, dice.Object);
            Result<FrenzySaveResult> result = await sut.RollFrenzySaveAsync(1, "u1", FrenzyTrigger.Rotschreck, false);

            Assert.True(result.IsSuccess);
            Assert.True(result.Value!.SuppressedDueToBeastAlreadyActive);
            dice.Verify(
                d => d.Roll(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int?>()),
                Times.Never);
        }
    }
}
