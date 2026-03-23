using System.Diagnostics.Metrics;
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
using RequiemNexus.Domain.Contracts;
using RequiemNexus.Domain.Models;
using Xunit;

namespace RequiemNexus.Application.Tests;

/// <summary>
/// Integration-style tests for <see cref="KindredLineageService"/> (authorization, lineage validation, Blood Sympathy).
/// </summary>
public class KindredLineageServiceTests
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

    private static KindredLineageService CreateService(
        DbContextOptions<ApplicationDbContext> options,
        IAuthorizationHelper? authHelper = null,
        ITraitResolver? traitResolver = null,
        IDiceService? diceService = null)
    {
        var auth = authHelper ?? CreatePermissiveAuthMock().Object;
        var traits = traitResolver ?? CreateTraitResolverMock(2).Object;
        var dice = diceService ?? CreateDiceMock(1).Object;
        return new KindredLineageService(
            new TestApplicationDbContextFactory(options),
            auth,
            traits,
            dice,
            new RelationshipWebMetrics(_meterFactory.Value),
            NullLogger<KindredLineageService>.Instance);
    }

    private static Mock<IAuthorizationHelper> CreatePermissiveAuthMock()
    {
        var mock = new Mock<IAuthorizationHelper>();
        mock.Setup(a => a.RequireStorytellerAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        mock.Setup(a => a.RequireCharacterAccessAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        mock.Setup(a => a.RequireCampaignMemberAsync(It.IsAny<ApplicationDbContext>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        return mock;
    }

    private static Mock<IDiceService> CreateDiceMock(int successes)
    {
        var mock = new Mock<IDiceService>();
        mock.Setup(d => d.Roll(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int?>()))
            .Returns(new RollResult { Successes = successes });
        return mock;
    }

    private static Mock<ITraitResolver> CreateTraitResolverMock(int pool)
    {
        var mock = new Mock<ITraitResolver>();
        mock.Setup(t => t.ResolvePoolAsync(It.IsAny<Character>(), It.IsAny<PoolDefinition>()))
            .ReturnsAsync(pool);
        return mock;
    }

    private static async Task SeedTwoPcLineageCampaignAsync(ApplicationDbContext ctx)
    {
        ctx.Campaigns.Add(new Campaign { Id = 1, Name = "C1", StoryTellerId = "st" });
        ctx.Clans.Add(new Clan { Id = 1, Name = "Gangrel" });
        ctx.Characters.Add(new Character
        {
            Id = 1,
            Name = "Sire",
            ApplicationUserId = "u1",
            ClanId = 1,
            CampaignId = 1,
            BloodPotency = 4,
        });
        ctx.Characters.Add(new Character
        {
            Id = 2,
            Name = "Childe",
            ApplicationUserId = "u2",
            ClanId = 1,
            CampaignId = 1,
            BloodPotency = 4,
            SireCharacterId = 1,
        });
        await ctx.SaveChangesAsync();
    }

    [Fact]
    public async Task SetSireCharacterAsync_SetsSireAndClearsNpcAndDisplayName()
    {
        var options = CreateOptions(nameof(SetSireCharacterAsync_SetsSireAndClearsNpcAndDisplayName));
        await using (var ctx = new ApplicationDbContext(options))
        {
            ctx.Campaigns.Add(new Campaign { Id = 1, Name = "C", StoryTellerId = "st" });
            ctx.Clans.Add(new Clan { Id = 1, Name = "Gangrel" });
            ctx.Characters.Add(new Character { Id = 10, Name = "A", ApplicationUserId = "u", ClanId = 1, CampaignId = 1, SireDisplayName = "Old" });
            ctx.Characters.Add(new Character { Id = 11, Name = "B", ApplicationUserId = "u", ClanId = 1, CampaignId = 1 });
            await ctx.SaveChangesAsync();
        }

        var service = CreateService(options);
        Result<Unit> result = await service.SetSireCharacterAsync(10, 11, "st");
        Assert.True(result.IsSuccess);

        await using var verify = new ApplicationDbContext(options);
        Character a = await verify.Characters.SingleAsync(c => c.Id == 10);
        Assert.Equal(11, a.SireCharacterId);
        Assert.Null(a.SireNpcId);
        Assert.Null(a.SireDisplayName);
    }

    [Fact]
    public async Task SetSireCharacterAsync_SelfSire_ReturnsFailure()
    {
        var options = CreateOptions(nameof(SetSireCharacterAsync_SelfSire_ReturnsFailure));
        await using (var ctx = new ApplicationDbContext(options))
        {
            ctx.Campaigns.Add(new Campaign { Id = 1, Name = "C", StoryTellerId = "st" });
            ctx.Clans.Add(new Clan { Id = 1, Name = "Gangrel" });
            ctx.Characters.Add(new Character { Id = 1, Name = "A", ApplicationUserId = "u", ClanId = 1, CampaignId = 1 });
            await ctx.SaveChangesAsync();
        }

        var service = CreateService(options);
        Result<Unit> result = await service.SetSireCharacterAsync(1, 1, "st");
        Assert.False(result.IsSuccess);
        Assert.Contains("own sire", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SetSireCharacterAsync_Cycle_ReturnsFailure()
    {
        var options = CreateOptions(nameof(SetSireCharacterAsync_Cycle_ReturnsFailure));
        await using (var ctx = new ApplicationDbContext(options))
        {
            ctx.Campaigns.Add(new Campaign { Id = 1, Name = "C", StoryTellerId = "st" });
            ctx.Clans.Add(new Clan { Id = 1, Name = "Gangrel" });
            ctx.Characters.Add(new Character { Id = 1, Name = "A", ApplicationUserId = "u", ClanId = 1, CampaignId = 1 });
            ctx.Characters.Add(new Character { Id = 2, Name = "B", ApplicationUserId = "u", ClanId = 1, CampaignId = 1, SireCharacterId = 1 });
            await ctx.SaveChangesAsync();
        }

        var service = CreateService(options);
        Result<Unit> result = await service.SetSireCharacterAsync(1, 2, "st");
        Assert.False(result.IsSuccess);
        Assert.Contains("cycle", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SetSireCharacterAsync_CrossChronicle_ReturnsFailure()
    {
        var options = CreateOptions(nameof(SetSireCharacterAsync_CrossChronicle_ReturnsFailure));
        await using (var ctx = new ApplicationDbContext(options))
        {
            ctx.Campaigns.Add(new Campaign { Id = 1, Name = "C1", StoryTellerId = "st" });
            ctx.Campaigns.Add(new Campaign { Id = 2, Name = "C2", StoryTellerId = "st" });
            ctx.Clans.Add(new Clan { Id = 1, Name = "Gangrel" });
            ctx.Characters.Add(new Character { Id = 1, Name = "A", ApplicationUserId = "u", ClanId = 1, CampaignId = 1 });
            ctx.Characters.Add(new Character { Id = 2, Name = "B", ApplicationUserId = "u", ClanId = 1, CampaignId = 2 });
            await ctx.SaveChangesAsync();
        }

        var service = CreateService(options);
        Result<Unit> result = await service.SetSireCharacterAsync(1, 2, "st");
        Assert.False(result.IsSuccess);
        Assert.Contains("same chronicle", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SetSireCharacterAsync_AsNonStoryteller_ThrowsUnauthorized()
    {
        var options = CreateOptions(nameof(SetSireCharacterAsync_AsNonStoryteller_ThrowsUnauthorized));
        await using (var ctx = new ApplicationDbContext(options))
        {
            ctx.Campaigns.Add(new Campaign { Id = 1, Name = "C", StoryTellerId = "st" });
            ctx.Clans.Add(new Clan { Id = 1, Name = "Gangrel" });
            ctx.Characters.Add(new Character { Id = 1, Name = "A", ApplicationUserId = "u", ClanId = 1, CampaignId = 1 });
            ctx.Characters.Add(new Character { Id = 2, Name = "B", ApplicationUserId = "u", ClanId = 1, CampaignId = 1 });
            await ctx.SaveChangesAsync();
        }

        var auth = new Mock<IAuthorizationHelper>();
        auth.Setup(a => a.RequireStorytellerAsync(It.IsAny<int>(), "player", It.IsAny<string>()))
            .ThrowsAsync(new UnauthorizedAccessException("denied"));
        var service = CreateService(options, auth.Object);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.SetSireCharacterAsync(1, 2, "player"));
    }

    [Fact]
    public async Task RollBloodSympathyAsync_RelatedSameRange_Succeeds()
    {
        var options = CreateOptions(nameof(RollBloodSympathyAsync_RelatedSameRange_Succeeds));
        await using (var ctx = new ApplicationDbContext(options))
        {
            await SeedTwoPcLineageCampaignAsync(ctx);
        }

        var dice = CreateDiceMock(3);
        var service = CreateService(options, diceService: dice.Object);
        Result<RollResult> result = await service.RollBloodSympathyAsync(2, 1, "u2");

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value!.Successes);
        dice.Verify(d => d.Roll(4, true, false, false, false, null), Times.Once);
    }

    [Fact]
    public async Task RollBloodSympathyAsync_OutOfRange_ReturnsFailure()
    {
        var options = CreateOptions(nameof(RollBloodSympathyAsync_OutOfRange_ReturnsFailure));
        await using (var ctx = new ApplicationDbContext(options))
        {
            ctx.Campaigns.Add(new Campaign { Id = 1, Name = "C", StoryTellerId = "st" });
            ctx.Clans.Add(new Clan { Id = 1, Name = "Gangrel" });
            for (int i = 1; i <= 5; i++)
            {
                ctx.Characters.Add(new Character
                {
                    Id = i,
                    Name = $"V{i}",
                    ApplicationUserId = "u",
                    ClanId = 1,
                    CampaignId = 1,
                    BloodPotency = 2,
                    SireCharacterId = i == 1 ? null : i - 1,
                });
            }

            await ctx.SaveChangesAsync();
        }

        var service = CreateService(options);
        Result<RollResult> result = await service.RollBloodSympathyAsync(5, 1, "u");

        Assert.False(result.IsSuccess);
        Assert.Contains("range", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RollBloodSympathyAsync_Unrelated_ReturnsFailure()
    {
        var options = CreateOptions(nameof(RollBloodSympathyAsync_Unrelated_ReturnsFailure));
        await using (var ctx = new ApplicationDbContext(options))
        {
            ctx.Campaigns.Add(new Campaign { Id = 1, Name = "C", StoryTellerId = "st" });
            ctx.Clans.Add(new Clan { Id = 1, Name = "Gangrel" });
            ctx.Characters.Add(new Character { Id = 1, Name = "A", ApplicationUserId = "u", ClanId = 1, CampaignId = 1, BloodPotency = 4 });
            ctx.Characters.Add(new Character { Id = 2, Name = "B", ApplicationUserId = "u", ClanId = 1, CampaignId = 1, BloodPotency = 4 });
            await ctx.SaveChangesAsync();
        }

        var service = CreateService(options);
        Result<RollResult> result = await service.RollBloodSympathyAsync(1, 2, "u");

        Assert.False(result.IsSuccess);
        Assert.Contains("not connected", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetLineageGraphAsync_ReturnsSireAndChilder()
    {
        var options = CreateOptions(nameof(GetLineageGraphAsync_ReturnsSireAndChilder));
        await using (var ctx = new ApplicationDbContext(options))
        {
            await SeedTwoPcLineageCampaignAsync(ctx);
        }

        var service = CreateService(options);
        LineageGraphDto graph = await service.GetLineageGraphAsync(2, "u2");

        Assert.Equal(2, graph.CharacterId);
        Assert.NotNull(graph.Sire);
        Assert.Equal(1, graph.Sire!.CharacterId);
        Assert.Empty(graph.Childer);

        LineageGraphDto sireView = await service.GetLineageGraphAsync(1, "u1");
        Assert.Null(sireView.Sire);
        Assert.Single(sireView.Childer);
        Assert.Equal(2, sireView.Childer[0].CharacterId);
    }
}
