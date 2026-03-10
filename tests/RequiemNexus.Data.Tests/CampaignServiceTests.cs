using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RequiemNexus.Application.Services;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain;
using Xunit;

namespace RequiemNexus.Data.Tests;

/// <summary>
/// Integration tests for <see cref="CampaignService"/> wired to an EF Core InMemory database.
/// Each test creates its own database instance for full isolation.
/// </summary>
public class CampaignServiceTests
{
    private static ApplicationDbContext CreateContext(string dbName)
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ApplicationDbContext(options);
    }

    private static CampaignService CreateService(ApplicationDbContext ctx)
        => new(ctx, NullLogger<CampaignService>.Instance);

    // -----------------------------------------------------------------------
    // IsStoryteller
    // -----------------------------------------------------------------------

    [Fact]
    public void IsStoryteller_ReturnsTrue_WhenUserIsStoryteller()
    {
        Campaign campaign = new() { Name = "Test", StoryTellerId = "st-1" };
        CampaignService service = CreateService(CreateContext(nameof(IsStoryteller_ReturnsTrue_WhenUserIsStoryteller)));

        Assert.True(service.IsStoryteller(campaign, "st-1"));
    }

    [Fact]
    public void IsStoryteller_ReturnsFalse_WhenUserIsNotStoryteller()
    {
        Campaign campaign = new() { Name = "Test", StoryTellerId = "st-1" };
        CampaignService service = CreateService(CreateContext(nameof(IsStoryteller_ReturnsFalse_WhenUserIsNotStoryteller)));

        Assert.False(service.IsStoryteller(campaign, "player-1"));
    }

    // -----------------------------------------------------------------------
    // IsCampaignMember
    // -----------------------------------------------------------------------

    [Fact]
    public void IsCampaignMember_ReturnsTrue_ForStoryteller()
    {
        Campaign campaign = new() { Name = "Test", StoryTellerId = "st-1" };
        CampaignService service = CreateService(CreateContext(nameof(IsCampaignMember_ReturnsTrue_ForStoryteller)));

        Assert.True(service.IsCampaignMember(campaign, "st-1"));
    }

    [Fact]
    public void IsCampaignMember_ReturnsTrue_ForEnrolledPlayer()
    {
        Campaign campaign = new()
        {
            Name = "Test",
            StoryTellerId = "st-1",
            Characters = [new Character { ApplicationUserId = "player-1", Name = "V" }],
        };
        CampaignService service = CreateService(CreateContext(nameof(IsCampaignMember_ReturnsTrue_ForEnrolledPlayer)));

        Assert.True(service.IsCampaignMember(campaign, "player-1"));
    }

    [Fact]
    public void IsCampaignMember_ReturnsFalse_ForOutsider()
    {
        Campaign campaign = new()
        {
            Name = "Test",
            StoryTellerId = "st-1",
            Characters = [new Character { ApplicationUserId = "player-1", Name = "V" }],
        };
        CampaignService service = CreateService(CreateContext(nameof(IsCampaignMember_ReturnsFalse_ForOutsider)));

        Assert.False(service.IsCampaignMember(campaign, "outsider-99"));
    }

    // -----------------------------------------------------------------------
    // CreateCampaignAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task CreateCampaignAsync_PersistsCampaignWithCorrectStoryteller()
    {
        using ApplicationDbContext ctx = CreateContext(nameof(CreateCampaignAsync_PersistsCampaignWithCorrectStoryteller));
        CampaignService service = CreateService(ctx);

        Campaign created = await service.CreateCampaignAsync("Blood & Shadow", "A dark tale", "st-1");

        Campaign? persisted = await ctx.Campaigns.FindAsync(created.Id);
        Assert.NotNull(persisted);
        Assert.Equal("Blood & Shadow", persisted.Name);
        Assert.Equal("st-1", persisted.StoryTellerId);
    }

    // -----------------------------------------------------------------------
    // AddCharacterToCampaignAsync — ownership enforcement
    // -----------------------------------------------------------------------

    [Fact]
    public async Task AddCharacter_ByStoryteller_Succeeds()
    {
        using ApplicationDbContext ctx = CreateContext(nameof(AddCharacter_ByStoryteller_Succeeds));
        Campaign campaign = new() { Name = "C", StoryTellerId = "st-1" };
        Character character = new() { ApplicationUserId = "player-1", Name = "V" };
        ctx.Campaigns.Add(campaign);
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        CampaignService service = CreateService(ctx);
        await service.AddCharacterToCampaignAsync(campaign.Id, character.Id, "st-1");

        Character? updated = await ctx.Characters.FindAsync(character.Id);
        Assert.Equal(campaign.Id, updated!.CampaignId);
    }

    [Fact]
    public async Task AddCharacter_ByOwner_Succeeds()
    {
        using ApplicationDbContext ctx = CreateContext(nameof(AddCharacter_ByOwner_Succeeds));
        Campaign campaign = new() { Name = "C", StoryTellerId = "st-1" };
        Character character = new() { ApplicationUserId = "player-1", Name = "V" };
        ctx.Campaigns.Add(campaign);
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        CampaignService service = CreateService(ctx);
        await service.AddCharacterToCampaignAsync(campaign.Id, character.Id, "player-1");

        Character? updated = await ctx.Characters.FindAsync(character.Id);
        Assert.Equal(campaign.Id, updated!.CampaignId);
    }

    [Fact]
    public async Task AddCharacter_ByUnauthorizedUser_ThrowsUnauthorizedAccessException()
    {
        using ApplicationDbContext ctx = CreateContext(nameof(AddCharacter_ByUnauthorizedUser_ThrowsUnauthorizedAccessException));
        Campaign campaign = new() { Name = "C", StoryTellerId = "st-1" };
        Character character = new() { ApplicationUserId = "player-1", Name = "V" };
        ctx.Campaigns.Add(campaign);
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        CampaignService service = CreateService(ctx);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.AddCharacterToCampaignAsync(campaign.Id, character.Id, "outsider-99"));
    }

    // -----------------------------------------------------------------------
    // GetCharacterWithAccessCheckAsync — access levels
    // -----------------------------------------------------------------------

    [Fact]
    public async Task AccessCheck_Owner_ReturnsIsOwnerTrue()
    {
        using ApplicationDbContext ctx = CreateContext(nameof(AccessCheck_Owner_ReturnsIsOwnerTrue));
        Campaign campaign = new() { Name = "C", StoryTellerId = "st-1" };
        ctx.Campaigns.Add(campaign);
        await ctx.SaveChangesAsync();

        Character character = new() { ApplicationUserId = "player-1", Name = "V", CampaignId = campaign.Id };
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        CharacterManagementService charService = new(ctx, new CharacterCreationRules(), new BeatLedgerService(ctx));

        (Character Character, bool IsOwner)? result = await charService.GetCharacterWithAccessCheckAsync(character.Id, "player-1");

        Assert.NotNull(result);
        Assert.True(result!.Value.IsOwner);
    }

    [Fact]
    public async Task AccessCheck_Storyteller_ReturnsIsOwnerFalse()
    {
        using ApplicationDbContext ctx = CreateContext(nameof(AccessCheck_Storyteller_ReturnsIsOwnerFalse));
        Campaign campaign = new() { Name = "C", StoryTellerId = "st-1" };
        ctx.Campaigns.Add(campaign);
        await ctx.SaveChangesAsync();

        Character character = new() { ApplicationUserId = "player-1", Name = "V", CampaignId = campaign.Id };
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        CharacterManagementService charService = new(ctx, new CharacterCreationRules(), new BeatLedgerService(ctx));

        (Character Character, bool IsOwner)? result = await charService.GetCharacterWithAccessCheckAsync(character.Id, "st-1");

        Assert.NotNull(result);
        Assert.False(result!.Value.IsOwner);
    }

    [Fact]
    public async Task AccessCheck_FellowPlayer_ReturnsIsOwnerFalse()
    {
        using ApplicationDbContext ctx = CreateContext(nameof(AccessCheck_FellowPlayer_ReturnsIsOwnerFalse));
        Campaign campaign = new() { Name = "C", StoryTellerId = "st-1" };
        ctx.Campaigns.Add(campaign);
        await ctx.SaveChangesAsync();

        Character character1 = new() { ApplicationUserId = "player-1", Name = "V1", CampaignId = campaign.Id };
        Character character2 = new() { ApplicationUserId = "player-2", Name = "V2", CampaignId = campaign.Id };
        ctx.Characters.AddRange(character1, character2);
        await ctx.SaveChangesAsync();

        CharacterManagementService charService = new(ctx, new CharacterCreationRules(), new BeatLedgerService(ctx));

        // player-2 views player-1's sheet
        (Character Character, bool IsOwner)? result = await charService.GetCharacterWithAccessCheckAsync(character1.Id, "player-2");

        Assert.NotNull(result);
        Assert.False(result!.Value.IsOwner);
    }

    [Fact]
    public async Task AccessCheck_Outsider_ReturnsNull()
    {
        using ApplicationDbContext ctx = CreateContext(nameof(AccessCheck_Outsider_ReturnsNull));
        Campaign campaign = new() { Name = "C", StoryTellerId = "st-1" };
        ctx.Campaigns.Add(campaign);
        await ctx.SaveChangesAsync();

        Character character = new() { ApplicationUserId = "player-1", Name = "V", CampaignId = campaign.Id };
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        CharacterManagementService charService = new(ctx, new CharacterCreationRules(), new BeatLedgerService(ctx));

        (Character Character, bool IsOwner)? result = await charService.GetCharacterWithAccessCheckAsync(character.Id, "outsider-99");

        Assert.Null(result);
    }
}
