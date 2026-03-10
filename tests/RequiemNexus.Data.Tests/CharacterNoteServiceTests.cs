using Microsoft.EntityFrameworkCore;
using RequiemNexus.Application.Services;
using RequiemNexus.Data.Models;
using Xunit;

namespace RequiemNexus.Data.Tests;

/// <summary>
/// Integration tests for <see cref="CharacterNoteService"/> — visibility rules and
/// author-scoped mutations.
/// </summary>
public class CharacterNoteServiceTests
{
    private static ApplicationDbContext CreateContext(string dbName)
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ApplicationDbContext(options);
    }

    private static CharacterNoteService CreateService(ApplicationDbContext ctx) => new(ctx);

    private static async Task<(Campaign Campaign, Character Character)> SeedAsync(ApplicationDbContext ctx, string userId = "player-1", string stId = "st-1")
    {
        Campaign campaign = new() { Name = "Saga", StoryTellerId = stId };
        ctx.Campaigns.Add(campaign);

        Character character = new() { ApplicationUserId = userId, Name = "Test" };
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        character.CampaignId = campaign.Id;
        await ctx.SaveChangesAsync();
        return (campaign, character);
    }

    // ── Visibility ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetNotes_PlayerCannotSeeStPrivateNotes()
    {
        ApplicationDbContext ctx = CreateContext(nameof(GetNotes_PlayerCannotSeeStPrivateNotes));
        CharacterNoteService service = CreateService(ctx);
        (Campaign campaign, Character character) = await SeedAsync(ctx);

        await service.CreateNoteAsync(character.Id, campaign.Id, "Private ST Note", "Body", isStorytellerPrivate: true, authorUserId: "st-1");
        await service.CreateNoteAsync(character.Id, campaign.Id, "Player Note", "Body", isStorytellerPrivate: false, authorUserId: "player-1");

        List<CharacterNote> notes = await service.GetNotesAsync(character.Id, "player-1");
        Assert.Single(notes);
        Assert.Equal("Player Note", notes[0].Title);
    }

    [Fact]
    public async Task GetNotes_StCanSeePrivateNotes()
    {
        ApplicationDbContext ctx = CreateContext(nameof(GetNotes_StCanSeePrivateNotes));
        CharacterNoteService service = CreateService(ctx);
        (Campaign campaign, Character character) = await SeedAsync(ctx);

        await service.CreateNoteAsync(character.Id, campaign.Id, "Private ST Note", "Body", isStorytellerPrivate: true, authorUserId: "st-1");
        await service.CreateNoteAsync(character.Id, campaign.Id, "Player Note", "Body", isStorytellerPrivate: false, authorUserId: "player-1");

        List<CharacterNote> notes = await service.GetNotesAsync(character.Id, "st-1");
        Assert.Equal(2, notes.Count);
    }

    // ── Create ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateNote_NonSt_CannotCreatePrivateNote()
    {
        ApplicationDbContext ctx = CreateContext(nameof(CreateNote_NonSt_CannotCreatePrivateNote));
        CharacterNoteService service = CreateService(ctx);
        (Campaign campaign, Character character) = await SeedAsync(ctx);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.CreateNoteAsync(character.Id, campaign.Id, "Sneaky", "Body", isStorytellerPrivate: true, authorUserId: "player-1"));
    }

    [Fact]
    public async Task CreateNote_PlayerNote_Persists()
    {
        ApplicationDbContext ctx = CreateContext(nameof(CreateNote_PlayerNote_Persists));
        CharacterNoteService service = CreateService(ctx);
        (Campaign campaign, Character character) = await SeedAsync(ctx);

        CharacterNote note = await service.CreateNoteAsync(character.Id, campaign.Id, "My Note", "Content", false, "player-1");

        CharacterNote? loaded = await ctx.CharacterNotes.FindAsync(note.Id);
        Assert.NotNull(loaded);
        Assert.Equal("My Note", loaded.Title);
    }

    // ── Delete ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteNote_ByAuthor_Removes()
    {
        ApplicationDbContext ctx = CreateContext(nameof(DeleteNote_ByAuthor_Removes));
        CharacterNoteService service = CreateService(ctx);
        (Campaign campaign, Character character) = await SeedAsync(ctx);

        CharacterNote note = await service.CreateNoteAsync(character.Id, campaign.Id, "Note", "Body", false, "player-1");
        await service.DeleteNoteAsync(note.Id, "player-1");

        CharacterNote? loaded = await ctx.CharacterNotes.FindAsync(note.Id);
        Assert.Null(loaded);
    }

    [Fact]
    public async Task DeleteNote_ByNonAuthorNonSt_ThrowsUnauthorized()
    {
        ApplicationDbContext ctx = CreateContext(nameof(DeleteNote_ByNonAuthorNonSt_ThrowsUnauthorized));
        CharacterNoteService service = CreateService(ctx);
        (Campaign campaign, Character character) = await SeedAsync(ctx);

        CharacterNote note = await service.CreateNoteAsync(character.Id, campaign.Id, "Note", "Body", false, "player-1");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.DeleteNoteAsync(note.Id, "some-other-user"));
    }
}
