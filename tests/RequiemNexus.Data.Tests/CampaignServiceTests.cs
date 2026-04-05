using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.Events;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Application.Security;
using RequiemNexus.Application.Services;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Contracts;
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
        var sessionMock = new Mock<ISessionService>().Object;
        return new CampaignService(
            ctx,
            factory,
            logger,
            authHelper,
            new CampaignLoreService(ctx, sessionMock, NullLogger<CampaignLoreService>.Instance),
            new CampaignSessionPrepService(ctx, authHelper, NullLogger<CampaignSessionPrepService>.Instance));
    }

    private static CharacterManagementService CreateCharacterService(ApplicationDbContext ctx, string databaseName)
    {
        IDbContextFactory<ApplicationDbContext> factory = InMemoryApplicationDbContextFactories.ForDatabaseName(databaseName);
        var auth = new AuthorizationHelper(factory, NullLogger<AuthorizationHelper>.Instance);
        IReferenceDataCache referenceCache = ReferenceDataCacheTestDoubles.EmptyButInitialized();
        var humanity = new HumanityService(
            ctx,
            Mock.Of<IAuthorizationHelper>(),
            Mock.Of<IDomainEventDispatcher>(),
            Mock.Of<IDiceService>(),
            Mock.Of<ISessionService>(),
            Mock.Of<IConditionService>(),
            referenceCache,
            NullLogger<HumanityService>.Instance);
        var characterQuery = new CharacterQueryService(ctx, factory);
        var session = new Mock<ISessionService>().Object;
        var creationRules = new RequiemNexus.Domain.Services.CharacterCreationRules();
        var beatLedger = new BeatLedgerService(ctx);
        var progression = new CharacterProgressionService(ctx, creationRules, beatLedger, auth, session);
        return new CharacterManagementService(
            ctx,
            creationRules,
            auth,
            session,
            new CharacterCreationService(),
            humanity,
            referenceCache,
            characterQuery,
            progression);
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

        var character = new Character { Name = "Vamp", ApplicationUserId = "st" };
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        // Act — Storyteller may enroll their own character without a prior roster entry.
        await service.AddCharacterToCampaignAsync(campaign.Id, character.Id, "st");

        // Assert
        Assert.Equal(campaign.Id, character.CampaignId);
    }

    [Fact]
    public async Task AddCharacterToCampaignAsync_RejectsPlayerWhoIsNotAlreadyMember()
    {
        string dbName = nameof(AddCharacterToCampaignAsync_RejectsPlayerWhoIsNotAlreadyMember);
        DbContextOptions<ApplicationDbContext> options = CreateOptions(dbName);
        using var ctx = CreateContext(options);
        var service = CreateCampaignService(ctx, options);

        var campaign = new Campaign { Name = "Test", StoryTellerId = "st" };
        ctx.Campaigns.Add(campaign);
        var character = new Character { Name = "Vamp", ApplicationUserId = "player" };
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.AddCharacterToCampaignAsync(campaign.Id, character.Id, "player"));
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
        campaign.InviteTokenHash = CampaignInviteTokenHasher.Hash("join-secret");
        await ctx.SaveChangesAsync();

        await service.JoinCampaignWithInviteAsync(campaign.Id, enrolled.Id, "join-secret", "player-a");

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
        campaign.InviteTokenHash = CampaignInviteTokenHasher.Hash("tok");
        await ctx.SaveChangesAsync();

        await service.JoinCampaignWithInviteAsync(campaign.Id, character.Id, "tok", "player-a");

        Campaign? loaded = await service.GetCampaignByIdAsync(campaign.Id, "player-a");
        Assert.NotNull(loaded);
        Assert.Single(loaded.Characters);
    }

    [Fact]
    public async Task GetJoinPreviewAsync_ReturnsNull_WhenInviteTokenWrong()
    {
        string dbName = nameof(GetJoinPreviewAsync_ReturnsNull_WhenInviteTokenWrong);
        DbContextOptions<ApplicationDbContext> options = CreateOptions(dbName);
        using var ctx = CreateContext(options);
        var service = CreateCampaignService(ctx, options);

        var campaign = new Campaign { Name = "X", StoryTellerId = "st" };
        ctx.Campaigns.Add(campaign);
        campaign.InviteTokenHash = CampaignInviteTokenHasher.Hash("good");
        await ctx.SaveChangesAsync();

        Assert.Null(await service.GetJoinPreviewAsync(campaign.Id, "bad", "any-user"));
    }

    [Fact]
    public async Task GetJoinPreviewAsync_ReturnsDto_WhenInviteTokenValid()
    {
        string dbName = nameof(GetJoinPreviewAsync_ReturnsDto_WhenInviteTokenValid);
        DbContextOptions<ApplicationDbContext> options = CreateOptions(dbName);
        using var ctx = CreateContext(options);
        var service = CreateCampaignService(ctx, options);

        var campaign = new Campaign { Name = "Chronicle", StoryTellerId = "st" };
        ctx.Campaigns.Add(campaign);
        campaign.InviteTokenHash = CampaignInviteTokenHasher.Hash("tok");
        await ctx.SaveChangesAsync();

        var dto = await service.GetJoinPreviewAsync(campaign.Id, "tok", "player");
        Assert.NotNull(dto);
        Assert.Equal(campaign.Id, dto!.CampaignId);
        Assert.Equal("Chronicle", dto.Name);
    }

    [Fact]
    public async Task RegenerateJoinInviteAsync_ReplacesStoredHash()
    {
        string dbName = nameof(RegenerateJoinInviteAsync_ReplacesStoredHash);
        DbContextOptions<ApplicationDbContext> options = CreateOptions(dbName);
        using var ctx = CreateContext(options);
        var service = CreateCampaignService(ctx, options);

        var campaign = new Campaign { Name = "Y", StoryTellerId = "st" };
        ctx.Campaigns.Add(campaign);
        await ctx.SaveChangesAsync();

        string first = await service.RegenerateJoinInviteAsync(campaign.Id, "st");
        string second = await service.RegenerateJoinInviteAsync(campaign.Id, "st");

        Assert.NotEqual(first, second);
        Campaign reloaded = await ctx.Campaigns.AsNoTracking().SingleAsync(c => c.Id == campaign.Id);
        Assert.True(CampaignInviteTokenHasher.Verify(reloaded.InviteTokenHash, second));
        Assert.False(CampaignInviteTokenHasher.Verify(reloaded.InviteTokenHash, first));
    }

    [Fact]
    public async Task PlayerAlreadyInCampaign_MayAddSecondUnassignedCharacter()
    {
        string dbName = nameof(PlayerAlreadyInCampaign_MayAddSecondUnassignedCharacter);
        DbContextOptions<ApplicationDbContext> options = CreateOptions(dbName);
        using var ctx = CreateContext(options);
        var service = CreateCampaignService(ctx, options);

        var campaign = new Campaign { Name = "Z", StoryTellerId = "st" };
        var first = new Character { Name = "A", ApplicationUserId = "p" };
        var second = new Character { Name = "B", ApplicationUserId = "p" };
        ctx.Campaigns.Add(campaign);
        ctx.Characters.AddRange(first, second);
        campaign.InviteTokenHash = CampaignInviteTokenHasher.Hash("inv");
        await ctx.SaveChangesAsync();

        await service.JoinCampaignWithInviteAsync(campaign.Id, first.Id, "inv", "p");
        await service.AddCharacterToCampaignAsync(campaign.Id, second.Id, "p");

        Assert.Equal(campaign.Id, second.CampaignId);
    }

    [Fact]
    public async Task SetDiscordWebhookUrlAsync_StorytellerMaySetAndClear()
    {
        string dbName = nameof(SetDiscordWebhookUrlAsync_StorytellerMaySetAndClear);
        DbContextOptions<ApplicationDbContext> options = CreateOptions(dbName);
        using var ctx = CreateContext(options);
        var service = CreateCampaignService(ctx, options);

        var campaign = new Campaign { Name = "Hook", StoryTellerId = "st" };
        ctx.Campaigns.Add(campaign);
        await ctx.SaveChangesAsync();

        const string url = "https://discord.com/api/webhooks/1/abcdefgh";
        await service.SetDiscordWebhookUrlAsync(campaign.Id, url, "st");

        Campaign? reloaded = await ctx.Campaigns.AsNoTracking().SingleAsync(c => c.Id == campaign.Id);
        Assert.Equal(url, reloaded.DiscordWebhookUrl);

        await service.SetDiscordWebhookUrlAsync(campaign.Id, null, "st");
        reloaded = await ctx.Campaigns.AsNoTracking().SingleAsync(c => c.Id == campaign.Id);
        Assert.Null(reloaded.DiscordWebhookUrl);
    }

    [Fact]
    public async Task SetDiscordWebhookUrlAsync_RejectsNonStoryteller()
    {
        string dbName = nameof(SetDiscordWebhookUrlAsync_RejectsNonStoryteller);
        DbContextOptions<ApplicationDbContext> options = CreateOptions(dbName);
        using var ctx = CreateContext(options);
        var service = CreateCampaignService(ctx, options);

        var campaign = new Campaign { Name = "X", StoryTellerId = "st" };
        ctx.Campaigns.Add(campaign);
        await ctx.SaveChangesAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.SetDiscordWebhookUrlAsync(
                campaign.Id,
                "https://discord.com/api/webhooks/1/tok",
                "not-st"));
    }

    [Fact]
    public async Task SetDiscordWebhookUrlAsync_RejectsInvalidUrl()
    {
        string dbName = nameof(SetDiscordWebhookUrlAsync_RejectsInvalidUrl);
        DbContextOptions<ApplicationDbContext> options = CreateOptions(dbName);
        using var ctx = CreateContext(options);
        var service = CreateCampaignService(ctx, options);

        var campaign = new Campaign { Name = "X", StoryTellerId = "st" };
        ctx.Campaigns.Add(campaign);
        await ctx.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SetDiscordWebhookUrlAsync(campaign.Id, "https://example.com/hook", "st"));
    }
}
