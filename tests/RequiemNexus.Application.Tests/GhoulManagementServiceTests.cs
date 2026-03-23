using System.Diagnostics.Metrics;
using System.Text.Json;
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
using RequiemNexus.Data.RealTime;
using RequiemNexus.Domain.Models;
using Xunit;

namespace RequiemNexus.Application.Tests;

/// <summary>
/// Integration-style tests for <see cref="GhoulManagementService"/>.
/// </summary>
public class GhoulManagementServiceTests
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

    private static GhoulManagementService CreateService(
        DbContextOptions<ApplicationDbContext> options,
        IAuthorizationHelper? auth = null,
        Mock<ISessionService>? sessionMock = null)
    {
        var session = sessionMock?.Object ?? CreateSessionMock().Object;
        return new GhoulManagementService(
            new TestApplicationDbContextFactory(options),
            auth ?? CreatePermissiveAuthMock().Object,
            new RelationshipWebMetrics(_meterFactory.Value),
            session,
            NullLogger<GhoulManagementService>.Instance);
    }

    private static Mock<ISessionService> CreateSessionMock()
    {
        var mock = new Mock<ISessionService>();
        mock.Setup(s => s.BroadcastCharacterUpdateAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
        mock.Setup(s => s.BroadcastRelationshipUpdateAsync(It.IsAny<int>(), It.IsAny<RelationshipUpdateDto>()))
            .Returns(Task.CompletedTask);
        return mock;
    }

    private static Mock<IAuthorizationHelper> CreatePermissiveAuthMock()
    {
        var mock = new Mock<IAuthorizationHelper>();
        mock.Setup(a => a.RequireStorytellerAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        mock.Setup(a => a.RequireCharacterAccessAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        return mock;
    }

    private static async Task SeedChronicleWithRegnantAsync(ApplicationDbContext ctx)
    {
        ctx.Campaigns.Add(new Campaign { Id = 1, Name = "C1", StoryTellerId = "st" });
        ctx.Clans.Add(new Clan { Id = 1, Name = "Gangrel" });
        ctx.Disciplines.AddRange(
            new Discipline { Id = 10, Name = "Animalism", Description = string.Empty },
            new Discipline { Id = 11, Name = "Protean", Description = string.Empty },
            new Discipline { Id = 12, Name = "Resilience", Description = string.Empty },
            new Discipline { Id = 99, Name = "Auspex", Description = string.Empty });
        ctx.ClanDisciplines.AddRange(
            new ClanDiscipline { Id = 1, ClanId = 1, DisciplineId = 10 },
            new ClanDiscipline { Id = 2, ClanId = 1, DisciplineId = 11 },
            new ClanDiscipline { Id = 3, ClanId = 1, DisciplineId = 12 });
        ctx.Characters.Add(new Character
        {
            Id = 1,
            Name = "Regnant",
            ApplicationUserId = "u1",
            ClanId = 1,
            CampaignId = 1,
            BloodPotency = 2,
            Beats = 0,
        });
        ctx.ChronicleNpcs.Add(new ChronicleNpc
        {
            Id = 1,
            CampaignId = 1,
            Name = "Story NPC",
            PublicDescription = "x",
            StorytellerNotes = string.Empty,
        });
        await ctx.SaveChangesAsync();
    }

    [Fact]
    public async Task CreateGhoul_Feed_UpdatesLastFedAndVitae()
    {
        DbContextOptions<ApplicationDbContext> options = CreateOptions(nameof(CreateGhoul_Feed_UpdatesLastFedAndVitae));
        await using var ctx = new ApplicationDbContext(options);
        await SeedChronicleWithRegnantAsync(ctx);
        GhoulManagementService sut = CreateService(options);

        Result<GhoulDto> created = await sut.CreateGhoulAsync(
            new CreateGhoulRequest(1, "Rolf", RegnantCharacterId: 1, null, null, null, null, null),
            "st");

        Assert.True(created.IsSuccess);
        Assert.Null(created.Value!.LastFedAt);
        Assert.Equal(0, created.Value.VitaeInSystem);

        Result<GhoulDto> fed = await sut.FeedGhoulAsync(created.Value.Id, "st");
        Assert.True(fed.IsSuccess);
        Assert.NotNull(fed.Value!.LastFedAt);
        Assert.Equal(1, fed.Value.VitaeInSystem);
    }

    [Fact]
    public async Task GetAgingAlerts_ReturnsOverdueGhouls()
    {
        DbContextOptions<ApplicationDbContext> options = CreateOptions(nameof(GetAgingAlerts_ReturnsOverdueGhouls));
        await using var ctx = new ApplicationDbContext(options);
        await SeedChronicleWithRegnantAsync(ctx);
        DateTime old = DateTime.UtcNow - TimeSpan.FromDays(45);
        ctx.Ghouls.Add(new Ghoul
        {
            ChronicleId = 1,
            Name = "Hungry",
            RegnantCharacterId = 1,
            LastFedAt = old,
            VitaeInSystem = 1,
            CreatedAt = old,
        });
        await ctx.SaveChangesAsync();

        GhoulManagementService sut = CreateService(options);
        IReadOnlyList<GhoulAgingAlertDto> alerts = await sut.GetAgingAlertsAsync(1, "st");

        Assert.Single(alerts);
        Assert.True(alerts[0].OverdueMonths >= 0);
    }

    [Fact]
    public async Task ReleaseGhoul_ExcludedFromActiveList()
    {
        DbContextOptions<ApplicationDbContext> options = CreateOptions(nameof(ReleaseGhoul_ExcludedFromActiveList));
        await using var ctx = new ApplicationDbContext(options);
        await SeedChronicleWithRegnantAsync(ctx);
        GhoulManagementService sut = CreateService(options);

        Result<GhoulDto> created = await sut.CreateGhoulAsync(
            new CreateGhoulRequest(1, "Temp", null, 1, null, null, null, null),
            "st");
        Assert.True(created.IsSuccess);

        Result<Unit> released = await sut.ReleaseGhoulAsync(created.Value!.Id, "st");
        Assert.True(released.IsSuccess);

        IReadOnlyList<GhoulDto> list = await sut.GetGhoulsForChronicleAsync(1, "st");
        Assert.Empty(list);
    }

    [Fact]
    public async Task Mutations_Throw_WhenStorytellerDenied()
    {
        DbContextOptions<ApplicationDbContext> options = CreateOptions(nameof(Mutations_Throw_WhenStorytellerDenied));
        await using var ctx = new ApplicationDbContext(options);
        await SeedChronicleWithRegnantAsync(ctx);

        var auth = new Mock<IAuthorizationHelper>();
        auth.Setup(a => a.RequireStorytellerAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new UnauthorizedAccessException());

        GhoulManagementService sut = CreateService(options, auth: auth.Object);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            sut.CreateGhoulAsync(
                new CreateGhoulRequest(1, "X", 1, null, null, null, null, null),
                "not-st"));
    }

    [Fact]
    public async Task SetDisciplineAccess_EnforcesBloodPotency_ForPcRegnant()
    {
        DbContextOptions<ApplicationDbContext> options = CreateOptions(nameof(SetDisciplineAccess_EnforcesBloodPotency_ForPcRegnant));
        await using var ctx = new ApplicationDbContext(options);
        await SeedChronicleWithRegnantAsync(ctx);
        GhoulManagementService sut = CreateService(options);

        Result<GhoulDto> created = await sut.CreateGhoulAsync(
            new CreateGhoulRequest(1, "Dotty", RegnantCharacterId: 1, null, null, null, null, null),
            "st");
        Assert.True(created.IsSuccess);

        Result<Unit> tooMany = await sut.SetDisciplineAccessAsync(
            created.Value!.Id,
            [10, 11, 12],
            "st");
        Assert.False(tooMany.IsSuccess);

        Result<Unit> ok = await sut.SetDisciplineAccessAsync(created.Value.Id, [10, 11], "st");
        Assert.True(ok.IsSuccess);

        ctx.ChangeTracker.Clear();
        Ghoul? row = await ctx.Ghouls.AsNoTracking().FirstAsync(g => g.Id == created.Value.Id);
        int[]? stored = JsonSerializer.Deserialize<int[]>(row.AccessibleDisciplinesJson!);
        Assert.NotNull(stored);
        Assert.Equal(2, stored!.Length);
    }

    [Fact]
    public async Task SetDisciplineAccess_RejectsOutOfClan_ForPcRegnant()
    {
        DbContextOptions<ApplicationDbContext> options = CreateOptions(nameof(SetDisciplineAccess_RejectsOutOfClan_ForPcRegnant));
        await using var ctx = new ApplicationDbContext(options);
        await SeedChronicleWithRegnantAsync(ctx);
        GhoulManagementService sut = CreateService(options);

        Result<GhoulDto> created = await sut.CreateGhoulAsync(
            new CreateGhoulRequest(1, "Spy", RegnantCharacterId: 1, null, null, null, null, null),
            "st");
        Assert.True(created.IsSuccess);

        Result<Unit> bad = await sut.SetDisciplineAccessAsync(created.Value!.Id, [99], "st");
        Assert.False(bad.IsSuccess);
    }

    [Fact]
    public async Task SetDisciplineAccess_SkipsValidation_ForNpcRegnant()
    {
        DbContextOptions<ApplicationDbContext> options = CreateOptions(nameof(SetDisciplineAccess_SkipsValidation_ForNpcRegnant));
        await using var ctx = new ApplicationDbContext(options);
        await SeedChronicleWithRegnantAsync(ctx);
        GhoulManagementService sut = CreateService(options);

        Result<GhoulDto> created = await sut.CreateGhoulAsync(
            new CreateGhoulRequest(1, "NPC pet", null, 1, null, null, null, null),
            "st");
        Assert.True(created.IsSuccess);

        Result<Unit> ok = await sut.SetDisciplineAccessAsync(
            created.Value!.Id,
            [10, 11, 12, 99],
            "st");
        Assert.True(ok.IsSuccess);
    }

    [Fact]
    public async Task GetGhoulsForRegnant_CallsCharacterAccess()
    {
        DbContextOptions<ApplicationDbContext> options = CreateOptions(nameof(GetGhoulsForRegnant_CallsCharacterAccess));
        await using var ctx = new ApplicationDbContext(options);
        await SeedChronicleWithRegnantAsync(ctx);
        ctx.Ghouls.Add(new Ghoul
        {
            ChronicleId = 1,
            Name = "Minion",
            RegnantCharacterId = 1,
            LastFedAt = DateTime.UtcNow,
            VitaeInSystem = 1,
            CreatedAt = DateTime.UtcNow,
        });
        await ctx.SaveChangesAsync();

        var auth = CreatePermissiveAuthMock();
        GhoulManagementService sut = CreateService(options, auth: auth.Object);

        IReadOnlyList<GhoulDto> list = await sut.GetGhoulsForRegnantAsync(1, "u1");

        Assert.Single(list);
        auth.Verify(a => a.RequireCharacterAccessAsync(1, "u1", It.IsAny<string>()), Times.Once);
    }
}
