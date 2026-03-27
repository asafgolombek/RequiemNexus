using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.Events;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Application.Services;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Models;
using Xunit;

namespace RequiemNexus.Application.Tests;

public class TorporServiceTests
{
    private static Mock<IAuthorizationHelper> CreateStAuthMock()
    {
        var auth = new Mock<IAuthorizationHelper>();
        auth.Setup(a => a.RequireStorytellerAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
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

    private static async Task SeedAsync(ApplicationDbContext ctx)
    {
        ctx.Users.Add(new ApplicationUser
        {
            Id = "st1",
            UserName = "st1",
            NormalizedUserName = "ST1",
            Email = "st1@test",
            NormalizedEmail = "ST1@TEST",
            EmailConfirmed = true,
        });
        ctx.Campaigns.Add(new Campaign { Id = 1, Name = "C", StoryTellerId = "st1" });
        ctx.Characters.Add(new Character
        {
            Id = 1,
            ApplicationUserId = "st1",
            Name = "TorporTest",
            CampaignId = 1,
            MaxHealth = 5,
            CurrentHealth = 5,
            MaxWillpower = 5,
            CurrentWillpower = 5,
            MaxVitae = 10,
            CurrentVitae = 5,
            BloodPotency = 1,
        });
        await ctx.SaveChangesAsync();
    }

    private static TorporService CreateSut(ApplicationDbContext ctx, IAuthorizationHelper auth)
    {
        var dispatcher = new Mock<IDomainEventDispatcher>();
        var vitae = new VitaeService(
            ctx,
            auth,
            dispatcher.Object,
            new Mock<ILogger<VitaeService>>().Object);
        var session = new Mock<ISessionService>();
        session.Setup(s => s.BroadcastCharacterUpdateAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
        return new TorporService(
            ctx,
            auth,
            vitae,
            session.Object,
            new Mock<ILogger<TorporService>>().Object);
    }

    [Fact]
    public async Task EnterTorporAsync_SetsTorporSince_AndResolvesFrenzyTilt()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown)
        {
            await SeedAsync(ctx);
            ctx.CharacterTilts.Add(new CharacterTilt
            {
                CharacterId = 1,
                TiltType = TiltType.Frenzy,
                IsActive = true,
                AppliedAt = DateTime.UtcNow,
            });
            await ctx.SaveChangesAsync();

            TorporService sut = CreateSut(ctx, CreateStAuthMock().Object);
            Result<Unit> result = await sut.EnterTorporAsync(1, "st1");

            Assert.True(result.IsSuccess);
            Character c = await ctx.Characters.AsNoTracking().FirstAsync(x => x.Id == 1);
            Assert.True(c.TorporSince.HasValue);
            CharacterTilt tilt = await ctx.CharacterTilts.SingleAsync(t => t.CharacterId == 1);
            Assert.False(tilt.IsActive);
        }
    }

    [Fact]
    public async Task AwakenFromTorporAsync_NarrativeFalse_DeductsVitae_AndClearsTorpor()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown)
        {
            await SeedAsync(ctx);
            Character c = await ctx.Characters.FirstAsync(x => x.Id == 1);
            c.TorporSince = DateTime.UtcNow.AddDays(-1);
            c.CurrentVitae = 3;
            await ctx.SaveChangesAsync();

            var auth = CreateStAuthMock();
            auth.Setup(a => a.RequireCharacterAccessAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            TorporService sut = CreateSut(ctx, auth.Object);
            Result<Unit> result = await sut.AwakenFromTorporAsync(1, "st1", narrativeAwakening: false);

            Assert.True(result.IsSuccess);
            Character reloaded = await ctx.Characters.AsNoTracking().FirstAsync(x => x.Id == 1);
            Assert.Null(reloaded.TorporSince);
            Assert.Null(reloaded.LastStarvationNotifiedAt);
            Assert.Equal(2, reloaded.CurrentVitae);
        }
    }

