using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RequiemNexus.Application.Services;
using RequiemNexus.Data.Models;
using Xunit;

namespace RequiemNexus.Data.Tests;

/// <summary>
/// Integration tests for character archiving, retirement, and dice macros
/// (Milestone 8 — Player Character Management).
/// </summary>
public class CharacterArchivingTests
{
    private static ApplicationDbContext CreateContext(string dbName)
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ApplicationDbContext(options);
    }

    private static CharacterManagementService CreateService(ApplicationDbContext ctx, string dbName)
    {
        // The factory must target the same InMemory database as ctx so that
        // GetCharactersByUserIdAsync / GetArchivedCharactersAsync see seeded data.
        ServiceCollection services = new();
        services.AddDbContextFactory<ApplicationDbContext>(
            o => o.UseInMemoryDatabase(dbName));
        IDbContextFactory<ApplicationDbContext> factory = services.BuildServiceProvider()
            .GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        BeatLedgerService beatLedger = new(ctx);
        return new CharacterManagementService(ctx, factory, new RequiemNexus.Domain.CharacterCreationRules(), beatLedger);
    }

    private static DiceMacroService CreateDiceMacroService(ApplicationDbContext ctx) => new(ctx);

    private static async Task<(Campaign Campaign, Character Character)> SeedAsync(ApplicationDbContext ctx, string userId = "user-1", string stId = "st-1")
    {
        Campaign campaign = new() { Name = "Saga", StoryTellerId = stId };
        ctx.Campaigns.Add(campaign);

        Character character = new()
        {
            ApplicationUserId = userId,
            Name = "Test Character",
            CampaignId = null,
        };
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();
        return (campaign, character);
    }

    // ── Archiving ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ArchiveCharacter_ByOwner_SetsIsArchivedTrue()
    {
        ApplicationDbContext ctx = CreateContext(nameof(ArchiveCharacter_ByOwner_SetsIsArchivedTrue));
        CharacterManagementService service = CreateService(ctx, nameof(ArchiveCharacter_ByOwner_SetsIsArchivedTrue));
        (_, Character character) = await SeedAsync(ctx);

        await service.ArchiveCharacterAsync(character.Id, "user-1");

        Character? loaded = await ctx.Characters.FindAsync(character.Id);
        Assert.True(loaded!.IsArchived);
        Assert.NotNull(loaded.ArchivedAt);
    }

    [Fact]
    public async Task ArchiveCharacter_NonOwner_ThrowsUnauthorized()
    {
        ApplicationDbContext ctx = CreateContext(nameof(ArchiveCharacter_NonOwner_ThrowsUnauthorized));
        CharacterManagementService service = CreateService(ctx, nameof(ArchiveCharacter_NonOwner_ThrowsUnauthorized));
        (_, Character character) = await SeedAsync(ctx);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.ArchiveCharacterAsync(character.Id, "not-the-owner"));
    }

    [Fact]
    public async Task UnarchiveCharacter_ByOwner_SetsIsArchivedFalse()
    {
        ApplicationDbContext ctx = CreateContext(nameof(UnarchiveCharacter_ByOwner_SetsIsArchivedFalse));
        CharacterManagementService service = CreateService(ctx, nameof(UnarchiveCharacter_ByOwner_SetsIsArchivedFalse));
        (_, Character character) = await SeedAsync(ctx);

        await service.ArchiveCharacterAsync(character.Id, "user-1");
        await service.UnarchiveCharacterAsync(character.Id, "user-1");

        Character? loaded = await ctx.Characters.FindAsync(character.Id);
        Assert.False(loaded!.IsArchived);
        Assert.Null(loaded.ArchivedAt);
    }

    [Fact]
    public async Task GetCharactersByUserId_ExcludesArchived()
    {
        ApplicationDbContext ctx = CreateContext(nameof(GetCharactersByUserId_ExcludesArchived));
        CharacterManagementService service = CreateService(ctx, nameof(GetCharactersByUserId_ExcludesArchived));
        (_, Character character) = await SeedAsync(ctx);

        await service.ArchiveCharacterAsync(character.Id, "user-1");

        List<Character> result = await service.GetCharactersByUserIdAsync("user-1");
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetArchivedCharacters_ReturnsArchivedOnly()
    {
        ApplicationDbContext ctx = CreateContext(nameof(GetArchivedCharacters_ReturnsArchivedOnly));
        CharacterManagementService service = CreateService(ctx, nameof(GetArchivedCharacters_ReturnsArchivedOnly));
        (_, Character character) = await SeedAsync(ctx);

        await service.ArchiveCharacterAsync(character.Id, "user-1");

        List<Character> archived = await service.GetArchivedCharactersAsync("user-1");
        Assert.Single(archived);
        Assert.Equal(character.Id, archived[0].Id);
    }

    // ── Retirement ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task RetireCharacter_ByOwner_SetsIsRetiredTrue()
    {
        ApplicationDbContext ctx = CreateContext(nameof(RetireCharacter_ByOwner_SetsIsRetiredTrue));
        CharacterManagementService service = CreateService(ctx, nameof(RetireCharacter_ByOwner_SetsIsRetiredTrue));
        (Campaign campaign, Character character) = await SeedAsync(ctx);
        character.CampaignId = campaign.Id;
        await ctx.SaveChangesAsync();

        await service.RetireCharacterAsync(character.Id, "user-1");

        Character? loaded = await ctx.Characters.FindAsync(character.Id);
        Assert.True(loaded!.IsRetired);
        Assert.NotNull(loaded.RetiredAt);
    }

    [Fact]
    public async Task RetireCharacter_BySt_Succeeds()
    {
        ApplicationDbContext ctx = CreateContext(nameof(RetireCharacter_BySt_Succeeds));
        CharacterManagementService service = CreateService(ctx, nameof(RetireCharacter_BySt_Succeeds));
        (Campaign campaign, Character character) = await SeedAsync(ctx);
        character.CampaignId = campaign.Id;
        await ctx.SaveChangesAsync();

        await service.RetireCharacterAsync(character.Id, "st-1");

        Character? loaded = await ctx.Characters.FindAsync(character.Id);
        Assert.True(loaded!.IsRetired);
    }

    [Fact]
    public async Task UnretireCharacter_ByOwner_SetsIsRetiredFalse()
    {
        ApplicationDbContext ctx = CreateContext(nameof(UnretireCharacter_ByOwner_SetsIsRetiredFalse));
        CharacterManagementService service = CreateService(ctx, nameof(UnretireCharacter_ByOwner_SetsIsRetiredFalse));
        (Campaign campaign, Character character) = await SeedAsync(ctx);
        character.CampaignId = campaign.Id;
        await ctx.SaveChangesAsync();

        await service.RetireCharacterAsync(character.Id, "user-1");
        await service.UnretireCharacterAsync(character.Id, "user-1");

        Character? loaded = await ctx.Characters.FindAsync(character.Id);
        Assert.False(loaded!.IsRetired);
        Assert.Null(loaded.RetiredAt);
    }

    // ── Dice Macros ────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateDiceMacro_ByOwner_Persists()
    {
        ApplicationDbContext ctx = CreateContext(nameof(CreateDiceMacro_ByOwner_Persists));
        DiceMacroService service = CreateDiceMacroService(ctx);
        (_, Character character) = await SeedAsync(ctx);

        DiceMacro macro = await service.CreateDiceMacroAsync(character.Id, "Dex+Stealth", 5, "Sneak check", "user-1");

        DiceMacro? loaded = await ctx.DiceMacros.FindAsync(macro.Id);
        Assert.NotNull(loaded);
        Assert.Equal("Dex+Stealth", loaded.Name);
        Assert.Equal(5, loaded.DicePool);
    }

    [Fact]
    public async Task CreateDiceMacro_NonOwner_ThrowsUnauthorized()
    {
        ApplicationDbContext ctx = CreateContext(nameof(CreateDiceMacro_NonOwner_ThrowsUnauthorized));
        DiceMacroService service = CreateDiceMacroService(ctx);
        (_, Character character) = await SeedAsync(ctx);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.CreateDiceMacroAsync(character.Id, "Dex+Stealth", 5, string.Empty, "not-owner"));
    }

    [Fact]
    public async Task DeleteDiceMacro_ByOwner_Removes()
    {
        ApplicationDbContext ctx = CreateContext(nameof(DeleteDiceMacro_ByOwner_Removes));
        DiceMacroService service = CreateDiceMacroService(ctx);
        (_, Character character) = await SeedAsync(ctx);

        DiceMacro macro = await service.CreateDiceMacroAsync(character.Id, "Test", 3, string.Empty, "user-1");
        await service.DeleteDiceMacroAsync(macro.Id, "user-1");

        DiceMacro? loaded = await ctx.DiceMacros.FindAsync(macro.Id);
        Assert.Null(loaded);
    }
}
