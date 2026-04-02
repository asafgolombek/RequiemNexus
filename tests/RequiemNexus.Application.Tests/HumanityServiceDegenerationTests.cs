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
using RequiemNexus.Data.Models.Enums;
using RequiemNexus.Domain.Contracts;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Events;
using RequiemNexus.Domain.Models;
using Xunit;

namespace RequiemNexus.Application.Tests;

public class HumanityServiceDegenerationTests
{
    private static HumanityService CreateSut(
        ApplicationDbContext ctx,
        IDiceService dice,
        Mock<IConditionService>? conditionMock = null,
        Mock<ISessionService>? sessionMock = null)
    {
        var auth = new Mock<IAuthorizationHelper>();
        auth.Setup(a => a.RequireCharacterAccessAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var dispatcher = new Mock<IDomainEventDispatcher>();
        var cond = conditionMock ?? new Mock<IConditionService>();
        cond.Setup(c => c.ApplyConditionAsync(
                It.IsAny<int>(),
                It.IsAny<ConditionType>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string>()))
            .ReturnsAsync(new CharacterCondition { Id = 1, CharacterId = 1, ConditionType = ConditionType.Guilty });

        var session = sessionMock ?? new Mock<ISessionService>();
        session.Setup(s => s.PublishDiceRollAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int?>(),
                It.IsAny<string>(),
                It.IsAny<RollResult>()))
            .Returns(Task.CompletedTask);
        session.Setup(s => s.BroadcastCharacterUpdateAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
        session.Setup(s => s.BroadcastChronicleUpdateAsync(It.IsAny<RequiemNexus.Data.RealTime.ChronicleUpdateDto>()))
            .Returns(Task.CompletedTask);

        return new HumanityService(
            ctx,
            auth.Object,
            dispatcher.Object,
            dice,
            session.Object,
            cond.Object,
            ReferenceDataCacheTestDoubles.EmptyButInitialized(),
            Mock.Of<ILogger<HumanityService>>());
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

    private static async Task SeedCharacterAsync(ApplicationDbContext ctx, int humanity = 7, int stains = 4, int resolve = 3)
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
                Name = "K",
                CampaignId = 1,
                Humanity = humanity,
                HumanityStains = stains,
                MaxWillpower = 5,
                CurrentWillpower = 5,
                MaxVitae = 10,
                CurrentVitae = 5,
                BloodPotency = 1,
            });
        ctx.CharacterAttributes.Add(new CharacterAttribute
        {
            CharacterId = 1,
            Name = nameof(AttributeId.Resolve),
            Rating = resolve,
            Category = TraitCategory.Mental,
        });
        await ctx.SaveChangesAsync();
    }

    [Fact]
    public async Task ExecuteDegenerationRollAsync_Success_ClearsStains_KeepsHumanity()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown)
        {
            await SeedCharacterAsync(ctx);
            var dice = new Mock<IDiceService>();
            dice.Setup(d => d.Roll(It.IsAny<int>(), true, false, false, false, null))
                .Returns(new RollResult { Successes = 2, DiceRolled = [8, 9] });

            var sut = CreateSut(ctx, dice.Object);
            Result<DegenerationRollOutcome> result = await sut.ExecuteDegenerationRollAsync(1, "u1");

            Assert.True(result.IsSuccess);
            Assert.True(result.Value!.HumanityUnchanged);
            Assert.Equal(7, result.Value.NewHumanity);
            Assert.False(result.Value.GuiltyConditionApplied);
            Character? reloaded = await ctx.Characters.AsNoTracking().FirstAsync(c => c.Id == 1);
            Assert.Equal(0, reloaded.HumanityStains);
            Assert.Equal(7, reloaded.Humanity);
        }
    }

    [Fact]
    public async Task ExecuteDegenerationRollAsync_Failure_ReducesHumanity_ClearsStains()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown)
        {
            await SeedCharacterAsync(ctx);
            var dice = new Mock<IDiceService>();
            dice.Setup(d => d.Roll(It.IsAny<int>(), true, false, false, false, null))
                .Returns(new RollResult { Successes = 0, DiceRolled = [2, 3], IsDramaticFailure = false });

            var sut = CreateSut(ctx, dice.Object);
            Result<DegenerationRollOutcome> result = await sut.ExecuteDegenerationRollAsync(1, "u1");

            Assert.True(result.IsSuccess);
            Assert.False(result.Value!.HumanityUnchanged);
            Assert.Equal(6, result.Value.NewHumanity);
            Assert.False(result.Value.GuiltyConditionApplied);
            Character? reloaded = await ctx.Characters.AsNoTracking().FirstAsync(c => c.Id == 1);
            Assert.Equal(0, reloaded.HumanityStains);
            Assert.Equal(6, reloaded.Humanity);
        }
    }

    [Fact]
    public async Task ExecuteDegenerationRollAsync_DramaticFailure_AppliesGuilty()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown)
        {
            await SeedCharacterAsync(ctx);
            var dice = new Mock<IDiceService>();
            dice.Setup(d => d.Roll(It.IsAny<int>(), true, false, false, false, null))
                .Returns(new RollResult { Successes = 0, DiceRolled = [1], IsDramaticFailure = true });

            var conditionMock = new Mock<IConditionService>();
            conditionMock.Setup(c => c.ApplyConditionAsync(
                    1,
                    ConditionType.Guilty,
                    null,
                    null,
                    "u1"))
                .ReturnsAsync(new CharacterCondition { Id = 2, CharacterId = 1, ConditionType = ConditionType.Guilty });

            var sut = CreateSut(ctx, dice.Object, conditionMock);
            Result<DegenerationRollOutcome> result = await sut.ExecuteDegenerationRollAsync(1, "u1");

            Assert.True(result.IsSuccess);
            Assert.True(result.Value!.GuiltyConditionApplied);
            conditionMock.Verify(
                c => c.ApplyConditionAsync(1, ConditionType.Guilty, null, null, "u1"),
                Times.Once);
        }
    }

    [Fact]
    public async Task ExecuteDegenerationRollAsync_Humanity0_PassesChanceDiePoolToDice()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown)
        {
            await SeedCharacterAsync(ctx, humanity: 0, stains: 0, resolve: 4);
            var dice = new Mock<IDiceService>();
            dice.Setup(d => d.Roll(0, true, false, false, false, null))
                .Returns(new RollResult { Successes = 1, DiceRolled = [10] });

            var sut = CreateSut(ctx, dice.Object);
            await sut.ExecuteDegenerationRollAsync(1, "u1");

            dice.Verify(d => d.Roll(0, true, false, false, false, It.IsAny<int?>()), Times.Once);
        }
    }
}
