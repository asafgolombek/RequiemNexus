using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.DTOs;
using RequiemNexus.Application.Events;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Application.Services;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.Models.Enums;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Models;
using Xunit;

namespace RequiemNexus.Application.Tests;

/// <summary>
/// Tests for <see cref="SorceryActivationService"/> — paid rite activation and resource deduction.
/// </summary>
public class SorceryServiceTests
{
    private const string _defaultRequirementsJson = """[{"type":"InternalVitae","value":1,"isConsumed":true}]""";

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

    private static SorceryActivationService CreateService(
        ApplicationDbContext ctx,
        ITraitResolver traitResolver,
        IAuthorizationHelper? authHelper = null)
    {
        var auth = authHelper ?? CreatePermissiveAuthMock().Object;
        var sessionService = new Mock<ISessionService>();
        sessionService
            .Setup(s => s.BroadcastCharacterUpdateAsync(It.IsAny<int>()))
            .Returns(Task.CompletedTask);
        var logger = new Mock<ILogger<SorceryActivationService>>().Object;
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
        return new SorceryActivationService(
            ctx,
            auth,
            sessionService.Object,
            traitResolver,
            vitaeService,
            willpowerService,
            logger);
    }

    /// <summary>SQLite in-memory so relational transactions and Vitae/Willpower services are exercised.</summary>
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
        return mock.Object;
    }

    [Fact]
    public async Task BeginRiteActivationAsync_DeductsVitaeAndReturnsPool()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown)
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
            var covenant = new CovenantDefinition
            {
                Id = 1,
                Name = "The Circle of the Crone",
                SupportsBloodSorcery = true,
            };
            var discipline = new Discipline { Id = 10, Name = "Crúac" };
            ctx.CovenantDefinitions.Add(covenant);
            ctx.Disciplines.Add(discipline);
            ctx.Characters.Add(new Character
            {
                Id = 1,
                Name = "Rita",
                ApplicationUserId = "player1",
                CovenantId = 1,
                CurrentVitae = 5,
                CurrentWillpower = 4,
                HumanityStains = 0,
                BloodPotency = 1,
                MaxVitae = 10,
                MaxWillpower = 5,
            });
            ctx.SorceryRiteDefinitions.Add(new SorceryRiteDefinition
            {
                Id = 1,
                Name = "Test Rite",
                Description = "Test",
                Level = 1,
                SorceryType = SorceryType.Cruac,
                XpCost = 1,
                RequiredCovenantId = 1,
                RequirementsJson = _defaultRequirementsJson,
                PoolDefinitionJson = """{"traits":[]}""",
            });
            ctx.CharacterRites.Add(new CharacterRite
            {
                Id = 1,
                CharacterId = 1,
                SorceryRiteDefinitionId = 1,
                Status = RiteLearnStatus.Approved,
            });
            await ctx.SaveChangesAsync();

            var sut = CreateService(ctx, CreateTraitResolverMock(7));
            int dice = await sut.BeginRiteActivationAsync(1, 1, "player1", new BeginRiteActivationRequest());

            Assert.Equal(7, dice);
            Character? reloaded = await ctx.Characters.AsNoTracking().FirstAsync(c => c.Id == 1);
            Assert.Equal(4, reloaded.CurrentVitae);
        }
    }

    [Fact]
    public async Task BeginRiteActivationAsync_InsufficientVitae_Throws()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown)
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
            ctx.CovenantDefinitions.Add(new CovenantDefinition
            {
                Id = 1,
                Name = "The Circle of the Crone",
                SupportsBloodSorcery = true,
            });
            ctx.Disciplines.Add(new Discipline { Id = 10, Name = "Crúac" });
            ctx.Characters.Add(new Character
            {
                Id = 1,
                Name = "Rita",
                ApplicationUserId = "player1",
                CovenantId = 1,
                CurrentVitae = 0,
                CurrentWillpower = 4,
                BloodPotency = 1,
                MaxVitae = 10,
                MaxWillpower = 5,
            });
            ctx.SorceryRiteDefinitions.Add(new SorceryRiteDefinition
            {
                Id = 1,
                Name = "Test Rite",
                Description = "Test",
                Level = 1,
                SorceryType = SorceryType.Cruac,
                XpCost = 1,
                RequiredCovenantId = 1,
                RequirementsJson = _defaultRequirementsJson,
                PoolDefinitionJson = """{"traits":[]}""",
            });
            ctx.CharacterRites.Add(new CharacterRite
            {
                Id = 1,
                CharacterId = 1,
                SorceryRiteDefinitionId = 1,
                Status = RiteLearnStatus.Approved,
            });
            await ctx.SaveChangesAsync();

            var sut = CreateService(ctx, CreateTraitResolverMock(1));
            InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                sut.BeginRiteActivationAsync(1, 1, "player1", new BeginRiteActivationRequest()));

            Assert.Contains("Vitae", ex.Message, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task BeginRiteActivationAsync_AppliesHumanityStains()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown)
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
            ctx.CovenantDefinitions.Add(new CovenantDefinition
            {
                Id = 1,
                Name = "The Circle of the Crone",
                SupportsBloodSorcery = true,
            });
            ctx.Disciplines.Add(new Discipline { Id = 10, Name = "Crúac" });
            ctx.Characters.Add(new Character
            {
                Id = 1,
                Name = "Rita",
                ApplicationUserId = "player1",
                CovenantId = 1,
                CurrentVitae = 5,
                CurrentWillpower = 4,
                HumanityStains = 1,
                BloodPotency = 1,
                MaxVitae = 10,
                MaxWillpower = 5,
            });
            ctx.SorceryRiteDefinitions.Add(new SorceryRiteDefinition
            {
                Id = 1,
                Name = "Stain Rite",
                Description = "Test",
                Level = 1,
                SorceryType = SorceryType.Cruac,
                XpCost = 1,
                RequiredCovenantId = 1,
                RequirementsJson = """[{"type":"HumanityStain","value":2,"isConsumed":true}]""",
                PoolDefinitionJson = null,
            });
            ctx.CharacterRites.Add(new CharacterRite
            {
                Id = 1,
                CharacterId = 1,
                SorceryRiteDefinitionId = 1,
                Status = RiteLearnStatus.Approved,
            });
            await ctx.SaveChangesAsync();

            var sut = CreateService(ctx, CreateTraitResolverMock(0));
            int dice = await sut.BeginRiteActivationAsync(1, 1, "player1", new BeginRiteActivationRequest());

            Assert.Equal(0, dice);
            Character? reloaded = await ctx.Characters.AsNoTracking().FirstAsync(c => c.Id == 1);
            Assert.Equal(3, reloaded.HumanityStains);
        }
    }
}
