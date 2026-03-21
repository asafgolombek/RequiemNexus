using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Application.Services;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using Xunit;

namespace RequiemNexus.Data.Tests;

public class CampaignServiceTests
{
    private sealed class MatchingDbContextFactory(DbContextOptions<ApplicationDbContext> options) : IDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext() => new(options);
        public ValueTask<ApplicationDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new ApplicationDbContext(options));
    }

    /// <summary>
    /// Uses the same <see cref="DbContextOptions{ApplicationDbContext}"/> instance for <paramref name="ctx"/> and the factory
    /// so in-memory stores are shared (a new options builder per call creates an isolated store).
    /// </summary>
    private static CampaignService CreateCampaignService(ApplicationDbContext ctx, DbContextOptions<ApplicationDbContext> options)
    {
        var logger = new Mock<ILogger<CampaignService>>().Object;
        var factory = new MatchingDbContextFactory(options);
        var authHelper = new AuthorizationHelper(factory, NullLogger<AuthorizationHelper>.Instance);

        return new CampaignService(ctx, factory, logger, authHelper, new Mock<ISessionService>().Object);
    }

    private static CharacterManagementService CreateCharacterService(ApplicationDbContext ctx, string databaseName)
    {
        IDbContextFactory<ApplicationDbContext> factory = InMemoryApplicationDbContextFactories.ForDatabaseName(databaseName);
        var auth = new AuthorizationHelper(factory, NullLogger<AuthorizationHelper>.Instance);
        return new CharacterManagementService(ctx, factory, new RequiemNexus.Domain.CharacterCreationRules(), new BeatLedgerService(ctx), auth, new Mock<ISessionService>().Object);
    }

    private static DbContextOptions<ApplicationDbContext> CreateOptions(string dbName) =>
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

    private static ApplicationDbContext CreateContext(DbContextOptions<ApplicationDbContext> options) =>
        new ApplicationDbContext(options);

    [Fact]
    public async Task CreateCampaignAsync_SetsFieldsCorrectly()
    {
        // Arrange
        string dbName = nameof(CreateCampaignAsync_SetsFieldsCorrectly);
        DbContextOptions<ApplicationDbContext> options = CreateOptions(dbName);
        using var ctx = CreateContext(options);
        var service = CreateCampaignService(ctx, options);

        // Act
        var campaign = await service.CreateCampaignAsync("New Campaign", "Description", "st-user");

        // Assert
        Assert.Equal("New Campaign", campaign.Name);
        Assert.Equal("Description", campaign.Description);
        Assert.Equal("st-user", campaign.StoryTellerId);
        Assert.True(campaign.IsActive);
    }

    [Fact]
    public async Task EnrollCharacterAsync_UpdatesCharacter()
    {
        // Arrange
        string dbName = nameof(EnrollCharacterAsync_UpdatesCharacter);
        DbContextOptions<ApplicationDbContext> options = CreateOptions(dbName);
        using var ctx = CreateContext(options);
        var service = CreateCampaignService(ctx, options);

        var campaign = new Campaign { Name = "Test", StoryTellerId = "st" };
        ctx.Campaigns.Add(campaign);

        var character = new Character { Name = "Vamp", ApplicationUserId = "user" };
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        // Act
        await service.AddCharacterToCampaignAsync(campaign.Id, character.Id, "user");

        // Assert
        Assert.Equal(campaign.Id, character.CampaignId);
    }

    [Fact]
    public async Task RemoveCharacterAsync_ClearsCampaignId()
    {
        // Arrange
        string dbName = nameof(RemoveCharacterAsync_ClearsCampaignId);
        DbContextOptions<ApplicationDbContext> options = CreateOptions(dbName);
        using var ctx = CreateContext(options);
        var service = CreateCampaignService(ctx, options);

        var campaign = new Campaign { Name = "Test", StoryTellerId = "st" };
        ctx.Campaigns.Add(campaign);

        var character = new Character { Name = "Vamp", ApplicationUserId = "user", CampaignId = 1 };
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        // Act
        await service.RemoveCharacterFromCampaignAsync(campaign.Id, character.Id, "st");

        // Assert
        Assert.Null(character.CampaignId);
    }

    [Fact]
    public async Task InMemory_store_is_shared_when_options_instance_is_shared()
    {
        string dbName = nameof(InMemory_store_is_shared_when_options_instance_is_shared);
        DbContextOptions<ApplicationDbContext> options = CreateOptions(dbName);
        using var writer = new ApplicationDbContext(options);
        var factory = new MatchingDbContextFactory(options);
        writer.Campaigns.Add(new Campaign { Name = "X", StoryTellerId = "st" });
        await writer.SaveChangesAsync();

        await using ApplicationDbContext reader = factory.CreateDbContext();
        Assert.Equal(1, await reader.Campaigns.CountAsync());
    }

    [Fact]
    public async Task GetCampaignByIdAsync_ReturnsNull_WhenUserIsNotMember()
    {
        string dbName = nameof(GetCampaignByIdAsync_ReturnsNull_WhenUserIsNotMember);
        DbContextOptions<ApplicationDbContext> options = CreateOptions(dbName);
        using var ctx = CreateContext(options);
        var service = CreateCampaignService(ctx, options);

        var campaign = new Campaign { Name = "Private", StoryTellerId = "st-user" };
        ctx.Campaigns.Add(campaign);
        var enrolled = new Character { Name = "PC", ApplicationUserId = "player-a" };
        ctx.Characters.Add(enrolled);
        await ctx.SaveChangesAsync();

        await service.AddCharacterToCampaignAsync(campaign.Id, enrolled.Id, "player-a");

        Campaign? memberView = await service.GetCampaignByIdAsync(campaign.Id, "player-a");
        Assert.NotNull(memberView);

        Campaign? outsider = await service.GetCampaignByIdAsync(campaign.Id, "someone-else");
        Assert.Null(outsider);
    }

    [Fact]
    public async Task GetCampaignByIdAsync_ReturnsCampaign_WhenUserIsStoryteller()
    {
        string dbName = nameof(GetCampaignByIdAsync_ReturnsCampaign_WhenUserIsStoryteller);
        DbContextOptions<ApplicationDbContext> options = CreateOptions(dbName);
        var service = CreateCampaignService(new ApplicationDbContext(options), options);

        int campaignId;
        await using (ApplicationDbContext seed = new(options))
        {
            var campaign = new Campaign { Name = "Saga", StoryTellerId = "st-user" };
            seed.Campaigns.Add(campaign);
            await seed.SaveChangesAsync();
            campaignId = campaign.Id;
        }

        Campaign? loaded = await service.GetCampaignByIdAsync(campaignId, "st-user");
        Assert.NotNull(loaded);
        Assert.Equal("Saga", loaded!.Name);
    }

    [Fact]
    public async Task GetCampaignByIdAsync_ReturnsCampaign_WhenUserOwnsCharacterInCampaign()
    {
        string dbName = nameof(GetCampaignByIdAsync_ReturnsCampaign_WhenUserOwnsCharacterInCampaign);
        DbContextOptions<ApplicationDbContext> options = CreateOptions(dbName);
        using var ctx = CreateContext(options);
        var service = CreateCampaignService(ctx, options);

        var campaign = new Campaign { Name = "Shared", StoryTellerId = "st-user" };
        ctx.Campaigns.Add(campaign);
        var character = new Character { Name = "PC", ApplicationUserId = "player-a" };
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        await service.AddCharacterToCampaignAsync(campaign.Id, character.Id, "player-a");

        Campaign? loaded = await service.GetCampaignByIdAsync(campaign.Id, "player-a");
        Assert.NotNull(loaded);
        Assert.Single(loaded.Characters);
    }
}
