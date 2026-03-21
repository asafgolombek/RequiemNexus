using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RequiemNexus.Application.Services;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.Models.Enums;
using RequiemNexus.Domain.Enums;
using Xunit;

namespace RequiemNexus.Data.Tests;

/// <summary>
/// Integration tests for the Danse Macabre services wired to an EF Core InMemory database.
/// Each test creates its own named database for full isolation.
/// </summary>
public class DanseMacabreServiceTests
{
    private static ApplicationDbContext CreateContext(string dbName)
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ApplicationDbContext(options);
    }

    private static AuthorizationHelper CreateAuthHelper(string databaseName) =>
        new(InMemoryApplicationDbContextFactories.ForDatabaseName(databaseName), NullLogger<AuthorizationHelper>.Instance);

    private static CityFactionService CreateFactionService(ApplicationDbContext ctx, string databaseName) =>
        new(ctx, NullLogger<CityFactionService>.Instance, CreateAuthHelper(databaseName));

    private static ChronicleNpcService CreateNpcService(ApplicationDbContext ctx, string databaseName) =>
        new(ctx, NullLogger<ChronicleNpcService>.Instance, CreateAuthHelper(databaseName));

    private static FeedingTerritoryService CreateTerritoryService(ApplicationDbContext ctx, string databaseName) =>
        new(ctx, NullLogger<FeedingTerritoryService>.Instance, CreateAuthHelper(databaseName));

    private static FactionRelationshipService CreateRelationshipService(ApplicationDbContext ctx, string databaseName) =>
        new(ctx, NullLogger<FactionRelationshipService>.Instance, CreateAuthHelper(databaseName));

    private static async Task<Campaign> SeedCampaignAsync(ApplicationDbContext ctx, string stId = "st-1")
    {
        Campaign campaign = new() { Name = "Test Saga", StoryTellerId = stId };
        ctx.Campaigns.Add(campaign);
        await ctx.SaveChangesAsync();
        return campaign;
    }

    // ── Authorization ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateFaction_NonSt_ThrowsUnauthorized()
    {
        ApplicationDbContext ctx = CreateContext(nameof(CreateFaction_NonSt_ThrowsUnauthorized));
        Campaign campaign = await SeedCampaignAsync(ctx);
        CityFactionService service = CreateFactionService(ctx, nameof(CreateFaction_NonSt_ThrowsUnauthorized));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.CreateFactionAsync(campaign.Id, "Invictus", FactionType.Covenant, 3, string.Empty, "random-user"));
    }

    [Fact]
    public async Task CreateNpc_NonSt_ThrowsUnauthorized()
    {
        ApplicationDbContext ctx = CreateContext(nameof(CreateNpc_NonSt_ThrowsUnauthorized));
        Campaign campaign = await SeedCampaignAsync(ctx);
        ChronicleNpcService service = CreateNpcService(ctx, nameof(CreateNpc_NonSt_ThrowsUnauthorized));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.CreateNpcAsync(campaign.Id, "Elias Vance", null, null, null, string.Empty, CreatureType.Mortal, "random-user"));
    }

    [Fact]
    public async Task CreateTerritory_NonSt_ThrowsUnauthorized()
    {
        ApplicationDbContext ctx = CreateContext(nameof(CreateTerritory_NonSt_ThrowsUnauthorized));
        Campaign campaign = await SeedCampaignAsync(ctx);
        FeedingTerritoryService service = CreateTerritoryService(ctx, nameof(CreateTerritory_NonSt_ThrowsUnauthorized));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.CreateTerritoryAsync(campaign.Id, "The Warehouse District", string.Empty, 2, null, "random-user"));
    }

    [Fact]
    public async Task SetRelationship_NonSt_ThrowsUnauthorized()
    {
        ApplicationDbContext ctx = CreateContext(nameof(SetRelationship_NonSt_ThrowsUnauthorized));
        Campaign campaign = await SeedCampaignAsync(ctx);
        FactionRelationshipService service = CreateRelationshipService(ctx, nameof(SetRelationship_NonSt_ThrowsUnauthorized));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.SetRelationshipAsync(campaign.Id, 1, 2, FactionStance.Hostile, string.Empty, "random-user"));
    }

    // ── NPC faction assignment ─────────────────────────────────────────────────

    [Fact]
    public async Task UpdateNpc_FactionAssignment_UpdatesPrimaryFactionId()
    {
        ApplicationDbContext ctx = CreateContext(nameof(UpdateNpc_FactionAssignment_UpdatesPrimaryFactionId));
        Campaign campaign = await SeedCampaignAsync(ctx);
        CityFactionService factionService = CreateFactionService(ctx, nameof(UpdateNpc_FactionAssignment_UpdatesPrimaryFactionId));
        ChronicleNpcService npcService = CreateNpcService(ctx, nameof(UpdateNpc_FactionAssignment_UpdatesPrimaryFactionId));

        CityFaction faction = await factionService.CreateFactionAsync(campaign.Id, "Invictus", FactionType.Covenant, 3, string.Empty, "st-1");
        ChronicleNpc npc = await npcService.CreateNpcAsync(campaign.Id, "Elias", null, null, null, string.Empty, CreatureType.Mortal, "st-1");

        await npcService.UpdateNpcAsync(npc.Id, npc.Name, null, faction.Id, "Legate", string.Empty, string.Empty, null, CreatureType.Mortal, "{}", "{}", "st-1");

        ChronicleNpc? loaded = await ctx.ChronicleNpcs.FindAsync(npc.Id);
        Assert.Equal(faction.Id, loaded!.PrimaryFactionId);
        Assert.Equal("Legate", loaded.RoleInFaction);
    }

    // ── Faction relationship ───────────────────────────────────────────────────

    [Fact]
    public async Task SetRelationship_CreatesThenUpdates()
    {
        ApplicationDbContext ctx = CreateContext(nameof(SetRelationship_CreatesThenUpdates));
        Campaign campaign = await SeedCampaignAsync(ctx);
        CityFactionService factionService = CreateFactionService(ctx, nameof(SetRelationship_CreatesThenUpdates));
        FactionRelationshipService relService = CreateRelationshipService(ctx, nameof(SetRelationship_CreatesThenUpdates));

        CityFaction factionA = await factionService.CreateFactionAsync(campaign.Id, "Invictus", FactionType.Covenant, 3, string.Empty, "st-1");
        CityFaction factionB = await factionService.CreateFactionAsync(campaign.Id, "Carthians", FactionType.Covenant, 2, string.Empty, "st-1");

        FactionRelationship rel = await relService.SetRelationshipAsync(campaign.Id, factionA.Id, factionB.Id, FactionStance.Hostile, "Ancient rivalry", "st-1");
        Assert.Equal(FactionStance.Hostile, rel.StanceFromA);

        // Update existing relationship
        FactionRelationship updated = await relService.SetRelationshipAsync(campaign.Id, factionA.Id, factionB.Id, FactionStance.Neutral, "Truce agreed", "st-1");
        Assert.Equal(FactionStance.Neutral, updated.StanceFromA);

        List<FactionRelationship> all = await relService.GetRelationshipsAsync(campaign.Id);
        Assert.Single(all);
    }

    // ── Deceased NPC filtering ─────────────────────────────────────────────────

    [Fact]
    public async Task SetNpcAlive_False_SetsIsAliveFalse()
    {
        ApplicationDbContext ctx = CreateContext(nameof(SetNpcAlive_False_SetsIsAliveFalse));
        Campaign campaign = await SeedCampaignAsync(ctx);
        ChronicleNpcService service = CreateNpcService(ctx, nameof(SetNpcAlive_False_SetsIsAliveFalse));

        ChronicleNpc npc = await service.CreateNpcAsync(campaign.Id, "Elias", null, null, null, string.Empty, CreatureType.Mortal, "st-1");
        await service.SetNpcAliveAsync(npc.Id, false, "st-1");

        ChronicleNpc? loaded = await ctx.ChronicleNpcs.FindAsync(npc.Id);
        Assert.False(loaded!.IsAlive);
    }

    [Fact]
    public async Task GetNpcs_ExcludesDeceased_WhenIncludeDeceasedFalse()
    {
        ApplicationDbContext ctx = CreateContext(nameof(GetNpcs_ExcludesDeceased_WhenIncludeDeceasedFalse));
        Campaign campaign = await SeedCampaignAsync(ctx);
        ChronicleNpcService service = CreateNpcService(ctx, nameof(GetNpcs_ExcludesDeceased_WhenIncludeDeceasedFalse));

        ChronicleNpc alive = await service.CreateNpcAsync(campaign.Id, "Alive NPC", null, null, null, string.Empty, CreatureType.Mortal, "st-1");
        ChronicleNpc deceased = await service.CreateNpcAsync(campaign.Id, "Dead NPC", null, null, null, string.Empty, CreatureType.Mortal, "st-1");
        await service.SetNpcAliveAsync(deceased.Id, false, "st-1");

        List<ChronicleNpc> result = await service.GetNpcsAsync(campaign.Id, includeDeceased: false);
        Assert.Single(result);
        Assert.Equal(alive.Id, result[0].Id);
    }
}
