using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RequiemNexus.Application.DTOs;
using RequiemNexus.Application.Services;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain;
using Xunit;

namespace RequiemNexus.Data.Tests;

/// <summary>
/// Integration tests for <see cref="StorytellerGlimpseService"/> wired to an EF Core InMemory database.
/// Each test creates its own named database for full isolation.
/// </summary>
public class StorytellerGlimpseServiceTests
{
    private static ApplicationDbContext CreateContext(string dbName)
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ApplicationDbContext(options);
    }

    private static StorytellerGlimpseService CreateService(ApplicationDbContext ctx)
    {
        BeatLedgerService ledger = new(ctx);
        CharacterCreationRules creationRules = new();
        return new StorytellerGlimpseService(
            ctx,
            ledger,
            creationRules,
            NullLogger<StorytellerGlimpseService>.Instance);
    }

    private static async Task<(Campaign Campaign, Character Character)> SeedCampaignWithCharacterAsync(
        ApplicationDbContext ctx,
        string stId = "st-1",
        string playerId = "player-1")
    {
        Campaign campaign = new() { Name = "Test Saga", StoryTellerId = stId };
        ctx.Campaigns.Add(campaign);
        await ctx.SaveChangesAsync();

        Character character = new()
        {
            Name = "Varian",
            ApplicationUserId = playerId,
            CampaignId = campaign.Id,
            MaxHealth = 7,
            CurrentHealth = 7,
            MaxWillpower = 5,
            CurrentWillpower = 5,
            MaxVitae = 10,
            CurrentVitae = 8,
            Humanity = 7,
        };
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        return (campaign, character);
    }

    // ── Authorization ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetCampaignVitalsAsync_ThrowsUnauthorized_WhenCallerIsNotStoryteller()
    {
        ApplicationDbContext ctx = CreateContext(nameof(GetCampaignVitalsAsync_ThrowsUnauthorized_WhenCallerIsNotStoryteller));
        (Campaign campaign, _) = await SeedCampaignWithCharacterAsync(ctx);
        StorytellerGlimpseService service = CreateService(ctx);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.GetCampaignVitalsAsync(campaign.Id, "random-user"));
    }

    [Fact]
    public async Task AwardBeatToCharacterAsync_ThrowsUnauthorized_WhenCallerIsNotStoryteller()
    {
        ApplicationDbContext ctx = CreateContext(nameof(AwardBeatToCharacterAsync_ThrowsUnauthorized_WhenCallerIsNotStoryteller));
        (Campaign campaign, Character character) = await SeedCampaignWithCharacterAsync(ctx);
        StorytellerGlimpseService service = CreateService(ctx);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.AwardBeatToCharacterAsync(campaign.Id, character.Id, "Good roleplaying", "random-user"));
    }

    [Fact]
    public async Task AwardBeatToCampaignAsync_ThrowsUnauthorized_WhenCallerIsNotStoryteller()
    {
        ApplicationDbContext ctx = CreateContext(nameof(AwardBeatToCampaignAsync_ThrowsUnauthorized_WhenCallerIsNotStoryteller));
        (Campaign campaign, _) = await SeedCampaignWithCharacterAsync(ctx);
        StorytellerGlimpseService service = CreateService(ctx);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.AwardBeatToCampaignAsync(campaign.Id, "Session end", "random-user"));
    }

    [Fact]
    public async Task AwardXpToCharacterAsync_ThrowsUnauthorized_WhenCallerIsNotStoryteller()
    {
        ApplicationDbContext ctx = CreateContext(nameof(AwardXpToCharacterAsync_ThrowsUnauthorized_WhenCallerIsNotStoryteller));
        (Campaign campaign, Character character) = await SeedCampaignWithCharacterAsync(ctx);
        StorytellerGlimpseService service = CreateService(ctx);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.AwardXpToCharacterAsync(campaign.Id, character.Id, 2, "Bonus XP", "random-user"));
    }

    // ── GetCampaignVitalsAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetCampaignVitalsAsync_ReturnsVitalsForAllCampaignCharacters()
    {
        ApplicationDbContext ctx = CreateContext(nameof(GetCampaignVitalsAsync_ReturnsVitalsForAllCampaignCharacters));
        (Campaign campaign, _) = await SeedCampaignWithCharacterAsync(ctx);

        // Add a second character
        ctx.Characters.Add(new Character
        {
            Name = "Mira",
            ApplicationUserId = "player-2",
            CampaignId = campaign.Id,
            MaxHealth = 6,
            CurrentHealth = 4,
            MaxWillpower = 4,
            CurrentWillpower = 4,
            MaxVitae = 10,
            CurrentVitae = 10,
            Humanity = 6,
        });
        await ctx.SaveChangesAsync();

        StorytellerGlimpseService service = CreateService(ctx);

        List<CharacterVitalsDto> vitals = await service.GetCampaignVitalsAsync(campaign.Id, "st-1");

        Assert.Equal(2, vitals.Count);
    }

    [Fact]
    public async Task GetCampaignVitalsAsync_CountsOnlyActiveConditions()
    {
        ApplicationDbContext ctx = CreateContext(nameof(GetCampaignVitalsAsync_CountsOnlyActiveConditions));
        (Campaign campaign, Character character) = await SeedCampaignWithCharacterAsync(ctx);

        ctx.CharacterConditions.Add(new RequiemNexus.Data.Models.CharacterCondition
        {
            CharacterId = character.Id,
            ConditionType = RequiemNexus.Domain.Enums.ConditionType.Guilty,
            IsResolved = false,
            AwardsBeat = true,
        });
        ctx.CharacterConditions.Add(new RequiemNexus.Data.Models.CharacterCondition
        {
            CharacterId = character.Id,
            ConditionType = RequiemNexus.Domain.Enums.ConditionType.Shaken,
            IsResolved = true,    // already resolved — should not count
            AwardsBeat = true,
        });
        await ctx.SaveChangesAsync();

        StorytellerGlimpseService service = CreateService(ctx);

        List<CharacterVitalsDto> vitals = await service.GetCampaignVitalsAsync(campaign.Id, "st-1");

        CharacterVitalsDto dto = Assert.Single(vitals);
        Assert.Equal(1, dto.ActiveConditionCount);
    }

    // ── AwardBeatToCharacterAsync ────────────────────────────────────────────

    [Fact]
    public async Task AwardBeatToCharacterAsync_IncrementsBeatAndWritesLedgerEntry()
    {
        ApplicationDbContext ctx = CreateContext(nameof(AwardBeatToCharacterAsync_IncrementsBeatAndWritesLedgerEntry));
        (Campaign campaign, Character character) = await SeedCampaignWithCharacterAsync(ctx);
        StorytellerGlimpseService service = CreateService(ctx);

        await service.AwardBeatToCharacterAsync(campaign.Id, character.Id, "Great scene", "st-1");

        Character updated = await ctx.Characters.FindAsync(character.Id) ?? throw new InvalidOperationException();
        Assert.Equal(1, updated.Beats);

        RequiemNexus.Data.Models.BeatLedgerEntry entry = Assert.Single(
            await ctx.BeatLedger.Where(b => b.CharacterId == character.Id).ToListAsync());
        Assert.Equal(RequiemNexus.Domain.Enums.BeatSource.StorytellerAward, entry.Source);
        Assert.Equal("Great scene", entry.Reason);
        Assert.Equal("st-1", entry.AwardedByUserId);
    }

    [Fact]
    public async Task AwardBeatToCharacterAsync_TriggersConversion_WhenFiveBeatsReached()
    {
        ApplicationDbContext ctx = CreateContext(nameof(AwardBeatToCharacterAsync_TriggersConversion_WhenFiveBeatsReached));
        (Campaign campaign, Character character) = await SeedCampaignWithCharacterAsync(ctx);
        character.Beats = 4;   // one more push triggers conversion
        await ctx.SaveChangesAsync();

        StorytellerGlimpseService service = CreateService(ctx);

        await service.AwardBeatToCharacterAsync(campaign.Id, character.Id, "5th Beat", "st-1");

        Character updated = await ctx.Characters.FindAsync(character.Id) ?? throw new InvalidOperationException();
        Assert.Equal(0, updated.Beats);
        Assert.Equal(1, updated.ExperiencePoints);

        // Should have both a Beat entry and an XP conversion entry
        List<RequiemNexus.Data.Models.BeatLedgerEntry> beats = await ctx.BeatLedger.Where(b => b.CharacterId == character.Id).ToListAsync();
        List<RequiemNexus.Data.Models.XpLedgerEntry> xp = await ctx.XpLedger.Where(x => x.CharacterId == character.Id).ToListAsync();
        Assert.Single(beats);
        Assert.Single(xp);
        Assert.Equal(RequiemNexus.Domain.Enums.XpSource.BeatConversion, xp[0].Source);
    }

    // ── AwardBeatToCampaignAsync ─────────────────────────────────────────────

    [Fact]
    public async Task AwardBeatToCampaignAsync_WritesLedgerEntryForEachCharacter()
    {
        ApplicationDbContext ctx = CreateContext(nameof(AwardBeatToCampaignAsync_WritesLedgerEntryForEachCharacter));
        (Campaign campaign, _) = await SeedCampaignWithCharacterAsync(ctx, playerId: "player-1");

        ctx.Characters.Add(new Character
        {
            Name = "Mira",
            ApplicationUserId = "player-2",
            CampaignId = campaign.Id,
            MaxHealth = 6,
            CurrentHealth = 6,
            MaxWillpower = 4,
            CurrentWillpower = 4,
            MaxVitae = 10,
            CurrentVitae = 10,
            Humanity = 6,
        });
        await ctx.SaveChangesAsync();

        StorytellerGlimpseService service = CreateService(ctx);

        await service.AwardBeatToCampaignAsync(campaign.Id, "Session end", "st-1");

        List<RequiemNexus.Data.Models.BeatLedgerEntry> entries = await ctx.BeatLedger.ToListAsync();
        Assert.Equal(2, entries.Count);
        Assert.All(entries, e =>
        {
            Assert.Equal(RequiemNexus.Domain.Enums.BeatSource.StorytellerAward, e.Source);
            Assert.Equal("Session end", e.Reason);
        });
    }

    // ── AwardXpToCharacterAsync ──────────────────────────────────────────────

    [Fact]
    public async Task AwardXpToCharacterAsync_IncrementsXpAndWritesLedgerEntry()
    {
        ApplicationDbContext ctx = CreateContext(nameof(AwardXpToCharacterAsync_IncrementsXpAndWritesLedgerEntry));
        (Campaign campaign, Character character) = await SeedCampaignWithCharacterAsync(ctx);
        StorytellerGlimpseService service = CreateService(ctx);

        await service.AwardXpToCharacterAsync(campaign.Id, character.Id, 3, "Bonus XP for great story", "st-1");

        Character updated = await ctx.Characters.FindAsync(character.Id) ?? throw new InvalidOperationException();
        Assert.Equal(3, updated.ExperiencePoints);
        Assert.Equal(3, updated.TotalExperiencePoints);

        RequiemNexus.Data.Models.XpLedgerEntry entry = Assert.Single(
            await ctx.XpLedger.Where(x => x.CharacterId == character.Id).ToListAsync());
        Assert.Equal(3, entry.Delta);
        Assert.Equal(RequiemNexus.Domain.Enums.XpSource.StorytellerAward, entry.Source);
    }

    [Fact]
    public async Task AwardXpToCharacterAsync_ThrowsArgumentOutOfRange_WhenAmountIsZero()
    {
        ApplicationDbContext ctx = CreateContext(nameof(AwardXpToCharacterAsync_ThrowsArgumentOutOfRange_WhenAmountIsZero));
        (Campaign campaign, Character character) = await SeedCampaignWithCharacterAsync(ctx);
        StorytellerGlimpseService service = CreateService(ctx);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.AwardXpToCharacterAsync(campaign.Id, character.Id, 0, "Zero XP", "st-1"));
    }
}