    [Fact]
    public async Task AwakenFromTorporAsync_NarrativeFalse_NoVitae_ReturnsFailure()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown)
        {
            await SeedAsync(ctx);
            Character c = await ctx.Characters.FirstAsync(x => x.Id == 1);
            c.TorporSince = DateTime.UtcNow;
            c.CurrentVitae = 0;
            await ctx.SaveChangesAsync();

            TorporService sut = CreateSut(ctx, CreateStAuthMock().Object);
            Result<Unit> result = await sut.AwakenFromTorporAsync(1, "st1", narrativeAwakening: false);

            Assert.False(result.IsSuccess);
        }
    }

    [Fact]
    public async Task AwakenFromTorporAsync_NarrativeTrue_NoVitaeCost()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown)
        {
            await SeedAsync(ctx);
            Character c = await ctx.Characters.FirstAsync(x => x.Id == 1);
            c.TorporSince = DateTime.UtcNow;
            c.CurrentVitae = 0;
            await ctx.SaveChangesAsync();

            TorporService sut = CreateSut(ctx, CreateStAuthMock().Object);
            Result<Unit> result = await sut.AwakenFromTorporAsync(1, "st1", narrativeAwakening: true);

            Assert.True(result.IsSuccess);
            Character reloaded = await ctx.Characters.AsNoTracking().FirstAsync(x => x.Id == 1);
            Assert.Null(reloaded.TorporSince);
            Assert.Equal(0, reloaded.CurrentVitae);
        }
    }

    [Fact]
    public async Task CheckStarvationIntervalAsync_WhenDue_SetsLastStarvationNotifiedAt()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown)
        {
            await SeedAsync(ctx);
            Character c = await ctx.Characters.FirstAsync(x => x.Id == 1);
            c.TorporSince = DateTime.UtcNow.AddDays(-2);
            c.LastStarvationNotifiedAt = null;
            await ctx.SaveChangesAsync();

            TorporService sut = CreateSut(ctx, CreateStAuthMock().Object);
            await sut.CheckStarvationIntervalAsync(1);

            Character reloaded = await ctx.Characters.AsNoTracking().FirstAsync(x => x.Id == 1);
            Assert.NotNull(reloaded.LastStarvationNotifiedAt);
        }
    }

    [Fact]
    public async Task CheckStarvationIntervalAsync_WhenAlreadyNotifiedWithinInterval_DoesNotUpdate()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown)
        {
            await SeedAsync(ctx);
            DateTime notified = DateTime.UtcNow.AddHours(-1);
            Character c = await ctx.Characters.FirstAsync(x => x.Id == 1);
            c.TorporSince = DateTime.UtcNow.AddDays(-5);
            c.LastStarvationNotifiedAt = notified;
            await ctx.SaveChangesAsync();

            TorporService sut = CreateSut(ctx, CreateStAuthMock().Object);
            await sut.CheckStarvationIntervalAsync(1);

            Character reloaded = await ctx.Characters.AsNoTracking().FirstAsync(x => x.Id == 1);
            Assert.Equal(notified, reloaded.LastStarvationNotifiedAt);
        }
    }

    [Fact]
    public async Task CheckStarvationIntervalAsync_BloodPotencyTen_DoesNotNotify()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown)
        {
            await SeedAsync(ctx);
            Character c = await ctx.Characters.FirstAsync(x => x.Id == 1);
            c.BloodPotency = 10;
            c.TorporSince = DateTime.UtcNow.AddYears(-1000);
            c.LastStarvationNotifiedAt = null;
            await ctx.SaveChangesAsync();

            TorporService sut = CreateSut(ctx, CreateStAuthMock().Object);
            await sut.CheckStarvationIntervalAsync(1);

            Character reloaded = await ctx.Characters.AsNoTracking().FirstAsync(x => x.Id == 1);
            Assert.Null(reloaded.LastStarvationNotifiedAt);
        }
    }
}
