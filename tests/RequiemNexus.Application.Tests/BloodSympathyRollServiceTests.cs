using System.Diagnostics.Metrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.Observability;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Application.Services;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Contracts;
using RequiemNexus.Domain.Models;
using Xunit;

namespace RequiemNexus.Application.Tests;

/// <summary>
/// Tests for <see cref="BloodSympathyRollService"/> (Phase 18 range guard).
/// </summary>
public class BloodSympathyRollServiceTests
{
    private static readonly Lazy<IMeterFactory> _meterFactory = new(() =>
    {
        var services = new ServiceCollection();
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

    private static BloodSympathyRollService CreateService(
        DbContextOptions<ApplicationDbContext> options,
        IAuthorizationHelper? authHelper = null)
    {
        var auth = authHelper ?? CreatePermissiveAuthMock().Object;
        var traits = new Mock<ITraitResolver>();
        traits.Setup(t => t.ResolvePoolAsync(It.IsAny<Character>(), It.IsAny<PoolDefinition>()))
            .ReturnsAsync(3);
        var dice = new Mock<IDiceService>();
        dice.Setup(d => d.Roll(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int?>()))
            .Returns(new RollResult { Successes = 1 });
        var session = new Mock<ISessionService>();
        session
            .Setup(s => s.PublishDiceRollAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int?>(),
                It.IsAny<string>(),
                It.IsAny<RollResult>()))
            .Returns(Task.CompletedTask);
        return new BloodSympathyRollService(
            new TestApplicationDbContextFactory(options),
            auth,
            traits.Object,
            dice.Object,
            new RelationshipWebMetrics(_meterFactory.Value),
            session.Object,
            NullLogger<BloodSympathyRollService>.Instance);
    }

    private static Mock<IAuthorizationHelper> CreatePermissiveAuthMock()
    {
        var mock = new Mock<IAuthorizationHelper>();
        mock.Setup(a => a.RequireCharacterAccessAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        return mock;
    }

    /// <summary>
    /// Three PCs in a sire chain (degrees 2 apart for ends). BP 2 → rating 1 each → effective range 1; roll across degree 2 must fail.
    /// </summary>
    private static async Task SeedLineageChainAsync(ApplicationDbContext ctx)
    {
        ctx.Campaigns.Add(new Campaign { Id = 1, Name = "C", StoryTellerId = "st" });
        ctx.Clans.Add(new Clan { Id = 1, Name = "Gangrel" });
        ctx.Characters.Add(new Character
        {
            Id = 1,
            Name = "Grandsire",
            ApplicationUserId = "u1",
            ClanId = 1,
            CampaignId = 1,
            BloodPotency = 2,
            SireCharacterId = null,
        });
        ctx.Characters.Add(new Character
        {
            Id = 2,
            Name = "Sire",
            ApplicationUserId = "u2",
            ClanId = 1,
            CampaignId = 1,
            BloodPotency = 2,
            SireCharacterId = 1,
        });
        ctx.Characters.Add(new Character
        {
            Id = 3,
            Name = "Childe",
            ApplicationUserId = "u3",
            ClanId = 1,
            CampaignId = 1,
            BloodPotency = 2,
            SireCharacterId = 2,
        });
        await ctx.SaveChangesAsync();
    }

    [Fact]
    public async Task RollBloodSympathy_TargetBeyondRange_ReturnsFailure()
    {
        string db = nameof(RollBloodSympathy_TargetBeyondRange_ReturnsFailure);
        var options = CreateOptions(db);
        await using var ctx = new ApplicationDbContext(options);
        await SeedLineageChainAsync(ctx);

        BloodSympathyRollService sut = CreateService(options);

        Result<RollResult> result = await sut.RollBloodSympathyAsync(1, 3, "u1");

        Assert.False(result.IsSuccess);
        Assert.Contains("range", result.Error!, StringComparison.OrdinalIgnoreCase);
    }
}
