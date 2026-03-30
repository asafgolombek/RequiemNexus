using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.Models;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Application.Services;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.Models.Enums;
using RequiemNexus.Domain.Contracts;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Models;
using Xunit;

namespace RequiemNexus.Application.Tests;

public class TouchstoneServiceRemorseTests
{
    private static TouchstoneService CreateSut(
        ApplicationDbContext ctx,
        IDiceService dice,
        Mock<IHumanityService>? humanityMock = null,
        Mock<IConditionService>? conditionMock = null)
    {
        var auth = new Mock<IAuthorizationHelper>();
        auth.Setup(a => a.RequireCharacterAccessAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var humanity = humanityMock ?? new Mock<IHumanityService>();
        humanity.Setup(h => h.EvaluateStainsAsync(It.IsAny<int>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var cond = conditionMock ?? new Mock<IConditionService>();
        cond.Setup(c => c.ApplyConditionAsync(
                It.IsAny<int>(),
                It.IsAny<ConditionType>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string>()))
            .ReturnsAsync(new CharacterCondition { Id = 1, CharacterId = 1, ConditionType = ConditionType.Guilty });

        var session = new Mock<ISessionService>();
        session.Setup(s => s.PublishDiceRollAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int?>(),
                It.IsAny<string>(),
                It.IsAny<RollResult>()))
            .Returns(Task.CompletedTask);
        session.Setup(s => s.BroadcastCharacterUpdateAsync(It.IsAny<int>())).Returns(Task.CompletedTask);

        return new TouchstoneService(
            ctx,
            auth.Object,
            dice,
            session.Object,
            cond.Object,
            humanity.Object,
            Mock.Of<ILogger<TouchstoneService>>());
    }

    private static async Task<(ApplicationDbContext Ctx, IAsyncDisposable Teardown)> CreateSqliteContextAsync()
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        var ctx = new ApplicationDbContext(options);
        await ctx.Database.EnsureCreatedAsync();
        IAsyncDisposable teardown = new SqliteTeardown(connection, ctx);
        return (ctx, teardown);
    }

    private sealed class SqliteTeardown(Microsoft.Data.Sqlite.SqliteConnection connection, ApplicationDbContext context) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }

    private static async Task SeedCharacterAsync(
        ApplicationDbContext ctx,
        int humanity = 7,
        int stains = 3,
        string touchstone = "",
        bool addTouchstoneMerit = false)
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
        ctx.Campaigns.Add(new Campaign { Id = 1, Name = "C", StoryTellerId = "u1" });
        ctx.Characters.Add(
            new Character
            {
                Id = 1,
                ApplicationUserId = "u1",
                Name = "Kindred",
                CampaignId = 1,
                Humanity = humanity,
                HumanityStains = stains,
                Touchstone = touchstone,
                MaxHealth = 7,
                CurrentHealth = 7,
                MaxWillpower = 4,
                CurrentWillpower = 4,
                MaxVitae = 10,
                CurrentVitae = 10,
            });
        ctx.CharacterAttributes.Add(
            new CharacterAttribute { CharacterId = 1, Name = nameof(AttributeId.Resolve), Rating = 3, Category = TraitCategory.Social });

        if (addTouchstoneMerit)
        {
            ctx.Merits.Add(new Merit { Id = 99, Name = "Touchstone", Description = "x", ValidRatings = "•" });
            ctx.CharacterMerits.Add(new CharacterMerit { CharacterId = 1, MeritId = 99, Rating = 2 });
        }

        await ctx.SaveChangesAsync();
    }

    [Fact]
    public async Task RollRemorseAsync_NoStains_ReturnsFailure()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown.ConfigureAwait(false))
        {
            await SeedCharacterAsync(ctx, stains: 0);
            var dice = new Mock<IDiceService>();
            TouchstoneService sut = CreateSut(ctx, dice.Object);

            Result<DegenerationRollOutcome> result = await sut.RollRemorseAsync(1, "u1");

            Assert.False(result.IsSuccess);
            Assert.Equal("No stains to roll remorse for", result.Error);
            dice.Verify(
                d => d.Roll(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int?>()),
                Times.Never);
        }
    }

    [Fact]
    public async Task RollRemorseAsync_StainsAtThreshold_ReturnsFailure()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown.ConfigureAwait(false))
        {
            await SeedCharacterAsync(ctx, humanity: 7, stains: 7);
            var dice = new Mock<IDiceService>();
            TouchstoneService sut = CreateSut(ctx, dice.Object);

            Result<DegenerationRollOutcome> result = await sut.RollRemorseAsync(1, "u1");

            Assert.False(result.IsSuccess);
            Assert.Equal("Use degeneration roll, not remorse", result.Error);
        }
    }

    [Fact]
    public async Task RollRemorseAsync_WithTouchstoneText_PassesHumanityPlusOnePool()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown.ConfigureAwait(false))
        {
            await SeedCharacterAsync(ctx, humanity: 5, stains: 2, touchstone: "Sister");
            var dice = new Mock<IDiceService>();
            dice.Setup(d => d.Roll(6, true, false, false, false, null))
                .Returns(new RollResult { Successes = 1, DiceRolled = [8] });
            TouchstoneService sut = CreateSut(ctx, dice.Object);

            Result<DegenerationRollOutcome> result = await sut.RollRemorseAsync(1, "u1");

            Assert.True(result.IsSuccess);
            dice.Verify(d => d.Roll(6, true, false, false, false, null), Times.Once);
        }
    }

    [Fact]
    public async Task RollRemorseAsync_NoTouchstoneAnchor_PassesHumanityPoolOnly()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown.ConfigureAwait(false))
        {
            await SeedCharacterAsync(ctx, humanity: 5, stains: 2, touchstone: "");
            var dice = new Mock<IDiceService>();
            dice.Setup(d => d.Roll(5, true, false, false, false, null))
                .Returns(new RollResult { Successes = 1, DiceRolled = [8] });
            TouchstoneService sut = CreateSut(ctx, dice.Object);

            Result<DegenerationRollOutcome> result = await sut.RollRemorseAsync(1, "u1");

            Assert.True(result.IsSuccess);
            dice.Verify(d => d.Roll(5, true, false, false, false, null), Times.Once);
        }
    }

    [Fact]
    public async Task RollRemorseAsync_TouchstoneMeritOnly_AddsBonusDie()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown.ConfigureAwait(false))
        {
            await SeedCharacterAsync(ctx, humanity: 4, stains: 1, addTouchstoneMerit: true);
            var dice = new Mock<IDiceService>();
            dice.Setup(d => d.Roll(5, true, false, false, false, null))
                .Returns(new RollResult { Successes = 1, DiceRolled = [8] });
            TouchstoneService sut = CreateSut(ctx, dice.Object);

            await sut.RollRemorseAsync(1, "u1");

            dice.Verify(d => d.Roll(5, true, false, false, false, null), Times.Once);
        }
    }
}
