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
using RequiemNexus.Domain.Models;
using Xunit;

namespace RequiemNexus.Application.Tests;

/// <summary>
/// Tests for <see cref="DisciplineActivationService"/> — power activation, pool resolution, and resource deduction.
/// </summary>
public class DisciplineActivationServiceTests
{
    private const string _emptyPoolJson = """{"traits":[]}""";

    private static Mock<IAuthorizationHelper> CreatePermissiveAuthMock()
    {
        var authHelper = new Mock<IAuthorizationHelper>();
        authHelper
            .Setup(a => a.RequireCharacterOwnerAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        authHelper
            .Setup(a => a.RequireCharacterAccessAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        return authHelper;
    }

    private static DisciplineActivationService CreateService(
        ApplicationDbContext ctx,
        ITraitResolver traitResolver,
        Mock<ISessionService>? sessionMock = null,
        IAuthorizationHelper? authHelper = null)
    {
        var auth = authHelper ?? CreatePermissiveAuthMock().Object;
        Mock<ISessionService> session = sessionMock ?? new Mock<ISessionService>();
        session.Setup(s => s.BroadcastCharacterUpdateAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
        var logger = new Mock<ILogger<DisciplineActivationService>>().Object;
        var dispatcher = new Mock<IDomainEventDispatcher>();
        var vitaeService = new VitaeService(
            ctx,
            auth,
            dispatcher.Object,
            new Mock<ILogger<VitaeService>>().Object);
        var willpowerService = new WillpowerService(
            ctx,
            auth,
            new Mock<ILogger<WillpowerService>>().Object);
        return new DisciplineActivationService(
            ctx,
            auth,
            traitResolver,
            vitaeService,
            willpowerService,
            session.Object,
            logger);
    }

    private static async Task<(ApplicationDbContext Context, IAsyncDisposable Teardown)> CreateSqliteContextAsync()
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

    private static ITraitResolver CreateTraitResolverMock(int poolValue)
    {
        var mock = new Mock<ITraitResolver>();
        mock.Setup(t => t.ResolvePool(It.IsAny<Character>(), It.IsAny<PoolDefinition>()))
            .Returns(poolValue);
        mock.Setup(t => t.ResolvePoolAsync(It.IsAny<Character>(), It.IsAny<PoolDefinition>()))
            .ReturnsAsync(poolValue);
        return mock.Object;
    }

    private static async Task SeedAsync(
        ApplicationDbContext ctx,
        string cost,
        string? poolJson,
        int disciplineRating,
        int powerLevel,
        int currentVitae,
        int currentWillpower)
    {
        ctx.Users.Add(new ApplicationUser
        {
            Id = "player1",
            UserName = "player1@test",
            NormalizedUserName = "PLAYER1@TEST",
            Email = "player1@test.com",
            NormalizedEmail = "PLAYER1@TEST.COM",
            EmailConfirmed = true,
        });
        ctx.Disciplines.Add(new Discipline { Id = 1, Name = "Vigor" });
        ctx.DisciplinePowers.Add(new DisciplinePower
        {
            Id = 1,
            DisciplineId = 1,
            Level = powerLevel,
            Name = "Test Power",
            Description = "Test",
            Cost = cost,
            PoolDefinitionJson = poolJson,
        });
        ctx.Characters.Add(new Character
        {
            Id = 1,
            Name = "Kindred",
            ApplicationUserId = "player1",
            CurrentVitae = currentVitae,
            CurrentWillpower = currentWillpower,
            BloodPotency = 1,
            MaxVitae = 10,
            MaxWillpower = 5,
        });
        ctx.CharacterDisciplines.Add(new CharacterDiscipline
        {
            Id = 1,
            CharacterId = 1,
            DisciplineId = 1,
            Rating = disciplineRating,
        });
        await ctx.SaveChangesAsync();
    }

    [Fact]
    public async Task ActivatePowerAsync_VitaeCost_DeductsVitaeAndReturnsPool()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown)
        {
            await SeedAsync(ctx, "1 Vitae", _emptyPoolJson, disciplineRating: 2, powerLevel: 1, currentVitae: 5, currentWillpower: 4);
            var sut = CreateService(ctx, CreateTraitResolverMock(7));
            int dice = await sut.ActivatePowerAsync(1, 1, "player1");
            Assert.Equal(7, dice);
            Character? reloaded = await ctx.Characters.AsNoTracking().FirstAsync(c => c.Id == 1);
            Assert.Equal(4, reloaded.CurrentVitae);
        }
    }

    [Fact]
    public async Task ActivatePowerAsync_WillpowerCost_DeductsWillpowerAndReturnsPool()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown)
        {
            await SeedAsync(ctx, "1 Willpower", _emptyPoolJson, disciplineRating: 2, powerLevel: 1, currentVitae: 5, currentWillpower: 4);
            var sut = CreateService(ctx, CreateTraitResolverMock(6));
            int dice = await sut.ActivatePowerAsync(1, 1, "player1");
            Assert.Equal(6, dice);
            Character? reloaded = await ctx.Characters.AsNoTracking().FirstAsync(c => c.Id == 1);
            Assert.Equal(3, reloaded.CurrentWillpower);
        }
    }

    [Fact]
    public async Task ActivatePowerAsync_NoCost_DoesNotDeductResourcesAndReturnsPool()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown)
        {
            await SeedAsync(ctx, "—", _emptyPoolJson, disciplineRating: 2, powerLevel: 1, currentVitae: 5, currentWillpower: 4);
            var sut = CreateService(ctx, CreateTraitResolverMock(5));
            int dice = await sut.ActivatePowerAsync(1, 1, "player1");
            Assert.Equal(5, dice);
            Character? reloaded = await ctx.Characters.AsNoTracking().FirstAsync(c => c.Id == 1);
            Assert.Equal(5, reloaded.CurrentVitae);
            Assert.Equal(4, reloaded.CurrentWillpower);
        }
    }

    [Fact]
    public async Task ActivatePowerAsync_InsufficientVitae_ThrowsInvalidOperationException()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown)
        {
            await SeedAsync(ctx, "1 Vitae", _emptyPoolJson, disciplineRating: 2, powerLevel: 1, currentVitae: 0, currentWillpower: 4);
            var sut = CreateService(ctx, CreateTraitResolverMock(3));
            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ActivatePowerAsync(1, 1, "player1"));
        }
    }

    [Fact]
    public async Task ActivatePowerAsync_InsufficientWillpower_ThrowsInvalidOperationException()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown)
        {
            await SeedAsync(ctx, "1 Willpower", _emptyPoolJson, disciplineRating: 2, powerLevel: 1, currentVitae: 5, currentWillpower: 0);
            var sut = CreateService(ctx, CreateTraitResolverMock(3));
            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ActivatePowerAsync(1, 1, "player1"));
        }
    }

    [Fact]
    public async Task ActivatePowerAsync_NullPoolDefinitionJson_ThrowsInvalidOperationException()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown)
        {
            await SeedAsync(ctx, "—", null, disciplineRating: 2, powerLevel: 1, currentVitae: 5, currentWillpower: 4);
            var sut = CreateService(ctx, CreateTraitResolverMock(1));
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ActivatePowerAsync(1, 1, "player1"));
            Assert.Contains("rollable pool", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task ActivatePowerAsync_CharacterLacksRequiredRating_ThrowsInvalidOperationException()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown)
        {
            await SeedAsync(ctx, "—", _emptyPoolJson, disciplineRating: 1, powerLevel: 2, currentVitae: 5, currentWillpower: 4);
            var sut = CreateService(ctx, CreateTraitResolverMock(1));
            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ActivatePowerAsync(1, 1, "player1"));
        }
    }

    [Fact]
    public async Task ResolveActivationPoolAsync_NullPool_ReturnsZero()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown)
        {
            await SeedAsync(ctx, "—", null, disciplineRating: 2, powerLevel: 1, currentVitae: 5, currentWillpower: 4);
            var sut = CreateService(ctx, CreateTraitResolverMock(99));
            int preview = await sut.ResolveActivationPoolAsync(1, 1, "player1");
            Assert.Equal(0, preview);
        }
    }

    [Fact]
    public async Task ActivatePowerAsync_UsesResolvePoolAsyncForFinalPool()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown)
        {
            await SeedAsync(ctx, "—", _emptyPoolJson, disciplineRating: 2, powerLevel: 1, currentVitae: 5, currentWillpower: 4);
            var mock = new Mock<ITraitResolver>();
            mock.Setup(t => t.ResolvePool(It.IsAny<Character>(), It.IsAny<PoolDefinition>())).Returns(3);
            mock.Setup(t => t.ResolvePoolAsync(It.IsAny<Character>(), It.IsAny<PoolDefinition>())).ReturnsAsync(11);
            var sut = CreateService(ctx, mock.Object);
            int dice = await sut.ActivatePowerAsync(1, 1, "player1");
            Assert.Equal(11, dice);
        }
    }

    [Fact]
    public async Task ResolveActivationPoolAsync_UsesSyncResolvePoolOnly()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown)
        {
            await SeedAsync(ctx, "1 Vitae", _emptyPoolJson, disciplineRating: 2, powerLevel: 1, currentVitae: 5, currentWillpower: 4);
            var mock = new Mock<ITraitResolver>();
            mock.Setup(t => t.ResolvePool(It.IsAny<Character>(), It.IsAny<PoolDefinition>())).Returns(4);
            mock.Setup(t => t.ResolvePoolAsync(It.IsAny<Character>(), It.IsAny<PoolDefinition>())).ReturnsAsync(999);
            var sut = CreateService(ctx, mock.Object);
            int preview = await sut.ResolveActivationPoolAsync(1, 1, "player1");
            Assert.Equal(4, preview);
            mock.Verify(
                t => t.ResolvePoolAsync(It.IsAny<Character>(), It.IsAny<PoolDefinition>()),
                Times.Never);
        }
    }

    [Fact]
    public async Task ActivatePowerAsync_NonZeroCost_BroadcastsCharacterUpdate()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown)
        {
            await SeedAsync(ctx, "1 Vitae", _emptyPoolJson, disciplineRating: 2, powerLevel: 1, currentVitae: 5, currentWillpower: 4);
            var sessionMock = new Mock<ISessionService>();
            sessionMock.Setup(s => s.BroadcastCharacterUpdateAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
            var sut = CreateService(ctx, CreateTraitResolverMock(2), sessionMock);
            await sut.ActivatePowerAsync(1, 1, "player1");
            sessionMock.Verify(s => s.BroadcastCharacterUpdateAsync(1), Times.Once);
        }
    }

    [Fact]
    public async Task ActivatePowerAsync_ZeroCost_DoesNotBroadcastCharacterUpdate()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown)
        {
            await SeedAsync(ctx, "—", _emptyPoolJson, disciplineRating: 2, powerLevel: 1, currentVitae: 5, currentWillpower: 4);
            var sessionMock = new Mock<ISessionService>();
            sessionMock.Setup(s => s.BroadcastCharacterUpdateAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
            var sut = CreateService(ctx, CreateTraitResolverMock(2), sessionMock);
            await sut.ActivatePowerAsync(1, 1, "player1");
            sessionMock.Verify(s => s.BroadcastCharacterUpdateAsync(It.IsAny<int>()), Times.Never);
        }
    }

    [Fact]
    public async Task ActivatePowerAsync_PlayerChoiceVitae_SpendsVitae()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown)
        {
            await SeedAsync(
                ctx,
                "1 Vitae or 1 Willpower",
                _emptyPoolJson,
                disciplineRating: 2,
                powerLevel: 1,
                currentVitae: 5,
                currentWillpower: 5);
            var sut = CreateService(ctx, CreateTraitResolverMock(3));
            int dice = await sut.ActivatePowerAsync(1, 1, "player1", DisciplineActivationResourceChoice.Vitae);
            Assert.Equal(3, dice);
            Character? reloaded = await ctx.Characters.AsNoTracking().FirstAsync(c => c.Id == 1);
            Assert.Equal(4, reloaded.CurrentVitae);
            Assert.Equal(5, reloaded.CurrentWillpower);
        }
    }

    [Fact]
    public async Task ActivatePowerAsync_PlayerChoiceWillpower_SpendsWillpower()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown)
        {
            await SeedAsync(
                ctx,
                "1 Vitae or 1 Willpower",
                _emptyPoolJson,
                disciplineRating: 2,
                powerLevel: 1,
                currentVitae: 5,
                currentWillpower: 5);
            var sut = CreateService(ctx, CreateTraitResolverMock(3));
            int dice = await sut.ActivatePowerAsync(1, 1, "player1", DisciplineActivationResourceChoice.Willpower);
            Assert.Equal(3, dice);
            Character? reloaded = await ctx.Characters.AsNoTracking().FirstAsync(c => c.Id == 1);
            Assert.Equal(5, reloaded.CurrentVitae);
            Assert.Equal(4, reloaded.CurrentWillpower);
        }
    }

    [Fact]
    public async Task ActivatePowerAsync_PlayerChoiceMissingChoice_ThrowsInvalidOperationException()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown)
        {
            await SeedAsync(
                ctx,
                "1 Vitae or 1 Willpower",
                _emptyPoolJson,
                disciplineRating: 2,
                powerLevel: 1,
                currentVitae: 5,
                currentWillpower: 5);
            var sut = CreateService(ctx, CreateTraitResolverMock(3));
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ActivatePowerAsync(1, 1, "player1"));
            Assert.Contains("resource choice", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task ActivatePowerAsync_PlayerChoiceNeitherResource_ThrowsInvalidOperationException()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown)
        {
            await SeedAsync(
                ctx,
                "1 Vitae or 1 Willpower",
                _emptyPoolJson,
                disciplineRating: 2,
                powerLevel: 1,
                currentVitae: 0,
                currentWillpower: 0);
            var sut = CreateService(ctx, CreateTraitResolverMock(3));
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                sut.ActivatePowerAsync(1, 1, "player1", DisciplineActivationResourceChoice.Vitae));
            Assert.Contains("neither Vitae nor Willpower", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
    }

}
