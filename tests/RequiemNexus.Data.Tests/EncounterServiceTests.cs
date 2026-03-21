using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.DTOs;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Application.Services;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.Models.Enums;
using RequiemNexus.Data.RealTime;
using RequiemNexus.Domain;
using RequiemNexus.Domain.Contracts;
using RequiemNexus.Domain.Models;
using Xunit;

namespace RequiemNexus.Data.Tests;

/// <summary>
/// Integration tests for <see cref="EncounterService"/> wired to an EF Core InMemory database.
/// Each test creates its own named database for full isolation.
/// </summary>
public class EncounterServiceTests
{
    private static DbContextOptions<ApplicationDbContext> CreateOptions(string dbName)
    {
        var root = new InMemoryDatabaseRoot();
        return new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName, root)
            .Options;
    }

    private static (ApplicationDbContext Ctx, DbContextOptions<ApplicationDbContext> Options) CreateContext(string dbName)
    {
        DbContextOptions<ApplicationDbContext> options = CreateOptions(dbName);
        return (new ApplicationDbContext(options), options);
    }

    private sealed class TestApplicationDbContextFactory : IDbContextFactory<ApplicationDbContext>
    {
        private readonly DbContextOptions<ApplicationDbContext> _options;

        public TestApplicationDbContextFactory(DbContextOptions<ApplicationDbContext> options) => _options = options;

        public ApplicationDbContext CreateDbContext() => new(_options);

        public ValueTask<ApplicationDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(new ApplicationDbContext(_options));
    }

    private static (EncounterService Service, EncounterPrepService Prep, NpcCombatService Npc) CreateServices(
        ApplicationDbContext ctx,
        DbContextOptions<ApplicationDbContext> options,
        Mock<ISessionService>? sessionMock = null,
        IDiceService? diceService = null)
    {
        Mock<ISessionService> mock = sessionMock ?? new Mock<ISessionService>();
        mock
            .Setup(s => s.UpdateInitiativeAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<IEnumerable<InitiativeEntryDto>>()))
            .Returns(Task.CompletedTask);

        Mock<IDiceService> defaultDice = new();
        defaultDice
            .Setup(d => d.Roll(It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int?>()))
            .Returns<int, bool, bool, bool, bool, int?>(
                (pool, _, _, _, _, _) => new RollResult
                {
                    Successes = 1,
                    DiceRolled = pool <= 0 ? [5] : Enumerable.Repeat(6, pool).ToList(),
                });

        var rules = new CharacterCreationRules();
        var auth = new AuthorizationHelper(ctx, NullLogger<AuthorizationHelper>.Instance);
        var dice = diceService ?? defaultDice.Object;

        var prep = new EncounterPrepService(ctx, NullLogger<EncounterPrepService>.Instance, auth, rules);
        var npc = new NpcCombatService(ctx, NullLogger<NpcCombatService>.Instance, auth, mock.Object, dice);
        var service = new EncounterService(ctx, NullLogger<EncounterService>.Instance, auth, mock.Object);

        return (service, prep, npc);
    }

    /// <summary>
    /// Creates a draft, launches it (no NPC templates), and returns the encounter id for active-combat operations.
    /// </summary>
    private static async Task<int> CreateLaunchedEmptyEncounterAsync(
        EncounterService service,
        EncounterPrepService prep,
        int campaignId,
        string stId)
    {
        CombatEncounter draft = await prep.CreateDraftEncounterAsync(campaignId, "Fight", stId);
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

    private static async Task<NpcStatBlock> SeedNpcStatBlockAsync(
        ApplicationDbContext ctx,
        int campaignId,
        string name,
        int health)
    {
        NpcStatBlock block = new()
        {
            CampaignId = campaignId,
            Name = name,
            Concept = "Test block",
            Health = health,
            Willpower = 4,
            Size = 5,
            IsPrebuilt = false,
        };
        ctx.NpcStatBlocks.Add(block);
        await ctx.SaveChangesAsync();
        return block;
    }

    private static async Task<ChronicleNpc> SeedChronicleNpcAsync(
        ApplicationDbContext ctx,
        int campaignId,
        string attributesJson,
        int? linkedStatBlockId = null,
        string name = "Chronicle NPC",
        CreatureType creatureType = CreatureType.Mortal,
        string skillsJson = "{}")
    {
        ChronicleNpc npc = new()
        {
            CampaignId = campaignId,
            Name = name,
            PublicDescription = "Test",
            AttributesJson = attributesJson,
            SkillsJson = skillsJson,
            CreatureType = creatureType,
            IsVampire = creatureType == CreatureType.Vampire,
            LinkedStatBlockId = linkedStatBlockId,
        };
        ctx.ChronicleNpcs.Add(npc);
        await ctx.SaveChangesAsync();
        return npc;
    }

    // ── Authorization ────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateDraftEncounterAsync_ThrowsUnauthorized_WhenCallerIsNotStoryteller()
    {
        var (ctx, options) = CreateContext(nameof(CreateDraftEncounterAsync_ThrowsUnauthorized_WhenCallerIsNotStoryteller));
        Campaign campaign = await SeedCampaignAsync(ctx);
        var (_, prep, _) = CreateServices(ctx, options);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => prep.CreateDraftEncounterAsync(campaign.Id, "Test Encounter", "random-user"));
    }

    [Fact]
    public async Task AddCharacterToEncounterAsync_ThrowsUnauthorized_WhenCallerIsNotStoryteller()
    {
        var (ctx, options) = CreateContext(nameof(AddCharacterToEncounterAsync_ThrowsUnauthorized_WhenCallerIsNotStoryteller));
        Campaign campaign = await SeedCampaignAsync(ctx);
        var (service, prep, _) = CreateServices(ctx, options);
        int encounterId = await CreateLaunchedEmptyEncounterAsync(service, prep, campaign.Id, "st-1");
        Character character = await SeedCharacterAsync(ctx, campaign.Id);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.AddCharacterToEncounterAsync(encounterId, character.Id, 3, 7, "random-user"));
    }

    [Fact]
    public async Task AdvanceTurnAsync_ThrowsUnauthorized_WhenCallerIsNotStoryteller()
    {
        var (ctx, options) = CreateContext(nameof(AdvanceTurnAsync_ThrowsUnauthorized_WhenCallerIsNotStoryteller));
        Campaign campaign = await SeedCampaignAsync(ctx);
        var (service, prep, _) = CreateServices(ctx, options);
        int encounterId = await CreateLaunchedEmptyEncounterAsync(service, prep, campaign.Id, "st-1");
        await service.AddNpcToEncounterAsync(encounterId, "Guard", 3, 5, "st-1");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.AdvanceTurnAsync(encounterId, "random-user"));
    }

    [Fact]
    public async Task ResolveEncounterAsync_ThrowsUnauthorized_WhenCallerIsNotStoryteller()
    {
        var (ctx, options) = CreateContext(nameof(ResolveEncounterAsync_ThrowsUnauthorized_WhenCallerIsNotStoryteller));
        Campaign campaign = await SeedCampaignAsync(ctx);
        var (service, prep, _) = CreateServices(ctx, options);
        int encounterId = await CreateLaunchedEmptyEncounterAsync(service, prep, campaign.Id, "st-1");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.ResolveEncounterAsync(encounterId, "random-user"));
    }

    [Fact]
    public async Task GetActiveEncounterForCampaignAsync_AllowsConcurrentReads_ForCampaignMember()
    {
        var (ctx, options) = CreateContext(nameof(GetActiveEncounterForCampaignAsync_AllowsConcurrentReads_ForCampaignMember));
        Campaign campaign = await SeedCampaignAsync(ctx);
        await SeedCharacterAsync(ctx, campaign.Id, playerId: "player-1");
        var (service, prep, _) = CreateServices(ctx, options);
        await CreateLaunchedEmptyEncounterAsync(service, prep, campaign.Id, "st-1");

        Task<CombatEncounter?> first = service.GetActiveEncounterForCampaignAsync(campaign.Id, "player-1");
        Task<CombatEncounter?> second = service.GetActiveEncounterForCampaignAsync(campaign.Id, "player-1");

        CombatEncounter?[] results = await Task.WhenAll(first, second);

        Assert.NotNull(results[0]);
        Assert.NotNull(results[1]);
        Assert.Equal(results[0]!.Id, results[1]!.Id);
    }

    // ── CreateDraftEncounterAsync ────────────────────────────────────────────

    [Fact]
    public async Task CreateDraftEncounterAsync_Persists_WhenCallerIsStoryteller()
    {
        var (ctx, options) = CreateContext(nameof(CreateDraftEncounterAsync_Persists_WhenCallerIsStoryteller));
        Campaign campaign = await SeedCampaignAsync(ctx);
        var (_, prep, _) = CreateServices(ctx, options);

        CombatEncounter encounter = await prep.CreateDraftEncounterAsync(campaign.Id, "The Alley Brawl", "st-1");

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
        var (ctx, options) = CreateContext(nameof(InitiativeOrder_SortsByTotalDescending));
        Campaign campaign = await SeedCampaignAsync(ctx);
        var (service, prep, _) = CreateServices(ctx, options);
        int encounterId = await CreateLaunchedEmptyEncounterAsync(service, prep, campaign.Id, "st-1");

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
        var (ctx, options) = CreateContext(nameof(InitiativeOrder_TieBreak_HigherModWins));
        Campaign campaign = await SeedCampaignAsync(ctx);
        var (service, prep, _) = CreateServices(ctx, options);
        int encounterId = await CreateLaunchedEmptyEncounterAsync(service, prep, campaign.Id, "st-1");

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
        var (ctx, options) = CreateContext(nameof(InitiativeOrder_TieBreak_PlayerCharacterBeforeNpc));
        Campaign campaign = await SeedCampaignAsync(ctx);
        var (service, prep, _) = CreateServices(ctx, options);
        int encounterId = await CreateLaunchedEmptyEncounterAsync(service, prep, campaign.Id, "st-1");
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
        var (ctx, options) = CreateContext(nameof(AdvanceTurnAsync_MarksCurrentActorAsActed));
        Campaign campaign = await SeedCampaignAsync(ctx);
        var (service, prep, _) = CreateServices(ctx, options);
        int encounterId = await CreateLaunchedEmptyEncounterAsync(service, prep, campaign.Id, "st-1");

        // Use distinctive totals to ensure a predictable order: Boss (12) then Guard (8)
        await service.AddNpcToEncounterAsync(encounterId, "Guard", 3, 5, "st-1");
        await service.AddNpcToEncounterAsync(encounterId, "Boss", 4, 8, "st-1");

        await service.AdvanceTurnAsync(encounterId, "st-1");

        List<InitiativeEntry> entries = await ctx.InitiativeEntries
            .AsNoTracking()
            .Where(i => i.EncounterId == encounterId)
            .ToListAsync();

        var boss = entries.First(e => e.NpcName == "Boss");
        var guard = entries.First(e => e.NpcName == "Guard");

        Assert.True(boss.HasActed);
        Assert.False(guard.HasActed);
    }

    [Fact]
    public async Task AdvanceTurnAsync_ResetsAllEntries_WhenRoundEnds()
    {
        var (ctx, options) = CreateContext(nameof(AdvanceTurnAsync_ResetsAllEntries_WhenRoundEnds));
        Campaign campaign = await SeedCampaignAsync(ctx);
        var (service, prep, _) = CreateServices(ctx, options);
        int encounterId = await CreateLaunchedEmptyEncounterAsync(service, prep, campaign.Id, "st-1");

        // Totals: A (8), B (5)
        await service.AddNpcToEncounterAsync(encounterId, "A", 3, 5, "st-1");
        await service.AddNpcToEncounterAsync(encounterId, "B", 2, 3, "st-1");

        await service.AdvanceTurnAsync(encounterId, "st-1"); // A acts
        await service.AdvanceTurnAsync(encounterId, "st-1"); // B acts -> round ends, resets

        List<InitiativeEntry> entries = await ctx.InitiativeEntries
            .AsNoTracking()
            .Where(i => i.EncounterId == encounterId)
            .ToListAsync();

        Assert.All(entries, e => Assert.False(e.HasActed));

        await service.AdvanceTurnAsync(encounterId, "st-1"); // A acts again in Round 2

        entries = await ctx.InitiativeEntries
            .AsNoTracking()
            .Where(i => i.EncounterId == encounterId)
            .ToListAsync();

        var a = entries.First(e => e.NpcName == "A");
        var b = entries.First(e => e.NpcName == "B");

        Assert.True(a.HasActed);
        Assert.False(b.HasActed);
    }

    // ── ResolveEncounterAsync ────────────────────────────────────────────────

    [Fact]
    public async Task ResolveEncounterAsync_SetsIsActiveFalse()
    {
        var (ctx, options) = CreateContext(nameof(ResolveEncounterAsync_SetsIsActiveFalse));
        Campaign campaign = await SeedCampaignAsync(ctx);
        var (service, prep, _) = CreateServices(ctx, options);
        int encounterId = await CreateLaunchedEmptyEncounterAsync(service, prep, campaign.Id, "st-1");

        await service.ResolveEncounterAsync(encounterId, "st-1");

        CombatEncounter? stored = await ctx.CombatEncounters.FindAsync(encounterId);
        Assert.NotNull(stored);
        Assert.False(stored.IsActive);
        Assert.NotNull(stored.ResolvedAt);
    }

    [Fact]
    public async Task ResolveEncounterAsync_ThrowsInvalidOperation_WhenAlreadyResolved()
    {
        var (ctx, options) = CreateContext(nameof(ResolveEncounterAsync_ThrowsInvalidOperation_WhenAlreadyResolved));
        Campaign campaign = await SeedCampaignAsync(ctx);
        var (service, prep, _) = CreateServices(ctx, options);
        int encounterId = await CreateLaunchedEmptyEncounterAsync(service, prep, campaign.Id, "st-1");
        await service.ResolveEncounterAsync(encounterId, "st-1");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ResolveEncounterAsync(encounterId, "st-1"));
    }

    // ── GetEncountersAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetEncountersAsync_ReturnsActiveFirst()
    {
        var (ctx, options) = CreateContext(nameof(GetEncountersAsync_ReturnsActiveFirst));
        Campaign campaign = await SeedCampaignAsync(ctx);
        var (service, prep, _) = CreateServices(ctx, options);

        CombatEncounter pastDraft = await prep.CreateDraftEncounterAsync(campaign.Id, "Past Fight", "st-1");
        await service.LaunchEncounterAsync(pastDraft.Id, "st-1");
        await service.ResolveEncounterAsync(pastDraft.Id, "st-1");

        CombatEncounter currentDraft = await prep.CreateDraftEncounterAsync(campaign.Id, "Current Fight", "st-1");
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
        var (ctx, options) = CreateContext(nameof(PauseEncounterAsync_SetsPaused_And_ClearsActive));
        Campaign campaign = await SeedCampaignAsync(ctx);
        var (service, prep, _) = CreateServices(ctx, options);
        int encounterId = await CreateLaunchedEmptyEncounterAsync(service, prep, campaign.Id, "st-1");

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
        var (ctx, options) = CreateContext(nameof(ResumeEncounterAsync_RestoresActive_FromPaused));
        Campaign campaign = await SeedCampaignAsync(ctx);
        var (service, prep, _) = CreateServices(ctx, options);
        int encounterId = await CreateLaunchedEmptyEncounterAsync(service, prep, campaign.Id, "st-1");

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
        var (ctx, options) = CreateContext(nameof(LaunchEncounterAsync_Throws_WhenAnotherEncounterIsPaused));
        Campaign campaign = await SeedCampaignAsync(ctx);
        var (service, prep, _) = CreateServices(ctx, options);

        CombatEncounter first = await prep.CreateDraftEncounterAsync(campaign.Id, "First", "st-1");
        await service.LaunchEncounterAsync(first.Id, "st-1");
        await service.PauseEncounterAsync(first.Id, "st-1");

        CombatEncounter second = await prep.CreateDraftEncounterAsync(campaign.Id, "Second", "st-1");

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.LaunchEncounterAsync(second.Id, "st-1"));
    }

    [Fact]
    public async Task ResolveEncounterAsync_WorksFromPausedState()
    {
        var (ctx, options) = CreateContext(nameof(ResolveEncounterAsync_WorksFromPausedState));
        Campaign campaign = await SeedCampaignAsync(ctx);
        var (service, prep, _) = CreateServices(ctx, options);
        int encounterId = await CreateLaunchedEmptyEncounterAsync(service, prep, campaign.Id, "st-1");

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
        var (ctx, options) = CreateContext(nameof(ResumeEncounterAsync_Throws_WhenAlreadyResolved));
        Campaign campaign = await SeedCampaignAsync(ctx);
        var (service, prep, _) = CreateServices(ctx, options);
        int encounterId = await CreateLaunchedEmptyEncounterAsync(service, prep, campaign.Id, "st-1");

        await service.PauseEncounterAsync(encounterId, "st-1");
        await service.ResolveEncounterAsync(encounterId, "st-1");

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ResumeEncounterAsync(encounterId, "st-1"));
    }

    // ── NPC template / active NPC validation ────────────────────────────────

    [Fact]
    public async Task AddNpcTemplateAsync_ThrowsArgumentException_WhenNameIsWhitespace()
    {
        var (ctx, options) = CreateContext(nameof(AddNpcTemplateAsync_ThrowsArgumentException_WhenNameIsWhitespace));
        Campaign campaign = await SeedCampaignAsync(ctx);
        var (service, prep, _) = CreateServices(ctx, options);
        CombatEncounter draft = await prep.CreateDraftEncounterAsync(campaign.Id, "Prep", "st-1");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            prep.AddNpcTemplateAsync(draft.Id, "   ", 0, 7, 4, null, true, null, "st-1"));
    }

    [Fact]
    public async Task AddNpcToEncounterAsync_ThrowsArgumentException_WhenNameIsWhitespace()
    {
        var (ctx, options) = CreateContext(nameof(AddNpcToEncounterAsync_ThrowsArgumentException_WhenNameIsWhitespace));
        Campaign campaign = await SeedCampaignAsync(ctx);
        var (service, prep, _) = CreateServices(ctx, options);
        int encounterId = await CreateLaunchedEmptyEncounterAsync(service, prep, campaign.Id, "st-1");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.AddNpcToEncounterAsync(encounterId, "", 2, 5, "st-1"));
    }

    [Fact]
    public async Task AddNpcToEncounterAsync_UsesNpcHealthBoxes_WhenProvided()
    {
        var (ctx, options) = CreateContext(nameof(AddNpcToEncounterAsync_UsesNpcHealthBoxes_WhenProvided));
        Campaign campaign = await SeedCampaignAsync(ctx);
        var (service, prep, _) = CreateServices(ctx, options);
        int encounterId = await CreateLaunchedEmptyEncounterAsync(service, prep, campaign.Id, "st-1");

        await service.AddNpcToEncounterAsync(encounterId, "Brute", 2, 6, "st-1", npcHealthBoxes: 12);

        InitiativeEntry? row = await ctx.InitiativeEntries.FirstOrDefaultAsync(i => i.EncounterId == encounterId);
        Assert.NotNull(row);
        Assert.Equal(12, row.NpcHealthBoxes);
        Assert.Equal(4, row.NpcMaxWillpower);
        Assert.Equal(4, row.NpcCurrentWillpower);
    }

    // ── Draft rename ────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateDraftEncounterNameAsync_RenamesEncounter()
    {
        var (ctx, options) = CreateContext(nameof(UpdateDraftEncounterNameAsync_RenamesEncounter));
        Campaign campaign = await SeedCampaignAsync(ctx);
        var (service, prep, _) = CreateServices(ctx, options);
        CombatEncounter draft = await prep.CreateDraftEncounterAsync(campaign.Id, "Old", "st-1");

        await prep.UpdateDraftEncounterNameAsync(draft.Id, "Warehouse ambush", "st-1");

        CombatEncounter? stored = await ctx.CombatEncounters.FindAsync(draft.Id);
        Assert.NotNull(stored);
        Assert.Equal("Warehouse ambush", stored.Name);
    }

    [Fact]
    public async Task UpdateDraftEncounterNameAsync_Throws_WhenEncounterLaunched()
    {
        var (ctx, options) = CreateContext(nameof(UpdateDraftEncounterNameAsync_Throws_WhenEncounterLaunched));
        Campaign campaign = await SeedCampaignAsync(ctx);
        var (service, prep, _) = CreateServices(ctx, options);
        int encounterId = await CreateLaunchedEmptyEncounterAsync(service, prep, campaign.Id, "st-1");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            prep.UpdateDraftEncounterNameAsync(encounterId, "Nope", "st-1"));
    }

    [Fact]
    public async Task UpdateDraftEncounterNameAsync_ThrowsUnauthorized_WhenCallerIsNotStoryteller()
    {
        var (ctx, options) = CreateContext(nameof(UpdateDraftEncounterNameAsync_ThrowsUnauthorized_WhenCallerIsNotStoryteller));
        Campaign campaign = await SeedCampaignAsync(ctx);
        var (service, prep, _) = CreateServices(ctx, options);
        CombatEncounter draft = await prep.CreateDraftEncounterAsync(campaign.Id, "Prep", "st-1");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            prep.UpdateDraftEncounterNameAsync(draft.Id, "Hack", "random-user"));
    }

    // ── Chronicle NPC encounter prep ───────────────────────────────────────

    [Fact]
    public async Task GetChronicleNpcEncounterPrepAsync_ReturnsWitsPlusComposure()
    {
        var (ctx, options) = CreateContext(nameof(GetChronicleNpcEncounterPrepAsync_ReturnsWitsPlusComposure));
        Campaign campaign = await SeedCampaignAsync(ctx);
        var (service, prep, _) = CreateServices(ctx, options);
        ChronicleNpc npc = await SeedChronicleNpcAsync(
            ctx,
            campaign.Id,
            "{\"Wits\":3,\"Composure\":4,\"Strength\":2,\"Dexterity\":2,\"Stamina\":2,\"Intelligence\":2,\"Resolve\":2,\"Presence\":2,\"Manipulation\":2}");

        ChronicleNpcEncounterPrepDto? dto = await prep.GetChronicleNpcEncounterPrepAsync(npc.Id, "st-1");

        Assert.NotNull(dto);
        Assert.Equal("Chronicle NPC", dto.Name);
        Assert.Equal(7, dto.SuggestedInitiativeMod);
        Assert.Equal(7, dto.SuggestedHealthBoxes);
        Assert.Null(dto.LinkedStatBlockName);
        Assert.Equal(6, dto.SuggestedMaxWillpower);
        Assert.False(dto.TracksVitae);
        Assert.Equal(0, dto.SuggestedMaxVitae);
    }

    [Fact]
    public async Task GetChronicleNpcEncounterPrepAsync_Vampire_SuggestsVitaeFromBloodPotency()
    {
        var (ctx, options) = CreateContext(nameof(GetChronicleNpcEncounterPrepAsync_Vampire_SuggestsVitaeFromBloodPotency));
        Campaign campaign = await SeedCampaignAsync(ctx);
        var (service, prep, _) = CreateServices(ctx, options);
        ChronicleNpc npc = await SeedChronicleNpcAsync(
            ctx,
            campaign.Id,
            "{\"Wits\":2,\"Composure\":2,\"Strength\":2,\"Dexterity\":2,\"Stamina\":2,\"Intelligence\":2,\"Resolve\":2,\"Presence\":2,\"Manipulation\":2,\"BloodPotency\":3}",
            creatureType: CreatureType.Vampire);

        ChronicleNpcEncounterPrepDto? dto = await prep.GetChronicleNpcEncounterPrepAsync(npc.Id, "st-1");

        Assert.NotNull(dto);
        Assert.True(dto.TracksVitae);
        Assert.Equal(12, dto.SuggestedMaxVitae);
    }

    [Fact]
    public async Task GetChronicleNpcEncounterPrepAsync_UsesLinkedStatBlockHealth()
    {
        var (ctx, options) = CreateContext(nameof(GetChronicleNpcEncounterPrepAsync_UsesLinkedStatBlockHealth));
        Campaign campaign = await SeedCampaignAsync(ctx);
        var (service, prep, _) = CreateServices(ctx, options);
        NpcStatBlock block = await SeedNpcStatBlockAsync(ctx, campaign.Id, "Thug template", health: 11);
        ChronicleNpc npc = await SeedChronicleNpcAsync(
            ctx,
            campaign.Id,
            "{\"Wits\":2,\"Composure\":2,\"Strength\":2,\"Dexterity\":2,\"Stamina\":2,\"Intelligence\":2,\"Resolve\":2,\"Presence\":2,\"Manipulation\":2}",
            linkedStatBlockId: block.Id);

        ChronicleNpcEncounterPrepDto? dto = await prep.GetChronicleNpcEncounterPrepAsync(npc.Id, "st-1");

        Assert.NotNull(dto);
        Assert.Equal(11, dto.SuggestedHealthBoxes);
        Assert.Equal("Thug template", dto.LinkedStatBlockName);
        Assert.Equal(4, dto.SuggestedMaxWillpower);
        Assert.False(dto.TracksVitae);
        Assert.Equal(0, dto.SuggestedMaxVitae);
    }

    [Fact]
    public async Task AddNpcTemplateFromChronicleNpcAsync_Throws_WhenNpcInOtherCampaign()
    {
        var (ctx, options) = CreateContext(nameof(AddNpcTemplateFromChronicleNpcAsync_Throws_WhenNpcInOtherCampaign));
        Campaign alpha = await SeedCampaignAsync(ctx, "st-1");
        Campaign beta = new() { Name = "Other city", StoryTellerId = "st-1" };
        ctx.Campaigns.Add(beta);
        await ctx.SaveChangesAsync();

        var (service, prep, _) = CreateServices(ctx, options);
        CombatEncounter draft = await prep.CreateDraftEncounterAsync(alpha.Id, "Fight", "st-1");
        ChronicleNpc npc = await SeedChronicleNpcAsync(
            ctx,
            beta.Id,
            "{\"Wits\":2,\"Composure\":2,\"Strength\":2,\"Dexterity\":2,\"Stamina\":2,\"Intelligence\":2,\"Resolve\":2,\"Presence\":2,\"Manipulation\":2}");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            prep.AddNpcTemplateFromChronicleNpcAsync(draft.Id, npc.Id, 0, 7, 4, 0, true, null, "st-1"));
    }

    [Fact]
    public async Task AddNpcTemplateFromChronicleNpcAsync_Throws_WhenChronicleNpcAlreadyInPrep()
    {
        var (ctx, options) = CreateContext(nameof(AddNpcTemplateFromChronicleNpcAsync_Throws_WhenChronicleNpcAlreadyInPrep));
        Campaign campaign = await SeedCampaignAsync(ctx);
        var (service, prep, _) = CreateServices(ctx, options);
        CombatEncounter draft = await prep.CreateDraftEncounterAsync(campaign.Id, "Prep", "st-1");
        ChronicleNpc npc = await SeedChronicleNpcAsync(
            ctx,
            campaign.Id,
            "{\"Wits\":2,\"Composure\":2,\"Strength\":2,\"Dexterity\":2,\"Stamina\":2,\"Intelligence\":2,\"Resolve\":2,\"Presence\":2,\"Manipulation\":2}");

        await prep.AddNpcTemplateFromChronicleNpcAsync(draft.Id, npc.Id, 1, 7, 4, 0, true, null, "st-1");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            prep.AddNpcTemplateFromChronicleNpcAsync(draft.Id, npc.Id, 1, 7, 4, 0, true, null, "st-1"));
    }

    [Fact]
    public async Task AddNpcTemplateFromChronicleNpcAsync_AddsRow_WithNpcName()
    {
        var (ctx, options) = CreateContext(nameof(AddNpcTemplateFromChronicleNpcAsync_AddsRow_WithNpcName));
        Campaign campaign = await SeedCampaignAsync(ctx);
        var (service, prep, _) = CreateServices(ctx, options);
        CombatEncounter draft = await prep.CreateDraftEncounterAsync(campaign.Id, "Prep", "st-1");
        ChronicleNpc npc = await SeedChronicleNpcAsync(
            ctx,
            campaign.Id,
            "{\"Wits\":2,\"Composure\":2,\"Strength\":2,\"Dexterity\":2,\"Stamina\":2,\"Intelligence\":2,\"Resolve\":2,\"Presence\":2,\"Manipulation\":2}",
            name: "Elena Vance");

        await prep.AddNpcTemplateFromChronicleNpcAsync(draft.Id, npc.Id, initiativeMod: 5, healthBoxes: 8, maxWillpower: 6, maxVitae: 0, true, null, "st-1");

        EncounterNpcTemplate? row = await ctx.Set<EncounterNpcTemplate>().FirstOrDefaultAsync(t => t.EncounterId == draft.Id);
        Assert.NotNull(row);
        Assert.Equal("Elena Vance", row.Name);
        Assert.Equal(5, row.InitiativeMod);
        Assert.Equal(8, row.HealthBoxes);
        Assert.Equal(6, row.MaxWillpower);
        Assert.Equal(npc.Id, row.ChronicleNpcId);
    }

    [Fact]
    public async Task AddNpcToEncounterFromChronicleNpcAsync_AddsInitiativeRow()
    {
        var (ctx, options) = CreateContext(nameof(AddNpcToEncounterFromChronicleNpcAsync_AddsInitiativeRow));
        Campaign campaign = await SeedCampaignAsync(ctx);
        var (service, prep, _) = CreateServices(ctx, options);
        int encounterId = await CreateLaunchedEmptyEncounterAsync(service, prep, campaign.Id, "st-1");
        NpcStatBlock block = await SeedNpcStatBlockAsync(ctx, campaign.Id, "Heavy", health: 10);
        ChronicleNpc npc = await SeedChronicleNpcAsync(
            ctx,
            campaign.Id,
            "{\"Wits\":2,\"Composure\":2,\"Strength\":2,\"Dexterity\":2,\"Stamina\":2,\"Intelligence\":2,\"Resolve\":2,\"Presence\":2,\"Manipulation\":2}",
            linkedStatBlockId: block.Id,
            name: "The Brute");

        InitiativeEntry entry = await service.AddNpcToEncounterFromChronicleNpcAsync(
            encounterId,
            npc.Id,
            initiativeMod: 4,
            rollResult: 6,
            healthBoxes: 10,
            maxWillpower: 4,
            maxVitae: 0,
            "st-1");

        Assert.Equal("The Brute", entry.NpcName);
        Assert.Equal(10, entry.Total);
        Assert.Equal(10, entry.NpcHealthBoxes);
        Assert.Equal(4, entry.NpcMaxWillpower);
        Assert.Equal(4, entry.NpcCurrentWillpower);
        Assert.Equal(npc.Id, entry.ChronicleNpcId);
    }

    [Fact]
    public async Task AddNpcToEncounterFromChronicleNpcAsync_Throws_WhenChronicleNpcAlreadyInFight()
    {
        var (ctx, options) = CreateContext(nameof(AddNpcToEncounterFromChronicleNpcAsync_Throws_WhenChronicleNpcAlreadyInFight));
        Campaign campaign = await SeedCampaignAsync(ctx);
        var (service, prep, _) = CreateServices(ctx, options);
        int encounterId = await CreateLaunchedEmptyEncounterAsync(service, prep, campaign.Id, "st-1");
        ChronicleNpc npc = await SeedChronicleNpcAsync(
            ctx,
            campaign.Id,
            "{\"Wits\":2,\"Composure\":2,\"Strength\":2,\"Dexterity\":2,\"Stamina\":2,\"Intelligence\":2,\"Resolve\":2,\"Presence\":2,\"Manipulation\":2}");

        await service.AddNpcToEncounterFromChronicleNpcAsync(
            encounterId,
            npc.Id,
            initiativeMod: 1,
            rollResult: 5,
            healthBoxes: 7,
            maxWillpower: 4,
            maxVitae: 0,
            "st-1");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AddNpcToEncounterFromChronicleNpcAsync(
                encounterId,
                npc.Id,
                initiativeMod: 1,
                rollResult: 5,
                healthBoxes: 7,
                maxWillpower: 4,
                maxVitae: 0,
                "st-1"));
    }

    [Fact]
    public async Task LaunchEncounterAsync_CopiesTemplateWillpowerToInitiative()
    {
        var (ctx, options) = CreateContext(nameof(LaunchEncounterAsync_CopiesTemplateWillpowerToInitiative));
        Campaign campaign = await SeedCampaignAsync(ctx);
        var (service, prep, _) = CreateServices(ctx, options);
        CombatEncounter draft = await prep.CreateDraftEncounterAsync(campaign.Id, "Ambush", "st-1");
        await prep.AddNpcTemplateAsync(draft.Id, "Hunter", 2, 9, 5, null, true, null, "st-1");

        await service.LaunchEncounterAsync(draft.Id, "st-1");

        InitiativeEntry? row = await ctx.InitiativeEntries.FirstOrDefaultAsync(i => i.EncounterId == draft.Id);
        Assert.NotNull(row);
        Assert.Equal(9, row.NpcHealthBoxes);
        Assert.Equal(5, row.NpcMaxWillpower);
        Assert.Equal(5, row.NpcCurrentWillpower);
    }

    [Fact]
    public async Task LaunchEncounterAsync_CopiesChronicleNpcIdAndVitaeFromTemplate()
    {
        var (ctx, options) = CreateContext(nameof(LaunchEncounterAsync_CopiesChronicleNpcIdAndVitaeFromTemplate));
        Campaign campaign = await SeedCampaignAsync(ctx);
        var (service, prep, _) = CreateServices(ctx, options);
        CombatEncounter draft = await prep.CreateDraftEncounterAsync(campaign.Id, "Night", "st-1");
        ChronicleNpc npc = await SeedChronicleNpcAsync(
            ctx,
            campaign.Id,
            "{\"Wits\":2,\"Composure\":2,\"Strength\":2,\"Dexterity\":2,\"Stamina\":2,\"Intelligence\":2,\"Resolve\":2,\"Presence\":2,\"Manipulation\":2}",
            creatureType: CreatureType.Vampire);

        await prep.AddNpcTemplateFromChronicleNpcAsync(
            draft.Id,
            npc.Id,
            initiativeMod: 3,
            healthBoxes: 7,
            maxWillpower: 5,
            maxVitae: 11,
            isRevealed: true,
            defaultMaskedName: null,
            storyTellerUserId: "st-1");

        await service.LaunchEncounterAsync(draft.Id, "st-1");

        InitiativeEntry? row = await ctx.InitiativeEntries.FirstOrDefaultAsync(i => i.EncounterId == draft.Id);
        Assert.NotNull(row);
        Assert.Equal(npc.Id, row.ChronicleNpcId);
        Assert.Equal(11, row.NpcMaxVitae);
        Assert.Equal(11, row.NpcCurrentVitae);
    }

    [Fact]
    public async Task SpendNpcWillpowerAsync_DecrementsCurrent()
    {
        var (ctx, options) = CreateContext(nameof(SpendNpcWillpowerAsync_DecrementsCurrent));
        Campaign campaign = await SeedCampaignAsync(ctx);
        var (service, prep, npc) = CreateServices(ctx, options);
        int encounterId = await CreateLaunchedEmptyEncounterAsync(service, prep, campaign.Id, "st-1");
        await service.AddNpcToEncounterAsync(encounterId, "Wolf", 1, 5, "st-1", npcHealthBoxes: 7, npcMaxWillpower: 3);

        InitiativeEntry? before = await ctx.InitiativeEntries.FirstOrDefaultAsync(i => i.EncounterId == encounterId);
        Assert.NotNull(before);
        int entryId = before.Id;

        await npc.SpendNpcWillpowerAsync(entryId, "st-1");

        InitiativeEntry? after = await ctx.InitiativeEntries.FindAsync(entryId);
        Assert.NotNull(after);
        Assert.Equal(2, after.NpcCurrentWillpower);
    }

    [Fact]
    public async Task SpendNpcVitaeAsync_DecrementsCurrent()
    {
        var (ctx, options) = CreateContext(nameof(SpendNpcVitaeAsync_DecrementsCurrent));
        Campaign campaign = await SeedCampaignAsync(ctx);
        var (service, prep, npc) = CreateServices(ctx, options);
        int encounterId = await CreateLaunchedEmptyEncounterAsync(service, prep, campaign.Id, "st-1");
        await service.AddNpcToEncounterAsync(
            encounterId,
            "Kindred",
            1,
            5,
            "st-1",
            npcHealthBoxes: 7,
            npcMaxWillpower: 3,
            chronicleNpcId: null,
            npcMaxVitae: 4);

        InitiativeEntry? before = await ctx.InitiativeEntries.FirstOrDefaultAsync(i => i.EncounterId == encounterId);
        Assert.NotNull(before);
        int entryId = before.Id;

        await npc.SpendNpcVitaeAsync(entryId, "st-1");

        InitiativeEntry? after = await ctx.InitiativeEntries.FindAsync(entryId);
        Assert.NotNull(after);
        Assert.Equal(3, after.NpcCurrentVitae);
    }

    [Fact]
    public async Task RollNpcEncounterPoolAsync_PlainNpc_RequiresManualPool()
    {
        var (ctx, options) = CreateContext(nameof(RollNpcEncounterPoolAsync_PlainNpc_RequiresManualPool));
        Campaign campaign = await SeedCampaignAsync(ctx);
        var (service, prep, npc) = CreateServices(ctx, options);
        int encounterId = await CreateLaunchedEmptyEncounterAsync(service, prep, campaign.Id, "st-1");
        await service.AddNpcToEncounterAsync(encounterId, "Grunt", 2, 4, "st-1");
        InitiativeEntry entry = await ctx.InitiativeEntries.FirstAsync(i => i.EncounterId == encounterId);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => npc.RollNpcEncounterPoolAsync(entry.Id, "Wits", "Composure", null, "st-1"));

        NpcEncounterRollResultDto dto = await npc.RollNpcEncounterPoolAsync(entry.Id, null, null, 3, "st-1");
        Assert.Equal(3, dto.DiceRolled.Count);
        Assert.Contains("Manual pool (3)", dto.PoolDescription);
    }

    [Fact]
    public async Task RollNpcEncounterPoolAsync_ChronicleNpc_BuildsPoolFromTraits()
    {
        Mock<IDiceService> dice = new();
        dice.Setup(d => d.Roll(6, true, false, false, false, null)).Returns(
            new RollResult { Successes = 2, DiceRolled = [5, 5, 5, 5, 5, 5] });

        var (ctx, options) = CreateContext(nameof(RollNpcEncounterPoolAsync_ChronicleNpc_BuildsPoolFromTraits));
        Campaign campaign = await SeedCampaignAsync(ctx);
        var (service, prep, npc) = CreateServices(ctx, options, diceService: dice.Object);
        int encounterId = await CreateLaunchedEmptyEncounterAsync(service, prep, campaign.Id, "st-1");
        ChronicleNpc n = await SeedChronicleNpcAsync(
            ctx,
            campaign.Id,
            "{\"Wits\":4}",
            skillsJson: "{\"Stealth\":2}");
        InitiativeEntry entry = await service.AddNpcToEncounterFromChronicleNpcAsync(
            encounterId,
            n.Id,
            initiativeMod: 2,
            rollResult: 5,
            healthBoxes: 7,
            maxWillpower: 4,
            maxVitae: 0,
            storyTellerUserId: "st-1");

        NpcEncounterRollResultDto dto = await npc.RollNpcEncounterPoolAsync(
            entry.Id,
            "Wits",
            "Stealth",
            null,
            "st-1");

        dice.Verify(d => d.Roll(6, true, false, false, false, null), Times.Once);
        Assert.Contains("Wits", dto.PoolDescription);
        Assert.Contains("Stealth", dto.PoolDescription);
        Assert.Contains("(6)", dto.PoolDescription);
    }

    [Fact]
    public async Task RollNpcEncounterPoolAsync_ManualOverride_IgnoresTraits()
    {
        Mock<IDiceService> dice = new();
        dice.Setup(d => d.Roll(2, true, false, false, false, null)).Returns(
            new RollResult { Successes = 1, DiceRolled = [5, 5] });

        var (ctx, options) = CreateContext(nameof(RollNpcEncounterPoolAsync_ManualOverride_IgnoresTraits));
        Campaign campaign = await SeedCampaignAsync(ctx);
        var (service, prep, npc) = CreateServices(ctx, options, diceService: dice.Object);
        int encounterId = await CreateLaunchedEmptyEncounterAsync(service, prep, campaign.Id, "st-1");
        ChronicleNpc n = await SeedChronicleNpcAsync(ctx, campaign.Id, "{\"Wits\":4}");
        InitiativeEntry entry = await service.AddNpcToEncounterFromChronicleNpcAsync(
            encounterId,
            n.Id,
            2,
            5,
            7,
            4,
            0,
            "st-1");

        NpcEncounterRollResultDto dto = await npc.RollNpcEncounterPoolAsync(
            entry.Id,
            "Wits",
            "Strength",
            2,
            "st-1");

        dice.Verify(d => d.Roll(2, true, false, false, false, null), Times.Once);
        Assert.Equal("Manual pool (2)", dto.PoolDescription);
    }

    [Fact]
    public async Task RollNpcEncounterPoolAsync_ThrowsUnauthorized_WhenNotStoryteller()
    {
        var (ctx, options) = CreateContext(nameof(RollNpcEncounterPoolAsync_ThrowsUnauthorized_WhenNotStoryteller));
        Campaign campaign = await SeedCampaignAsync(ctx);
        var (service, prep, npc) = CreateServices(ctx, options);
        int encounterId = await CreateLaunchedEmptyEncounterAsync(service, prep, campaign.Id, "st-1");
        await service.AddNpcToEncounterAsync(encounterId, "Grunt", 2, 4, "st-1");
        InitiativeEntry entry = await ctx.InitiativeEntries.FirstAsync(i => i.EncounterId == encounterId);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => npc.RollNpcEncounterPoolAsync(entry.Id, null, null, 2, "not-st"));
    }

    [Fact]
    public async Task RollNpcEncounterPoolAsync_Throws_WhenPlayerCharacterRow()
    {
        var (ctx, options) = CreateContext(nameof(RollNpcEncounterPoolAsync_Throws_WhenPlayerCharacterRow));
        Campaign campaign = await SeedCampaignAsync(ctx);
        var (service, prep, npc) = CreateServices(ctx, options);
        int encounterId = await CreateLaunchedEmptyEncounterAsync(service, prep, campaign.Id, "st-1");
        Character character = await SeedCharacterAsync(ctx, campaign.Id);
        InitiativeEntry entry = await service.AddCharacterToEncounterAsync(encounterId, character.Id, 3, 6, "st-1");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => npc.RollNpcEncounterPoolAsync(entry.Id, null, null, 3, "st-1"));
    }

    [Fact]
    public async Task RollNpcEncounterPoolAsync_ChronicleNpc_Throws_WhenMissingTraitsAndNoManual()
    {
        var (ctx, options) = CreateContext(nameof(RollNpcEncounterPoolAsync_ChronicleNpc_Throws_WhenMissingTraitsAndNoManual));
        Campaign campaign = await SeedCampaignAsync(ctx);
        var (service, prep, npc) = CreateServices(ctx, options);
        int encounterId = await CreateLaunchedEmptyEncounterAsync(service, prep, campaign.Id, "st-1");
        ChronicleNpc n = await SeedChronicleNpcAsync(ctx, campaign.Id, "{}");
        InitiativeEntry entry = await service.AddNpcToEncounterFromChronicleNpcAsync(
            encounterId,
            n.Id,
            2,
            5,
            7,
            4,
            0,
            "st-1");

        await Assert.ThrowsAsync<ArgumentException>(
            () => npc.RollNpcEncounterPoolAsync(entry.Id, null, null, null, "st-1"));
    }
}
