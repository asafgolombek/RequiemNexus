using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RequiemNexus.Application.Services;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using Xunit;

namespace RequiemNexus.Data.Tests;

/// <summary>
/// Integration tests for <see cref="NpcStatBlockService"/> wired to an EF Core InMemory database.
/// Each test creates its own named database for full isolation.
/// </summary>
public class NpcStatBlockServiceTests
{
    private static ApplicationDbContext CreateContext(string dbName)
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ApplicationDbContext(options);
    }

    private static NpcStatBlockService CreateService(ApplicationDbContext ctx, string databaseName)
    {
        IDbContextFactory<ApplicationDbContext> factory = InMemoryApplicationDbContextFactories.ForDatabaseName(databaseName);
        return new(ctx, NullLogger<NpcStatBlockService>.Instance, new AuthorizationHelper(factory, NullLogger<AuthorizationHelper>.Instance));
    }

    private static async Task<Campaign> SeedCampaignAsync(ApplicationDbContext ctx, string stId = "st-1")
    {
        Campaign campaign = new() { Name = "Test Saga", StoryTellerId = stId };
        ctx.Campaigns.Add(campaign);
        await ctx.SaveChangesAsync();
        return campaign;
    }

    private static async Task<NpcStatBlock> SeedPrebuiltAsync(ApplicationDbContext ctx, string name = "Mortal")
    {
        NpcStatBlock block = new()
        {
            Name = name,
            Concept = "Generic mortal",
            Size = 5,
            Health = 7,
            Willpower = 3,
            IsPrebuilt = true,
        };
        ctx.NpcStatBlocks.Add(block);
        await ctx.SaveChangesAsync();
        return block;
    }

    // ── Prebuilt queries ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetPrebuiltBlocks_ReturnsOnlyPrebuilt()
    {
        ApplicationDbContext ctx = CreateContext(nameof(GetPrebuiltBlocks_ReturnsOnlyPrebuilt));
        Campaign campaign = await SeedCampaignAsync(ctx);
        NpcStatBlockService service = CreateService(ctx, nameof(GetPrebuiltBlocks_ReturnsOnlyPrebuilt));

        await SeedPrebuiltAsync(ctx, "Mortal");
        await service.CreateBlockAsync(campaign.Id, "Custom", "C", 5, 7, 3, 0, 0, "{}", "{}", "{}", string.Empty, "st-1");

        List<NpcStatBlock> prebuilt = await service.GetPrebuiltBlocksAsync();
        Assert.Single(prebuilt);
        Assert.Equal("Mortal", prebuilt[0].Name);
    }

    [Fact]
    public async Task GetCampaignBlocks_ReturnsOnlyCustomForCampaign()
    {
        ApplicationDbContext ctx = CreateContext(nameof(GetCampaignBlocks_ReturnsOnlyCustomForCampaign));
        Campaign campaign = await SeedCampaignAsync(ctx);
        NpcStatBlockService service = CreateService(ctx, nameof(GetCampaignBlocks_ReturnsOnlyCustomForCampaign));

        await SeedPrebuiltAsync(ctx);
        await service.CreateBlockAsync(campaign.Id, "Custom", "C", 5, 7, 3, 0, 0, "{}", "{}", "{}", string.Empty, "st-1");

        List<NpcStatBlock> custom = await service.GetCampaignBlocksAsync(campaign.Id);
        Assert.Single(custom);
        Assert.Equal("Custom", custom[0].Name);
    }

    [Fact]
    public async Task GetAvailableBlocks_ReturnsBothPrebuiltAndCustom()
    {
        ApplicationDbContext ctx = CreateContext(nameof(GetAvailableBlocks_ReturnsBothPrebuiltAndCustom));
        Campaign campaign = await SeedCampaignAsync(ctx);
        NpcStatBlockService service = CreateService(ctx, nameof(GetAvailableBlocks_ReturnsBothPrebuiltAndCustom));

        await SeedPrebuiltAsync(ctx, "Mortal");
        await service.CreateBlockAsync(campaign.Id, "Custom NPC", "C", 5, 7, 3, 0, 0, "{}", "{}", "{}", string.Empty, "st-1");

        List<NpcStatBlock> available = await service.GetAvailableBlocksAsync(campaign.Id);
        Assert.Equal(2, available.Count);
    }

    // ── Custom block CRUD ──────────────────────────────────────────────────────

    [Fact]
    public async Task CreateBlock_BySt_Persists()
    {
        ApplicationDbContext ctx = CreateContext(nameof(CreateBlock_BySt_Persists));
        Campaign campaign = await SeedCampaignAsync(ctx);
        NpcStatBlockService service = CreateService(ctx, nameof(CreateBlock_BySt_Persists));

        NpcStatBlock block = await service.CreateBlockAsync(
            campaign.Id, "Enforcer", "Street thug", 5, 7, 3, 0, 0, "{}", "{\"Brawl\":2}", "{}", string.Empty, "st-1");

        NpcStatBlock? loaded = await ctx.NpcStatBlocks.FindAsync(block.Id);
        Assert.NotNull(loaded);
        Assert.Equal("Enforcer", loaded.Name);
        Assert.False(loaded.IsPrebuilt);
        Assert.Equal(campaign.Id, loaded.CampaignId);
    }

    [Fact]
    public async Task CreateBlock_NonSt_ThrowsUnauthorized()
    {
        ApplicationDbContext ctx = CreateContext(nameof(CreateBlock_NonSt_ThrowsUnauthorized));
        Campaign campaign = await SeedCampaignAsync(ctx);
        NpcStatBlockService service = CreateService(ctx, nameof(CreateBlock_NonSt_ThrowsUnauthorized));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.CreateBlockAsync(campaign.Id, "NPC", "C", 5, 7, 3, 0, 0, "{}", "{}", "{}", string.Empty, "not-the-st"));
    }

    [Fact]
    public async Task DeleteBlock_Prebuilt_ThrowsUnauthorized()
    {
        ApplicationDbContext ctx = CreateContext(nameof(DeleteBlock_Prebuilt_ThrowsUnauthorized));
        await SeedCampaignAsync(ctx);
        NpcStatBlockService service = CreateService(ctx, nameof(DeleteBlock_Prebuilt_ThrowsUnauthorized));

        NpcStatBlock prebuilt = await SeedPrebuiltAsync(ctx);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.DeleteBlockAsync(prebuilt.Id, "st-1"));
    }

    [Fact]
    public async Task UpdateBlock_Prebuilt_ThrowsUnauthorized()
    {
        ApplicationDbContext ctx = CreateContext(nameof(UpdateBlock_Prebuilt_ThrowsUnauthorized));
        await SeedCampaignAsync(ctx);
        NpcStatBlockService service = CreateService(ctx, nameof(UpdateBlock_Prebuilt_ThrowsUnauthorized));

        NpcStatBlock prebuilt = await SeedPrebuiltAsync(ctx);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.UpdateBlockAsync(prebuilt.Id, "Name", "C", 5, 7, 3, 0, 0, "{}", "{}", "{}", string.Empty, "st-1"));
    }

    [Fact]
    public async Task DeleteBlock_CustomBySt_RemovesBlock()
    {
        ApplicationDbContext ctx = CreateContext(nameof(DeleteBlock_CustomBySt_RemovesBlock));
        Campaign campaign = await SeedCampaignAsync(ctx);
        NpcStatBlockService service = CreateService(ctx, nameof(DeleteBlock_CustomBySt_RemovesBlock));

        NpcStatBlock block = await service.CreateBlockAsync(
            campaign.Id, "Custom", "C", 5, 7, 3, 0, 0, "{}", "{}", "{}", string.Empty, "st-1");

        await service.DeleteBlockAsync(block.Id, "st-1");

        NpcStatBlock? loaded = await ctx.NpcStatBlocks.FindAsync(block.Id);
        Assert.Null(loaded);
    }
}
