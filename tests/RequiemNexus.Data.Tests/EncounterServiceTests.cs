using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RequiemNexus.Application.Services;
using RequiemNexus.Data.Models;
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

    private static EncounterService CreateService(ApplicationDbContext ctx) =>
        new(ctx, NullLogger<EncounterService>.Instance, new AuthorizationHelper(ctx, NullLogger<AuthorizationHelper>.Instance));

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
    public async Task CreateEncounterAsync_ThrowsUnauthorized_WhenCallerIsNotStoryteller()
    {
        ApplicationDbContext ctx = CreateContext(nameof(CreateEncounterAsync_ThrowsUnauthorized_WhenCallerIsNotStoryteller));
        Campaign campaign = await SeedCampaignAsync(ctx);
        EncounterService service = CreateService(ctx);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.CreateEncounterAsync(campaign.Id, "Test Encounter", "random-user"));
    }

    [Fact]
    public async Task AddCharacterToEncounterAsync_ThrowsUnauthorized_WhenCallerIsNotStoryteller()
    {
        ApplicationDbContext ctx = CreateContext(nameof(AddCharacterToEncounterAsync_ThrowsUnauthorized_WhenCallerIsNotStoryteller));
        Campaign campaign = await SeedCampaignAsync(ctx);
        EncounterService service = CreateService(ctx);
        CombatEncounter encounter = await service.CreateEncounterAsync(campaign.Id, "Fight", "st-1");
        Character character = await SeedCharacterAsync(ctx, campaign.Id);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.AddCharacterToEncounterAsync(encounter.Id, character.Id, 3, 7, "random-user"));
    }

    [Fact]
    public async Task AdvanceTurnAsync_ThrowsUnauthorized_WhenCallerIsNotStoryteller()
    {
        ApplicationDbContext ctx = CreateContext(nameof(AdvanceTurnAsync_ThrowsUnauthorized_WhenCallerIsNotStoryteller));
        Campaign campaign = await SeedCampaignAsync(ctx);
        EncounterService service = CreateService(ctx);
        CombatEncounter encounter = await service.CreateEncounterAsync(campaign.Id, "Fight", "st-1");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.AdvanceTurnAsync(encounter.Id, "random-user"));
    }

    [Fact]
    public async Task ResolveEncounterAsync_ThrowsUnauthorized_WhenCallerIsNotStoryteller()
    {
        ApplicationDbContext ctx = CreateContext(nameof(ResolveEncounterAsync_ThrowsUnauthorized_WhenCallerIsNotStoryteller));
        Campaign campaign = await SeedCampaignAsync(ctx);
        EncounterService service = CreateService(ctx);
        CombatEncounter encounter = await service.CreateEncounterAsync(campaign.Id, "Fight", "st-1");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.ResolveEncounterAsync(encounter.Id, "random-user"));
    }

    // ── CreateEncounterAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateEncounterAsync_Persists_WhenCallerIsStoryteller()
    {
        ApplicationDbContext ctx = CreateContext(nameof(CreateEncounterAsync_Persists_WhenCallerIsStoryteller));
        Campaign campaign = await SeedCampaignAsync(ctx);
        EncounterService service = CreateService(ctx);

        CombatEncounter encounter = await service.CreateEncounterAsync(campaign.Id, "The Alley Brawl", "st-1");

        CombatEncounter? stored = await ctx.CombatEncounters.FindAsync(encounter.Id);
        Assert.NotNull(stored);
        Assert.Equal("The Alley Brawl", stored.Name);
        Assert.True(stored.IsActive);
        Assert.Null(stored.ResolvedAt);
    }

    // ── Initiative Sorting ───────────────────────────────────────────────────

    [Fact]
    public async Task InitiativeOrder_SortsByTotalDescending()
    {
        ApplicationDbContext ctx = CreateContext(nameof(InitiativeOrder_SortsByTotalDescending));
        Campaign campaign = await SeedCampaignAsync(ctx);
        EncounterService service = CreateService(ctx);
        CombatEncounter encounter = await service.CreateEncounterAsync(campaign.Id, "Fight", "st-1");

        // NPC with Total = 3+5 = 8
        await service.AddNpcToEncounterAsync(encounter.Id, "Guard", 3, 5, "st-1");
        // NPC with Total = 2+3 = 5
        await service.AddNpcToEncounterAsync(encounter.Id, "Rat", 2, 3, "st-1");
        // NPC with Total = 4+8 = 12
        await service.AddNpcToEncounterAsync(encounter.Id, "Boss", 4, 8, "st-1");

        CombatEncounter? loaded = await service.GetEncounterAsync(encounter.Id);
        List<InitiativeEntry> entries = loaded!.InitiativeEntries.OrderBy(i => i.Order).ToList();

        Assert.Equal(3, entries.Count);
        Assert.Equal("Boss", entries[0].NpcName);   // Total 12 — first
        Assert.Equal("Guard", entries[1].NpcName);  // Total 8 — second
        Assert.Equal("Rat", entries[2].NpcName);    // Total 5 — third
    }

    [Fact]
    public async Task InitiativeOrder_TieBreak_HigherModWins()
    {
        ApplicationDbContext ctx = CreateContext(nameof(InitiativeOrder_TieBreak_HigherModWins));
        Campaign campaign = await SeedCampaignAsync(ctx);
        EncounterService service = CreateService(ctx);
        CombatEncounter encounter = await service.CreateEncounterAsync(campaign.Id, "Fight", "st-1");

        // Both total 10, but "Swift" has higher mod (6 > 4)
        await service.AddNpcToEncounterAsync(encounter.Id, "Slow", 4, 6, "st-1");   // mod=4, roll=6, total=10
        await service.AddNpcToEncounterAsync(encounter.Id, "Swift", 6, 4, "st-1");  // mod=6, roll=4, total=10

        CombatEncounter? loaded = await service.GetEncounterAsync(encounter.Id);
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
        CombatEncounter encounter = await service.CreateEncounterAsync(campaign.Id, "Fight", "st-1");
        Character character = await SeedCharacterAsync(ctx, campaign.Id);

        // Both total 10, same mod — PC should come before NPC
        await service.AddNpcToEncounterAsync(encounter.Id, "Guard", 5, 5, "st-1");              // total=10, NPC
        await service.AddCharacterToEncounterAsync(encounter.Id, character.Id, 5, 5, "st-1");   // total=10, PC

        CombatEncounter? loaded = await service.GetEncounterAsync(encounter.Id);
        List<InitiativeEntry> entries = loaded!.InitiativeEntries.OrderBy(i => i.Order).ToList();

        Assert.NotNull(entries[0].CharacterId);  // PC first
        Assert.Null(entries[1].CharacterId);     // NPC second
    }

    // ── AdvanceTurnAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task AdvanceTurnAsync_MarksCurrentActorAsActed()
    {
        ApplicationDbContext ctx = CreateContext(nameof(AdvanceTurnAsync_MarksCurrentActorAsActed));
        Campaign campaign = await SeedCampaignAsync(ctx);
        EncounterService service = CreateService(ctx);
        CombatEncounter encounter = await service.CreateEncounterAsync(campaign.Id, "Fight", "st-1");
        await service.AddNpcToEncounterAsync(encounter.Id, "Guard", 3, 5, "st-1");
        await service.AddNpcToEncounterAsync(encounter.Id, "Boss", 4, 8, "st-1");

        await service.AdvanceTurnAsync(encounter.Id, "st-1");

        List<InitiativeEntry> entries = await ctx.InitiativeEntries
            .Where(i => i.EncounterId == encounter.Id)
            .OrderBy(i => i.Order)
            .ToListAsync();

        // First in order (Order=1) should now have acted; second should not.
        Assert.True(entries[0].HasActed);
        Assert.False(entries[1].HasActed);
    }

    [Fact]
    public async Task AdvanceTurnAsync_ResetsAllEntries_WhenRoundEnds()
    {
        ApplicationDbContext ctx = CreateContext(nameof(AdvanceTurnAsync_ResetsAllEntries_WhenRoundEnds));
        Campaign campaign = await SeedCampaignAsync(ctx);
        EncounterService service = CreateService(ctx);
        CombatEncounter encounter = await service.CreateEncounterAsync(campaign.Id, "Fight", "st-1");
        await service.AddNpcToEncounterAsync(encounter.Id, "A", 3, 5, "st-1");
        await service.AddNpcToEncounterAsync(encounter.Id, "B", 2, 3, "st-1");

        // Advance twice — both participants act, ending the round.
        await service.AdvanceTurnAsync(encounter.Id, "st-1");
        await service.AdvanceTurnAsync(encounter.Id, "st-1");
        // Third advance: all have acted, so the round resets (all → HasActed = false).
        await service.AdvanceTurnAsync(encounter.Id, "st-1");

        List<InitiativeEntry> entries = await ctx.InitiativeEntries
            .Where(i => i.EncounterId == encounter.Id)
            .ToListAsync();

        // After the reset call: no one has acted yet in the new round.
        Assert.All(entries, e => Assert.False(e.HasActed));

        // Fourth advance: the first participant acts in the new round.
        await service.AdvanceTurnAsync(encounter.Id, "st-1");
        // Re-fetch after the fourth advance.
        entries = await ctx.InitiativeEntries
            .Where(i => i.EncounterId == encounter.Id)
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
        CombatEncounter encounter = await service.CreateEncounterAsync(campaign.Id, "Fight", "st-1");

        await service.ResolveEncounterAsync(encounter.Id, "st-1");

        CombatEncounter? stored = await ctx.CombatEncounters.FindAsync(encounter.Id);
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
        CombatEncounter encounter = await service.CreateEncounterAsync(campaign.Id, "Fight", "st-1");
        await service.ResolveEncounterAsync(encounter.Id, "st-1");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ResolveEncounterAsync(encounter.Id, "st-1"));
    }

    // ── GetEncountersAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetEncountersAsync_ReturnsActiveFirst()
    {
        ApplicationDbContext ctx = CreateContext(nameof(GetEncountersAsync_ReturnsActiveFirst));
        Campaign campaign = await SeedCampaignAsync(ctx);
        EncounterService service = CreateService(ctx);

        CombatEncounter first = await service.CreateEncounterAsync(campaign.Id, "Past Fight", "st-1");
        await service.ResolveEncounterAsync(first.Id, "st-1");
        await service.CreateEncounterAsync(campaign.Id, "Current Fight", "st-1");

        List<CombatEncounter> encounters = await service.GetEncountersAsync(campaign.Id);

        Assert.Equal(2, encounters.Count);
        Assert.True(encounters[0].IsActive);   // Active encounter first
        Assert.False(encounters[1].IsActive);  // Resolved encounter second
    }
}
