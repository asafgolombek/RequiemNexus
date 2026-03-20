using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Application.Services;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.RealTime;
using Xunit;

namespace RequiemNexus.Data.Tests;

/// <summary>
/// Integration tests for <see cref="EncounterService"/> wired to an EF Core InMemory database.
/// Each test creates its own named database for full isolation.
/// </summary>
public class EncounterServiceTests
{
    private static ApplicationDbContext CreateContext(string dbName)
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ApplicationDbContext(options);
    }

    private static EncounterService CreateService(ApplicationDbContext ctx, Mock<ISessionService>? sessionMock = null)
    {
        Mock<ISessionService> mock = sessionMock ?? new Mock<ISessionService>();
        mock
            .Setup(s => s.UpdateInitiativeAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<IEnumerable<InitiativeEntryDto>>()))
            .Returns(Task.CompletedTask);

        return new EncounterService(
            ctx,
            NullLogger<EncounterService>.Instance,
            new AuthorizationHelper(ctx, NullLogger<AuthorizationHelper>.Instance),
            mock.Object);
    }

    /// <summary>
    /// Creates a draft, launches it (no NPC templates), and returns the encounter id for active-combat operations.
    /// </summary>
    private static async Task<int> CreateLaunchedEmptyEncounterAsync(EncounterService service, int campaignId, string stId)
    {
        CombatEncounter draft = await service.CreateDraftEncounterAsync(campaignId, "Fight", stId);
        await service.LaunchEncounterAsync(draft.Id, stId);
        return draft.Id;
    }

    private static async Task<Campaign> SeedCampaignAsync(
        ApplicationDbContext ctx,
        string stId = "st-1")
    {
        Campaign campaign = new() { Name = "Test Saga", StoryTellerId = stId };
        ctx.Campaigns.Add(campaign);
        await ctx.SaveChangesAsync();
        return campaign;
    }

    private static async Task<Character> SeedCharacterAsync(
        ApplicationDbContext ctx,
        int campaignId,
        string playerId = "player-1",
        string name = "Varian")
    {
        Character character = new()
        {
            Name = name,
            ApplicationUserId = playerId,
            CampaignId = campaignId,
        };
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();
        return character;
    }

    // ── Authorization ────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateDraftEncounterAsync_ThrowsUnauthorized_WhenCallerIsNotStoryteller()
    {
        ApplicationDbContext ctx = CreateContext(nameof(CreateDraftEncounterAsync_ThrowsUnauthorized_WhenCallerIsNotStoryteller));
        Campaign campaign = await SeedCampaignAsync(ctx);
        EncounterService service = CreateService(ctx);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.CreateDraftEncounterAsync(campaign.Id, "Test Encounter", "random-user"));
    }

    [Fact]
    public async Task AddCharacterToEncounterAsync_ThrowsUnauthorized_WhenCallerIsNotStoryteller()
    {
        ApplicationDbContext ctx = CreateContext(nameof(AddCharacterToEncounterAsync_ThrowsUnauthorized_WhenCallerIsNotStoryteller));
        Campaign campaign = await SeedCampaignAsync(ctx);
        EncounterService service = CreateService(ctx);
        int encounterId = await CreateLaunchedEmptyEncounterAsync(service, campaign.Id, "st-1");
        Character character = await SeedCharacterAsync(ctx, campaign.Id);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.AddCharacterToEncounterAsync(encounterId, character.Id, 3, 7, "random-user"));
    }

    [Fact]
    public async Task AdvanceTurnAsync_ThrowsUnauthorized_WhenCallerIsNotStoryteller()
    {
        ApplicationDbContext ctx = CreateContext(nameof(AdvanceTurnAsync_ThrowsUnauthorized_WhenCallerIsNotStoryteller));
        Campaign campaign = await SeedCampaignAsync(ctx);
        EncounterService service = CreateService(ctx);
        int encounterId = await CreateLaunchedEmptyEncounterAsync(service, campaign.Id, "st-1");
        await service.AddNpcToEncounterAsync(encounterId, "Guard", 3, 5, "st-1");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.AdvanceTurnAsync(encounterId, "random-user"));
    }

    [Fact]
    public async Task ResolveEncounterAsync_ThrowsUnauthorized_WhenCallerIsNotStoryteller()
    {
        ApplicationDbContext ctx = CreateContext(nameof(ResolveEncounterAsync_ThrowsUnauthorized_WhenCallerIsNotStoryteller));
        Campaign campaign = await SeedCampaignAsync(ctx);
        EncounterService service = CreateService(ctx);
        int encounterId = await CreateLaunchedEmptyEncounterAsync(service, campaign.Id, "st-1");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.ResolveEncounterAsync(encounterId, "random-user"));
    }

    // ── CreateDraftEncounterAsync ────────────────────────────────────────────

    [Fact]
    public async Task CreateDraftEncounterAsync_Persists_WhenCallerIsStoryteller()
    {
        ApplicationDbContext ctx = CreateContext(nameof(CreateDraftEncounterAsync_Persists_WhenCallerIsStoryteller));
        Campaign campaign = await SeedCampaignAsync(ctx);
        EncounterService service = CreateService(ctx);

        CombatEncounter encounter = await service.CreateDraftEncounterAsync(campaign.Id, "The Alley Brawl", "st-1");

        CombatEncounter? stored = await ctx.CombatEncounters.FindAsync(encounter.Id);
        Assert.NotNull(stored);
        Assert.Equal("The Alley Brawl", stored.Name);
        Assert.True(stored.IsDraft);
        Assert.False(stored.IsActive);
        Assert.False(stored.IsPaused);
        Assert.Null(stored.ResolvedAt);
    }

    // ── Initiative Sorting ───────────────────────────────────────────────────

    [Fact]
    public async Task InitiativeOrder_SortsByTotalDescending()
    {
        ApplicationDbContext ctx = CreateContext(nameof(InitiativeOrder_SortsByTotalDescending));
        Campaign campaign = await SeedCampaignAsync(ctx);
        EncounterService service = CreateService(ctx);
        int encounterId = await CreateLaunchedEmptyEncounterAsync(service, campaign.Id, "st-1");

        await service.AddNpcToEncounterAsync(encounterId, "Guard", 3, 5, "st-1");
        await service.AddNpcToEncounterAsync(encounterId, "Rat", 2, 3, "st-1");
        await service.AddNpcToEncounterAsync(encounterId, "Boss", 4, 8, "st-1");

        CombatEncounter? loaded = await service.GetEncounterAsync(encounterId, "st-1");
        List<InitiativeEntry> entries = loaded!.InitiativeEntries.OrderBy(i => i.Order).ToList();

        Assert.Equal(3, entries.Count);
        Assert.Equal("Boss", entries[0].NpcName);
        Assert.Equal("Guard", entries[1].NpcName);
        Assert.Equal("Rat", entries[2].NpcName);
    }

    [Fact]
    public async Task InitiativeOrder_TieBreak_HigherModWins()
    {
        ApplicationDbContext ctx = CreateContext(nameof(InitiativeOrder_TieBreak_HigherModWins));
        Campaign campaign = await SeedCampaignAsync(ctx);
        EncounterService service = CreateService(ctx);
        int encounterId = await CreateLaunchedEmptyEncounterAsync(service, campaign.Id, "st-1");

        await service.AddNpcToEncounterAsync(encounterId, "Slow", 4, 6, "st-1");
        await service.AddNpcToEncounterAsync(encounterId, "Swift", 6, 4, "st-1");

        CombatEncounter? loaded = await service.GetEncounterAsync(encounterId, "st-1");
        List<InitiativeEntry> entries = loaded!.InitiativeEntries.OrderBy(i => i.Order).ToList();

        Assert.Equal("Swift", entries[0].NpcName);
        Assert.Equal("Slow", entries[1].NpcName);
    }

    [Fact]
    public async Task InitiativeOrder_TieBreak_PlayerCharacterBeforeNpc()
    {
        ApplicationDbContext ctx = CreateContext(nameof(InitiativeOrder_TieBreak_PlayerCharacterBeforeNpc));
        Campaign campaign = await SeedCampaignAsync(ctx);
        EncounterService service = CreateService(ctx);
        int encounterId = await CreateLaunchedEmptyEncounterAsync(service, campaign.Id, "st-1");
        Character character = await SeedCharacterAsync(ctx, campaign.Id);

        await service.AddNpcToEncounterAsync(encounterId, "Guard", 5, 5, "st-1");
        await service.AddCharacterToEncounterAsync(encounterId, character.Id, 5, 5, "st-1");

        CombatEncounter? loaded = await service.GetEncounterAsync(encounterId, "st-1");
        List<InitiativeEntry> entries = loaded!.InitiativeEntries.OrderBy(i => i.Order).ToList();

        Assert.NotNull(entries[0].CharacterId);
        Assert.Null(entries[1].CharacterId);
    }

    // ── AdvanceTurnAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task AdvanceTurnAsync_MarksCurrentActorAsActed()
    {
        ApplicationDbContext ctx = CreateContext(nameof(AdvanceTurnAsync_MarksCurrentActorAsActed));
        Campaign campaign = await SeedCampaignAsync(ctx);
        EncounterService service = CreateService(ctx);
        int encounterId = await CreateLaunchedEmptyEncounterAsync(service, campaign.Id, "st-1");
        await service.AddNpcToEncounterAsync(encounterId, "Guard", 3, 5, "st-1");
        await service.AddNpcToEncounterAsync(encounterId, "Boss", 4, 8, "st-1");

        await service.AdvanceTurnAsync(encounterId, "st-1");

        List<InitiativeEntry> entries = await ctx.InitiativeEntries
            .Where(i => i.EncounterId == encounterId)
            .OrderBy(i => i.Order)
            .ToListAsync();

        Assert.True(entries[0].HasActed);
        Assert.False(entries[1].HasActed);
    }

    [Fact]
    public async Task AdvanceTurnAsync_ResetsAllEntries_WhenRoundEnds()
    {
        ApplicationDbContext ctx = CreateContext(nameof(AdvanceTurnAsync_ResetsAllEntries_WhenRoundEnds));
        Campaign campaign = await SeedCampaignAsync(ctx);
        EncounterService service = CreateService(ctx);
        int encounterId = await CreateLaunchedEmptyEncounterAsync(service, campaign.Id, "st-1");
        await service.AddNpcToEncounterAsync(encounterId, "A", 3, 5, "st-1");
        await service.AddNpcToEncounterAsync(encounterId, "B", 2, 3, "st-1");

        await service.AdvanceTurnAsync(encounterId, "st-1");
        await service.AdvanceTurnAsync(encounterId, "st-1");
        await service.AdvanceTurnAsync(encounterId, "st-1");

        List<InitiativeEntry> entries = await ctx.InitiativeEntries
            .Where(i => i.EncounterId == encounterId)
            .ToListAsync();

        Assert.All(entries, e => Assert.False(e.HasActed));

        await service.AdvanceTurnAsync(encounterId, "st-1");
        entries = await ctx.InitiativeEntries
            .Where(i => i.EncounterId == encounterId)
            .ToListAsync();

        Assert.Equal(1, entries.Count(e => e.HasActed));
        Assert.Equal(1, entries.Count(e => !e.HasActed));
    }

    // ── ResolveEncounterAsync ────────────────────────────────────────────────

    [Fact]
    public async Task ResolveEncounterAsync_SetsIsActiveFalse()
    {
        ApplicationDbContext ctx = CreateContext(nameof(ResolveEncounterAsync_SetsIsActiveFalse));
        Campaign campaign = await SeedCampaignAsync(ctx);
        EncounterService service = CreateService(ctx);
        int encounterId = await CreateLaunchedEmptyEncounterAsync(service, campaign.Id, "st-1");

        await service.ResolveEncounterAsync(encounterId, "st-1");

        CombatEncounter? stored = await ctx.CombatEncounters.FindAsync(encounterId);
        Assert.NotNull(stored);
        Assert.False(stored.IsActive);
        Assert.NotNull(stored.ResolvedAt);
    }

    [Fact]
    public async Task ResolveEncounterAsync_ThrowsInvalidOperation_WhenAlreadyResolved()
    {
        ApplicationDbContext ctx = CreateContext(nameof(ResolveEncounterAsync_ThrowsInvalidOperation_WhenAlreadyResolved));
        Campaign campaign = await SeedCampaignAsync(ctx);
        EncounterService service = CreateService(ctx);
        int encounterId = await CreateLaunchedEmptyEncounterAsync(service, campaign.Id, "st-1");
        await service.ResolveEncounterAsync(encounterId, "st-1");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ResolveEncounterAsync(encounterId, "st-1"));
    }

    // ── GetEncountersAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetEncountersAsync_ReturnsActiveFirst()
    {
        ApplicationDbContext ctx = CreateContext(nameof(GetEncountersAsync_ReturnsActiveFirst));
        Campaign campaign = await SeedCampaignAsync(ctx);
        EncounterService service = CreateService(ctx);

        CombatEncounter pastDraft = await service.CreateDraftEncounterAsync(campaign.Id, "Past Fight", "st-1");
        await service.LaunchEncounterAsync(pastDraft.Id, "st-1");
        await service.ResolveEncounterAsync(pastDraft.Id, "st-1");

        CombatEncounter currentDraft = await service.CreateDraftEncounterAsync(campaign.Id, "Current Fight", "st-1");
        await service.LaunchEncounterAsync(currentDraft.Id, "st-1");

        List<CombatEncounter> encounters = await service.GetEncountersAsync(campaign.Id, "st-1");

        Assert.Equal(2, encounters.Count);
        Assert.True(encounters[0].IsActive && !encounters[0].IsDraft);
        Assert.False(encounters[1].IsActive);
    }

    // ── Pause / Resume ─────────────────────────────────────────────────────

    [Fact]
    public async Task PauseEncounterAsync_SetsPaused_And_ClearsActive()
    {
        ApplicationDbContext ctx = CreateContext(nameof(PauseEncounterAsync_SetsPaused_And_ClearsActive));
        Campaign campaign = await SeedCampaignAsync(ctx);
        EncounterService service = CreateService(ctx);
        int encounterId = await CreateLaunchedEmptyEncounterAsync(service, campaign.Id, "st-1");

        await service.PauseEncounterAsync(encounterId, "st-1");

        CombatEncounter? stored = await ctx.CombatEncounters.FindAsync(encounterId);
        Assert.NotNull(stored);
        Assert.False(stored.IsActive);
        Assert.True(stored.IsPaused);
        Assert.Null(stored.ResolvedAt);
    }

    [Fact]
    public async Task ResumeEncounterAsync_RestoresActive_FromPaused()
    {
        ApplicationDbContext ctx = CreateContext(nameof(ResumeEncounterAsync_RestoresActive_FromPaused));
        Campaign campaign = await SeedCampaignAsync(ctx);
        EncounterService service = CreateService(ctx);
        int encounterId = await CreateLaunchedEmptyEncounterAsync(service, campaign.Id, "st-1");

        await service.PauseEncounterAsync(encounterId, "st-1");
        await service.ResumeEncounterAsync(encounterId, "st-1");

        CombatEncounter? stored = await ctx.CombatEncounters.FindAsync(encounterId);
        Assert.NotNull(stored);
        Assert.True(stored.IsActive);
        Assert.False(stored.IsPaused);
    }

    [Fact]
    public async Task LaunchEncounterAsync_Throws_WhenAnotherEncounterIsPaused()
    {
        ApplicationDbContext ctx = CreateContext(nameof(LaunchEncounterAsync_Throws_WhenAnotherEncounterIsPaused));
        Campaign campaign = await SeedCampaignAsync(ctx);
        EncounterService service = CreateService(ctx);

        CombatEncounter first = await service.CreateDraftEncounterAsync(campaign.Id, "First", "st-1");
        await service.LaunchEncounterAsync(first.Id, "st-1");
        await service.PauseEncounterAsync(first.Id, "st-1");

        CombatEncounter second = await service.CreateDraftEncounterAsync(campaign.Id, "Second", "st-1");

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.LaunchEncounterAsync(second.Id, "st-1"));
    }

    [Fact]
    public async Task ResolveEncounterAsync_WorksFromPausedState()
    {
        ApplicationDbContext ctx = CreateContext(nameof(ResolveEncounterAsync_WorksFromPausedState));
        Campaign campaign = await SeedCampaignAsync(ctx);
        EncounterService service = CreateService(ctx);
        int encounterId = await CreateLaunchedEmptyEncounterAsync(service, campaign.Id, "st-1");

        await service.PauseEncounterAsync(encounterId, "st-1");
        await service.ResolveEncounterAsync(encounterId, "st-1");

        CombatEncounter? stored = await ctx.CombatEncounters.FindAsync(encounterId);
        Assert.NotNull(stored);
        Assert.NotNull(stored.ResolvedAt);
        Assert.False(stored.IsActive);
        Assert.False(stored.IsPaused);
    }

    [Fact]
    public async Task ResumeEncounterAsync_Throws_WhenAlreadyResolved()
    {
        ApplicationDbContext ctx = CreateContext(nameof(ResumeEncounterAsync_Throws_WhenAlreadyResolved));
        Campaign campaign = await SeedCampaignAsync(ctx);
        EncounterService service = CreateService(ctx);
        int encounterId = await CreateLaunchedEmptyEncounterAsync(service, campaign.Id, "st-1");

        await service.PauseEncounterAsync(encounterId, "st-1");
        await service.ResolveEncounterAsync(encounterId, "st-1");

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ResumeEncounterAsync(encounterId, "st-1"));
    }
}
