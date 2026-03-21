using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Application.Services;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.Models.Enums;
using RequiemNexus.Data.RealTime;
using RequiemNexus.Domain;
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
            mock.Object,
            new CharacterCreationRules());
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
        CreatureType creatureType = CreatureType.Mortal)
    {
        ChronicleNpc npc = new()
        {
            CampaignId = campaignId,
            Name = name,
            PublicDescription = "Test",
            AttributesJson = attributesJson,
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

    // ── NPC template / active NPC validation ────────────────────────────────

    [Fact]
    public async Task AddNpcTemplateAsync_ThrowsArgumentException_WhenNameIsWhitespace()
    {
        ApplicationDbContext ctx = CreateContext(nameof(AddNpcTemplateAsync_ThrowsArgumentException_WhenNameIsWhitespace));
        Campaign campaign = await SeedCampaignAsync(ctx);
        EncounterService service = CreateService(ctx);
        CombatEncounter draft = await service.CreateDraftEncounterAsync(campaign.Id, "Prep", "st-1");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.AddNpcTemplateAsync(draft.Id, "   ", 0, 7, 4, null, true, null, "st-1"));
    }

    [Fact]
    public async Task AddNpcToEncounterAsync_ThrowsArgumentException_WhenNameIsWhitespace()
    {
        ApplicationDbContext ctx = CreateContext(nameof(AddNpcToEncounterAsync_ThrowsArgumentException_WhenNameIsWhitespace));
        Campaign campaign = await SeedCampaignAsync(ctx);
        EncounterService service = CreateService(ctx);
        int encounterId = await CreateLaunchedEmptyEncounterAsync(service, campaign.Id, "st-1");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.AddNpcToEncounterAsync(encounterId, "", 2, 5, "st-1"));
    }

    [Fact]
    public async Task AddNpcToEncounterAsync_UsesNpcHealthBoxes_WhenProvided()
    {
        ApplicationDbContext ctx = CreateContext(nameof(AddNpcToEncounterAsync_UsesNpcHealthBoxes_WhenProvided));
        Campaign campaign = await SeedCampaignAsync(ctx);
        EncounterService service = CreateService(ctx);
        int encounterId = await CreateLaunchedEmptyEncounterAsync(service, campaign.Id, "st-1");

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
        ApplicationDbContext ctx = CreateContext(nameof(UpdateDraftEncounterNameAsync_RenamesEncounter));
        Campaign campaign = await SeedCampaignAsync(ctx);
        EncounterService service = CreateService(ctx);
        CombatEncounter draft = await service.CreateDraftEncounterAsync(campaign.Id, "Old", "st-1");

        await service.UpdateDraftEncounterNameAsync(draft.Id, "Warehouse ambush", "st-1");

        CombatEncounter? stored = await ctx.CombatEncounters.FindAsync(draft.Id);
        Assert.NotNull(stored);
        Assert.Equal("Warehouse ambush", stored.Name);
    }

    [Fact]
    public async Task UpdateDraftEncounterNameAsync_Throws_WhenEncounterLaunched()
    {
        ApplicationDbContext ctx = CreateContext(nameof(UpdateDraftEncounterNameAsync_Throws_WhenEncounterLaunched));
        Campaign campaign = await SeedCampaignAsync(ctx);
        EncounterService service = CreateService(ctx);
        int encounterId = await CreateLaunchedEmptyEncounterAsync(service, campaign.Id, "st-1");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateDraftEncounterNameAsync(encounterId, "Nope", "st-1"));
    }

    [Fact]
    public async Task UpdateDraftEncounterNameAsync_ThrowsUnauthorized_WhenCallerIsNotStoryteller()
    {
        ApplicationDbContext ctx = CreateContext(nameof(UpdateDraftEncounterNameAsync_ThrowsUnauthorized_WhenCallerIsNotStoryteller));
        Campaign campaign = await SeedCampaignAsync(ctx);
        EncounterService service = CreateService(ctx);
        CombatEncounter draft = await service.CreateDraftEncounterAsync(campaign.Id, "Prep", "st-1");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.UpdateDraftEncounterNameAsync(draft.Id, "Hack", "random-user"));
    }

    // ── Chronicle NPC encounter prep ───────────────────────────────────────

    [Fact]
    public async Task GetChronicleNpcEncounterPrepAsync_ReturnsWitsPlusComposure()
    {
        ApplicationDbContext ctx = CreateContext(nameof(GetChronicleNpcEncounterPrepAsync_ReturnsWitsPlusComposure));
        Campaign campaign = await SeedCampaignAsync(ctx);
        EncounterService service = CreateService(ctx);
        ChronicleNpc npc = await SeedChronicleNpcAsync(
            ctx,
            campaign.Id,
            "{\"Wits\":3,\"Composure\":4,\"Strength\":2,\"Dexterity\":2,\"Stamina\":2,\"Intelligence\":2,\"Resolve\":2,\"Presence\":2,\"Manipulation\":2}");

        ChronicleNpcEncounterPrepDto? dto = await service.GetChronicleNpcEncounterPrepAsync(npc.Id, "st-1");

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
        ApplicationDbContext ctx = CreateContext(nameof(GetChronicleNpcEncounterPrepAsync_Vampire_SuggestsVitaeFromBloodPotency));
        Campaign campaign = await SeedCampaignAsync(ctx);
        EncounterService service = CreateService(ctx);
        ChronicleNpc npc = await SeedChronicleNpcAsync(
            ctx,
            campaign.Id,
            "{\"Wits\":2,\"Composure\":2,\"Strength\":2,\"Dexterity\":2,\"Stamina\":2,\"Intelligence\":2,\"Resolve\":2,\"Presence\":2,\"Manipulation\":2,\"BloodPotency\":3}",
            creatureType: CreatureType.Vampire);

        ChronicleNpcEncounterPrepDto? dto = await service.GetChronicleNpcEncounterPrepAsync(npc.Id, "st-1");

        Assert.NotNull(dto);
        Assert.True(dto.TracksVitae);
        Assert.Equal(12, dto.SuggestedMaxVitae);
    }

    [Fact]
    public async Task GetChronicleNpcEncounterPrepAsync_UsesLinkedStatBlockHealth()
    {
        ApplicationDbContext ctx = CreateContext(nameof(GetChronicleNpcEncounterPrepAsync_UsesLinkedStatBlockHealth));
        Campaign campaign = await SeedCampaignAsync(ctx);
        EncounterService service = CreateService(ctx);
        NpcStatBlock block = await SeedNpcStatBlockAsync(ctx, campaign.Id, "Thug template", health: 11);
        ChronicleNpc npc = await SeedChronicleNpcAsync(
            ctx,
            campaign.Id,
            "{\"Wits\":2,\"Composure\":2,\"Strength\":2,\"Dexterity\":2,\"Stamina\":2,\"Intelligence\":2,\"Resolve\":2,\"Presence\":2,\"Manipulation\":2}",
            linkedStatBlockId: block.Id);

        ChronicleNpcEncounterPrepDto? dto = await service.GetChronicleNpcEncounterPrepAsync(npc.Id, "st-1");

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
        ApplicationDbContext ctx = CreateContext(nameof(AddNpcTemplateFromChronicleNpcAsync_Throws_WhenNpcInOtherCampaign));
        Campaign alpha = await SeedCampaignAsync(ctx, "st-1");
        Campaign beta = new() { Name = "Other city", StoryTellerId = "st-1" };
        ctx.Campaigns.Add(beta);
        await ctx.SaveChangesAsync();

        EncounterService service = CreateService(ctx);
        CombatEncounter draft = await service.CreateDraftEncounterAsync(alpha.Id, "Fight", "st-1");
        ChronicleNpc npc = await SeedChronicleNpcAsync(
            ctx,
            beta.Id,
            "{\"Wits\":2,\"Composure\":2,\"Strength\":2,\"Dexterity\":2,\"Stamina\":2,\"Intelligence\":2,\"Resolve\":2,\"Presence\":2,\"Manipulation\":2}");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AddNpcTemplateFromChronicleNpcAsync(draft.Id, npc.Id, 0, 7, 4, 0, true, null, "st-1"));
    }

    [Fact]
    public async Task AddNpcTemplateFromChronicleNpcAsync_Throws_WhenChronicleNpcAlreadyInPrep()
    {
        ApplicationDbContext ctx = CreateContext(nameof(AddNpcTemplateFromChronicleNpcAsync_Throws_WhenChronicleNpcAlreadyInPrep));
        Campaign campaign = await SeedCampaignAsync(ctx);
        EncounterService service = CreateService(ctx);
        CombatEncounter draft = await service.CreateDraftEncounterAsync(campaign.Id, "Prep", "st-1");
        ChronicleNpc npc = await SeedChronicleNpcAsync(
            ctx,
            campaign.Id,
            "{\"Wits\":2,\"Composure\":2,\"Strength\":2,\"Dexterity\":2,\"Stamina\":2,\"Intelligence\":2,\"Resolve\":2,\"Presence\":2,\"Manipulation\":2}");

        await service.AddNpcTemplateFromChronicleNpcAsync(draft.Id, npc.Id, 1, 7, 4, 0, true, null, "st-1");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AddNpcTemplateFromChronicleNpcAsync(draft.Id, npc.Id, 1, 7, 4, 0, true, null, "st-1"));
    }

    [Fact]
    public async Task AddNpcTemplateFromChronicleNpcAsync_AddsRow_WithNpcName()
    {
        ApplicationDbContext ctx = CreateContext(nameof(AddNpcTemplateFromChronicleNpcAsync_AddsRow_WithNpcName));
        Campaign campaign = await SeedCampaignAsync(ctx);
        EncounterService service = CreateService(ctx);
        CombatEncounter draft = await service.CreateDraftEncounterAsync(campaign.Id, "Prep", "st-1");
        ChronicleNpc npc = await SeedChronicleNpcAsync(
            ctx,
            campaign.Id,
            "{\"Wits\":2,\"Composure\":2,\"Strength\":2,\"Dexterity\":2,\"Stamina\":2,\"Intelligence\":2,\"Resolve\":2,\"Presence\":2,\"Manipulation\":2}",
            name: "Elena Vance");

        await service.AddNpcTemplateFromChronicleNpcAsync(draft.Id, npc.Id, initiativeMod: 5, healthBoxes: 8, maxWillpower: 6, maxVitae: 0, true, null, "st-1");

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
        ApplicationDbContext ctx = CreateContext(nameof(AddNpcToEncounterFromChronicleNpcAsync_AddsInitiativeRow));
        Campaign campaign = await SeedCampaignAsync(ctx);
        EncounterService service = CreateService(ctx);
        int encounterId = await CreateLaunchedEmptyEncounterAsync(service, campaign.Id, "st-1");
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
        ApplicationDbContext ctx = CreateContext(nameof(AddNpcToEncounterFromChronicleNpcAsync_Throws_WhenChronicleNpcAlreadyInFight));
        Campaign campaign = await SeedCampaignAsync(ctx);
        EncounterService service = CreateService(ctx);
        int encounterId = await CreateLaunchedEmptyEncounterAsync(service, campaign.Id, "st-1");
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
        ApplicationDbContext ctx = CreateContext(nameof(LaunchEncounterAsync_CopiesTemplateWillpowerToInitiative));
        Campaign campaign = await SeedCampaignAsync(ctx);
        EncounterService service = CreateService(ctx);
        CombatEncounter draft = await service.CreateDraftEncounterAsync(campaign.Id, "Ambush", "st-1");
        await service.AddNpcTemplateAsync(draft.Id, "Hunter", 2, 9, 5, null, true, null, "st-1");

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
        ApplicationDbContext ctx = CreateContext(nameof(LaunchEncounterAsync_CopiesChronicleNpcIdAndVitaeFromTemplate));
        Campaign campaign = await SeedCampaignAsync(ctx);
        EncounterService service = CreateService(ctx);
        CombatEncounter draft = await service.CreateDraftEncounterAsync(campaign.Id, "Night", "st-1");
        ChronicleNpc npc = await SeedChronicleNpcAsync(
            ctx,
            campaign.Id,
            "{\"Wits\":2,\"Composure\":2,\"Strength\":2,\"Dexterity\":2,\"Stamina\":2,\"Intelligence\":2,\"Resolve\":2,\"Presence\":2,\"Manipulation\":2}",
            creatureType: CreatureType.Vampire);

        await service.AddNpcTemplateFromChronicleNpcAsync(
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
        ApplicationDbContext ctx = CreateContext(nameof(SpendNpcWillpowerAsync_DecrementsCurrent));
        Campaign campaign = await SeedCampaignAsync(ctx);
        EncounterService service = CreateService(ctx);
        int encounterId = await CreateLaunchedEmptyEncounterAsync(service, campaign.Id, "st-1");
        await service.AddNpcToEncounterAsync(encounterId, "Wolf", 1, 5, "st-1", npcHealthBoxes: 7, npcMaxWillpower: 3);

        InitiativeEntry? before = await ctx.InitiativeEntries.FirstOrDefaultAsync(i => i.EncounterId == encounterId);
        Assert.NotNull(before);
        int entryId = before.Id;

        await service.SpendNpcWillpowerAsync(entryId, "st-1");

        InitiativeEntry? after = await ctx.InitiativeEntries.FindAsync(entryId);
        Assert.NotNull(after);
        Assert.Equal(2, after.NpcCurrentWillpower);
    }

    [Fact]
    public async Task SpendNpcVitaeAsync_DecrementsCurrent()
    {
        ApplicationDbContext ctx = CreateContext(nameof(SpendNpcVitaeAsync_DecrementsCurrent));
        Campaign campaign = await SeedCampaignAsync(ctx);
        EncounterService service = CreateService(ctx);
        int encounterId = await CreateLaunchedEmptyEncounterAsync(service, campaign.Id, "st-1");
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

        await service.SpendNpcVitaeAsync(entryId, "st-1");

        InitiativeEntry? after = await ctx.InitiativeEntries.FindAsync(entryId);
        Assert.NotNull(after);
        Assert.Equal(3, after.NpcCurrentVitae);
    }
}
