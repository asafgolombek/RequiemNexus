using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.DTOs;
using RequiemNexus.Application.Events;
using RequiemNexus.Application.Services;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.Models.Enums;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Events;
using RequiemNexus.Domain.Services;
using Xunit;

namespace RequiemNexus.Application.Tests;

public class CharacterDisciplineServiceTests
{
    private readonly Mock<IBeatLedgerService> _beatLedgerMock = new();
    private readonly ExperienceCostRules _experienceCostRules = new();

    private static Mock<IAuthorizationHelper> CreateAuthMock()
    {
        var auth = new Mock<IAuthorizationHelper>();
        auth.Setup(a => a.RequireCharacterAccessAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        auth.Setup(a => a.IsStorytellerAsync(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(false);
        return auth;
    }

    private static CharacterDisciplineService CreateService(
        ApplicationDbContext ctx,
        Mock<IAuthorizationHelper>? auth = null,
        Mock<IDomainEventDispatcher>? dispatcher = null,
        Mock<IHumanityService>? humanity = null)
    {
        return new CharacterDisciplineService(
            ctx,
            new Mock<IBeatLedgerService>().Object,
            new ExperienceCostRules(),
            (auth ?? CreateAuthMock()).Object,
            (dispatcher ?? new Mock<IDomainEventDispatcher>()).Object,
            (humanity ?? CreateHumanityMock()).Object);
    }

    private static Mock<IHumanityService> CreateHumanityMock()
    {
        var h = new Mock<IHumanityService>();
        h.Setup(x => x.GetEffectiveMaxHumanity(It.IsAny<Character>())).Returns(10);
        return h;
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

    private static async Task SeedApplicationUserAsync(ApplicationDbContext ctx)
    {
        if (await ctx.Users.AnyAsync())
        {
            return;
        }

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
        await ctx.SaveChangesAsync();
    }

    [Fact]
    public async Task AddDisciplineAsync_InClan_UsesCorrectXp()
    {
        using var ctx = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
        var auth = CreateAuthMock();
        var service = new CharacterDisciplineService(
            ctx,
            _beatLedgerMock.Object,
            _experienceCostRules,
            auth.Object,
            new Mock<IDomainEventDispatcher>().Object,
            CreateHumanityMock().Object);

        var clan = new Clan { Id = 1, Name = "Ventrue" };
        var disc = new Discipline { Id = 1, Name = "Resilience" };
        clan.ClanDisciplines.Add(new ClanDiscipline { ClanId = 1, DisciplineId = 1 });
        ctx.Clans.Add(clan);
        ctx.Disciplines.Add(disc);

        var character = new Character { Id = 1, Name = "Test", ClanId = 1, ExperiencePoints = 100, BloodPotency = 1 };
        character.Clan = clan;
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        var result = await service.AddDisciplineAsync(new DisciplineAcquisitionRequest(1, disc.Id, 1), "user1");

        Assert.True(result.IsSuccess);
        Assert.Equal(96, character.ExperiencePoints);
        Assert.Single(character.Disciplines);
        Assert.Equal(1, character.Disciplines.First().Rating);
        _beatLedgerMock.Verify(
            l => l.RecordXpSpendAsync(1, null, 4, XpExpense.Discipline, It.IsAny<string>(), "user1", null),
            Times.Once);
    }

    [Fact]
    public async Task AddDisciplineAsync_OutOfClan_UsesCorrectXp()
    {
        using var ctx = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
        var auth = CreateAuthMock();
        var service = new CharacterDisciplineService(
            ctx,
            _beatLedgerMock.Object,
            _experienceCostRules,
            auth.Object,
            new Mock<IDomainEventDispatcher>().Object,
            CreateHumanityMock().Object);

        var clan = new Clan { Id = 1, Name = "Ventrue" };
        var disc = new Discipline { Id = 2, Name = "Vigor", RequiresMentorBloodToLearn = true };
        ctx.Clans.Add(clan);
        ctx.Disciplines.Add(disc);

        var character = new Character { Id = 1, Name = "Test", ClanId = 1, ExperiencePoints = 100, BloodPotency = 1, CampaignId = 1 };
        character.Clan = clan;
        ctx.Campaigns.Add(new Campaign { Id = 1, Name = "C", StoryTellerId = "st" });
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        auth.Setup(a => a.IsStorytellerAsync(1, "st")).ReturnsAsync(true);
        var result = await service.AddDisciplineAsync(
            new DisciplineAcquisitionRequest(1, disc.Id, 1, AcquisitionAcknowledgedByST: true),
            "st");

        Assert.True(result.IsSuccess);
        Assert.Equal(95, character.ExperiencePoints);
        _beatLedgerMock.Verify(
            l => l.RecordXpSpendAsync(1, 1, 5, XpExpense.Discipline, It.IsAny<string>(), "st", It.IsAny<string?>()),
            Times.Once);
    }

    [Fact]
    public async Task TryUpgradeDisciplineAsync_SuccessfulUpgrade_InClan()
    {
        using var ctx = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
        var auth = CreateAuthMock();
        var service = new CharacterDisciplineService(
            ctx,
            _beatLedgerMock.Object,
            _experienceCostRules,
            auth.Object,
            new Mock<IDomainEventDispatcher>().Object,
            CreateHumanityMock().Object);

        var clan = new Clan { Id = 1, Name = "Ventrue" };
        var disc = new Discipline { Id = 1, Name = "Resilience" };
        clan.ClanDisciplines.Add(new ClanDiscipline { ClanId = 1, DisciplineId = 1 });
        ctx.Clans.Add(clan);
        ctx.Disciplines.Add(disc);

        var character = new Character { Id = 1, Name = "Test", ClanId = 1, ExperiencePoints = 100, BloodPotency = 1 };
        character.Clan = clan;
        var cd = new CharacterDiscipline { Id = 1, CharacterId = 1, DisciplineId = 1, Rating = 1 };
        character.Disciplines.Add(cd);
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        var result = await service.TryUpgradeDisciplineAsync(new DisciplineAcquisitionRequest(1, 1, 2), "user1");

        Assert.True(result.IsSuccess);
        Assert.Equal(92, character.ExperiencePoints);
        Assert.Equal(2, cd.Rating);
        _beatLedgerMock.Verify(
            l => l.RecordXpSpendAsync(1, null, 8, XpExpense.Discipline, It.IsAny<string>(), "user1", null),
            Times.Once);
    }

    [Fact]
    public async Task TryUpgradeDisciplineAsync_InsufficientXp_ReturnsFailure()
    {
        using var ctx = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
        var service = CreateService(ctx);

        var character = new Character { Id = 1, Name = "Test", ExperiencePoints = 2, BloodPotency = 1 };
        var cd = new CharacterDiscipline { Id = 1, CharacterId = 1, DisciplineId = 1, Rating = 1 };
        character.Disciplines.Add(cd);
        ctx.Disciplines.Add(new Discipline { Id = 1, Name = "X" });
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        var result = await service.TryUpgradeDisciplineAsync(new DisciplineAcquisitionRequest(1, 1, 2), "user1");

        Assert.False(result.IsSuccess);
        Assert.Equal(2, character.ExperiencePoints);
        Assert.Equal(1, cd.Rating);
    }

    [Fact]
    public async Task AddDiscipline_BloodlineRestriction_Fails()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown)
        {
            await SeedApplicationUserAsync(ctx);
            var cov = new CovenantDefinition { Id = 1, Name = "The Invictus" };
            var clan = new Clan { Id = 1, Name = "Ventrue" };
            ctx.CovenantDefinitions.Add(cov);
            ctx.Clans.Add(clan);
            ctx.Disciplines.Add(new Discipline { Id = 100, Name = "PlaceholderFourth" });
            await ctx.SaveChangesAsync();

            var blDef = new BloodlineDefinition { Id = 1, Name = "TestBloodline", FourthDisciplineId = 100 };
            ctx.BloodlineDefinitions.Add(blDef);
            var disc = new Discipline
            {
                Id = 1,
                Name = "Secret",
                IsBloodlineDiscipline = true,
                BloodlineId = 1,
            };
            ctx.Disciplines.Add(disc);
            var character = new Character
            {
                Id = 1,
                ApplicationUserId = "u1",
                Name = "K",
                ClanId = 1,
                ExperiencePoints = 50,
                BloodPotency = 1,
            };
            character.Clan = clan;
            ctx.Characters.Add(character);
            await ctx.SaveChangesAsync();

            var service = CreateService(ctx);
            var result = await service.AddDisciplineAsync(new DisciplineAcquisitionRequest(1, 1, 1), "u1");

            Assert.False(result.IsSuccess);
            Assert.Contains("bloodline", result.Error, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task AddDiscipline_CovenantGate_NoMembership_Fails()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown)
        {
            await SeedApplicationUserAsync(ctx);
            var circle = new CovenantDefinition { Id = 5, Name = "The Circle of the Crone" };
            var disc = new Discipline
            {
                Id = 2,
                Name = "Crúac",
                IsCovenantDiscipline = true,
                CovenantId = 5,
            };
            disc.Covenant = circle;
            var clan = new Clan { Id = 1, Name = "Gangrel" };
            ctx.CovenantDefinitions.Add(circle);
            ctx.Clans.Add(clan);
            ctx.Disciplines.Add(disc);
            var character = new Character
            {
                Id = 1,
                ApplicationUserId = "u1",
                Name = "K",
                ClanId = 1,
                ExperiencePoints = 50,
                BloodPotency = 1,
            };
            character.Clan = clan;
            ctx.Characters.Add(character);
            await ctx.SaveChangesAsync();

            var service = CreateService(ctx);
            var result = await service.AddDisciplineAsync(new DisciplineAcquisitionRequest(1, 2, 1), "u1");

            Assert.False(result.IsSuccess);
            Assert.Contains("Covenant", result.Error, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task AddDiscipline_CovenantGate_STBypass_Succeeds_WithAuditNote()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown)
        {
            await SeedApplicationUserAsync(ctx);
            var circle = new CovenantDefinition { Id = 5, Name = "The Circle of the Crone" };
            var disc = new Discipline
            {
                Id = 2,
                Name = "Crúac",
                IsCovenantDiscipline = true,
                CovenantId = 5,
            };
            disc.Covenant = circle;
            var clan = new Clan { Id = 1, Name = "Gangrel" };
            ctx.CovenantDefinitions.Add(circle);
            ctx.Clans.Add(clan);
            ctx.Disciplines.Add(disc);
            ctx.Users.Add(
                new ApplicationUser
                {
                    Id = "st1",
                    UserName = "st1",
                    NormalizedUserName = "ST1",
                    Email = "st1@test",
                    NormalizedEmail = "ST1@TEST",
                    EmailConfirmed = true,
                });
            ctx.Campaigns.Add(new Campaign { Id = 10, Name = "Ch", StoryTellerId = "st1" });
            var character = new Character
            {
                Id = 1,
                ApplicationUserId = "u1",
                Name = "K",
                ClanId = 1,
                CampaignId = 10,
                ExperiencePoints = 50,
                BloodPotency = 1,
            };
            character.Clan = clan;
            ctx.Characters.Add(character);
            await ctx.SaveChangesAsync();

            var auth = CreateAuthMock();
            auth.Setup(a => a.IsStorytellerAsync(10, "st1")).ReturnsAsync(true);
            var beat = new Mock<IBeatLedgerService>();
            var service = new CharacterDisciplineService(
                ctx,
                beat.Object,
                _experienceCostRules,
                auth.Object,
                new Mock<IDomainEventDispatcher>().Object,
                CreateHumanityMock().Object);

            var result = await service.AddDisciplineAsync(
                new DisciplineAcquisitionRequest(1, 2, 1, AcquisitionAcknowledgedByST: true),
                "st1");

            Assert.True(result.IsSuccess);
            beat.Verify(
                b => b.RecordXpSpendAsync(
                    1,
                    10,
                    It.IsAny<int>(),
                    XpExpense.Discipline,
                    It.IsAny<string>(),
                    "st1",
                    It.Is<string?>(n => n != null && n.Contains("gate-override:covenant", StringComparison.Ordinal))),
                Times.Once);
        }
    }

    [Fact]
    public async Task AddDiscipline_ThebanFloor_Fails()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown)
        {
            await SeedApplicationUserAsync(ctx);
            var lancea = new CovenantDefinition { Id = 3, Name = "The Lancea et Sanctum" };
            var disc = new Discipline
            {
                Id = 4,
                Name = "Theban Sorcery",
                IsCovenantDiscipline = true,
                CovenantId = 3,
            };
            disc.Covenant = lancea;
            var clan = new Clan { Id = 1, Name = "Ventrue" };
            ctx.Clans.Add(clan);
            ctx.CovenantDefinitions.Add(lancea);
            ctx.Disciplines.Add(disc);
            ctx.Campaigns.Add(new Campaign { Id = 1, Name = "C", StoryTellerId = "u1" });
            var character = new Character
            {
                Id = 1,
                ApplicationUserId = "u1",
                Name = "K",
                ClanId = 1,
                CampaignId = 1,
                Humanity = 2,
                CovenantId = 3,
                ExperiencePoints = 80,
                BloodPotency = 1,
            };
            character.Clan = clan;
            ctx.Characters.Add(character);
            await ctx.SaveChangesAsync();

            var service = CreateService(ctx);
            var result = await service.AddDisciplineAsync(new DisciplineAcquisitionRequest(1, 4, 3), "u1");

            Assert.False(result.IsSuccess);
            Assert.Contains("Humanity", result.Error, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task AddDiscipline_TeacherGate_OutOfClan_Fails()
    {
        using var ctx = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
        var clan = new Clan { Id = 1, Name = "Ventrue" };
        var inClan = new Discipline { Id = 1, Name = "Animalism" };
        clan.ClanDisciplines.Add(new ClanDiscipline { ClanId = 1, DisciplineId = 1 });
        var auspex = new Discipline { Id = 2, Name = "Auspex", RequiresMentorBloodToLearn = true };
        ctx.Clans.Add(clan);
        ctx.Disciplines.AddRange(inClan, auspex);
        var character = new Character { Id = 1, Name = "T", ClanId = 1, ExperiencePoints = 50, BloodPotency = 1 };
        character.Clan = clan;
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        var service = CreateService(ctx);
        var result = await service.AddDisciplineAsync(new DisciplineAcquisitionRequest(1, 2, 1), "u1");

        Assert.False(result.IsSuccess);
        Assert.Contains("Vitae", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AddDiscipline_Cruac_AtHumanity4_DispatchesDegenEvent()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown)
        {
            await SeedApplicationUserAsync(ctx);
            var circle = new CovenantDefinition { Id = 5, Name = "The Circle of the Crone" };
            var disc = new Discipline { Id = 2, Name = "Crúac", IsCovenantDiscipline = true, CovenantId = 5 };
            disc.Covenant = circle;
            var clan = new Clan { Id = 1, Name = "Gangrel" };
            ctx.CovenantDefinitions.Add(circle);
            ctx.Clans.Add(clan);
            ctx.Disciplines.Add(disc);
            var character = new Character
            {
                Id = 1,
                ApplicationUserId = "u1",
                Name = "K",
                ClanId = 1,
                CovenantId = 5,
                Humanity = 4,
                ExperiencePoints = 50,
                BloodPotency = 1,
            };
            character.Clan = clan;
            ctx.Characters.Add(character);
            await ctx.SaveChangesAsync();

            var dispatcher = new Mock<IDomainEventDispatcher>();
            var auth = CreateAuthMock();
            var service = new CharacterDisciplineService(
                ctx,
                new Mock<IBeatLedgerService>().Object,
                _experienceCostRules,
                auth.Object,
                dispatcher.Object,
                CreateHumanityMock().Object);

            var result = await service.AddDisciplineAsync(new DisciplineAcquisitionRequest(1, 2, 1), "u1");

            Assert.True(result.IsSuccess);
            dispatcher.Verify(
                d => d.Dispatch(It.Is<DegenerationCheckRequiredEvent>(e =>
                    e.CharacterId == 1 && e.Reason == DegenerationReason.CrúacPurchase)),
                Times.Once);
        }
    }

    [Fact]
    public async Task AddDiscipline_Cruac_BelowHumanity4_NoDegenEvent()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown)
        {
            await SeedApplicationUserAsync(ctx);
            var circle = new CovenantDefinition { Id = 5, Name = "The Circle of the Crone" };
            var disc = new Discipline { Id = 2, Name = "Crúac", IsCovenantDiscipline = true, CovenantId = 5 };
            disc.Covenant = circle;
            var clan = new Clan { Id = 1, Name = "Gangrel" };
            ctx.CovenantDefinitions.Add(circle);
            ctx.Clans.Add(clan);
            ctx.Disciplines.Add(disc);
            var character = new Character
            {
                Id = 1,
                ApplicationUserId = "u1",
                Name = "K",
                ClanId = 1,
                CovenantId = 5,
                Humanity = 3,
                ExperiencePoints = 50,
                BloodPotency = 1,
            };
            character.Clan = clan;
            ctx.Characters.Add(character);
            await ctx.SaveChangesAsync();

            var dispatcher = new Mock<IDomainEventDispatcher>();
            var service = new CharacterDisciplineService(
                ctx,
                new Mock<IBeatLedgerService>().Object,
                _experienceCostRules,
                CreateAuthMock().Object,
                dispatcher.Object,
                CreateHumanityMock().Object);

            var result = await service.AddDisciplineAsync(new DisciplineAcquisitionRequest(1, 2, 1), "u1");

            Assert.True(result.IsSuccess);
            dispatcher.Verify(d => d.Dispatch(It.IsAny<DegenerationCheckRequiredEvent>()), Times.Never);
        }
    }

    [Fact]
    public async Task AddDiscipline_Necromancy_NotMekhet_NoBloodline_Fails()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown)
        {
            await SeedApplicationUserAsync(ctx);
            var nec = new Discipline { Id = 9, Name = "Necromancy", IsNecromancy = true };
            var clan = new Clan { Id = 1, Name = "Ventrue" };
            ctx.Clans.Add(clan);
            ctx.Disciplines.Add(nec);
            var character = new Character
            {
                Id = 1,
                ApplicationUserId = "u1",
                Name = "K",
                ClanId = 1,
                ExperiencePoints = 50,
                BloodPotency = 1,
            };
            character.Clan = clan;
            ctx.Characters.Add(character);
            await ctx.SaveChangesAsync();

            var service = CreateService(ctx);
            var result = await service.AddDisciplineAsync(new DisciplineAcquisitionRequest(1, 9, 1), "u1");

            Assert.False(result.IsSuccess);
            Assert.Contains("Necromancy", result.Error, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task AddDiscipline_Necromancy_MekhetClan_Succeeds()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown)
        {
            await SeedApplicationUserAsync(ctx);
            var nec = new Discipline { Id = 9, Name = "Necromancy", IsNecromancy = true };
            var clan = new Clan { Id = 1, Name = "Mekhet" };
            ctx.Clans.Add(clan);
            ctx.Disciplines.Add(nec);
            var character = new Character
            {
                Id = 1,
                ApplicationUserId = "u1",
                Name = "K",
                ClanId = 1,
                ExperiencePoints = 50,
                BloodPotency = 1,
            };
            character.Clan = clan;
            ctx.Characters.Add(character);
            await ctx.SaveChangesAsync();

            var service = CreateService(ctx);
            var result = await service.AddDisciplineAsync(new DisciplineAcquisitionRequest(1, 9, 1), "u1");

            Assert.True(result.IsSuccess);
        }
    }

    [Fact]
    public async Task AddDiscipline_Necromancy_NecromancyBloodline_Succeeds()
    {
        (ApplicationDbContext ctx, IAsyncDisposable teardown) = await CreateSqliteContextAsync();
        await using (teardown)
        {
            await SeedApplicationUserAsync(ctx);
            var nec = new Discipline { Id = 9, Name = "Necromancy", IsNecromancy = true };
            ctx.Disciplines.Add(nec);
            await ctx.SaveChangesAsync();

            var bl = new BloodlineDefinition { Id = 2, Name = "Nephilim", FourthDisciplineId = 9 };
            var clan = new Clan { Id = 1, Name = "Ventrue" };
            ctx.BloodlineDefinitions.Add(bl);
            ctx.Clans.Add(clan);
            var character = new Character
            {
                Id = 1,
                ApplicationUserId = "u1",
                Name = "K",
                ClanId = 1,
                ExperiencePoints = 50,
                BloodPotency = 1,
            };
            character.Clan = clan;
            character.Bloodlines.Add(new CharacterBloodline
            {
                BloodlineDefinitionId = 2,
                Status = BloodlineStatus.Active,
                BloodlineDefinition = bl,
            });
            ctx.Characters.Add(character);
            await ctx.SaveChangesAsync();

            var service = CreateService(ctx);
            var result = await service.AddDisciplineAsync(new DisciplineAcquisitionRequest(1, 9, 1), "u1");

            Assert.True(result.IsSuccess);
        }
    }
}
