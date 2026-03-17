using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Application.Services;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using Xunit;

namespace RequiemNexus.Data.Tests;

public class CampaignServiceTests
{
    private static CampaignService CreateCampaignService(ApplicationDbContext ctx)
    {
        var logger = new Mock<ILogger<CampaignService>>().Object;
        var authHelper = new Mock<IAuthorizationHelper>().Object;

        ServiceCollection services = new();
        services.AddDbContextFactory<ApplicationDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        var factory = services.BuildServiceProvider().GetRequiredService<IDbContextFactory<ApplicationDbContext>>();

        return new CampaignService(ctx, factory, logger, authHelper, new Mock<ISessionService>().Object);
    }

    private static CharacterManagementService CreateCharacterService(ApplicationDbContext ctx)
    {
        ServiceCollection services = new();
        services.AddDbContextFactory<ApplicationDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        var factory = services.BuildServiceProvider().GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        return new(ctx, factory, new RequiemNexus.Domain.CharacterCreationRules(), new BeatLedgerService(ctx), new Mock<ISessionService>().Object);
    }

    private static ApplicationDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CreateCampaignAsync_SetsFieldsCorrectly()
    {
        // Arrange
        using var ctx = CreateContext(nameof(CreateCampaignAsync_SetsFieldsCorrectly));
        var service = CreateCampaignService(ctx);

        // Act
        var campaign = await service.CreateCampaignAsync("New Campaign", "Description", "st-user");

        // Assert
        Assert.Equal("New Campaign", campaign.Name);
        Assert.Equal("Description", campaign.Description);
        Assert.Equal("st-user", campaign.StoryTellerId);
        Assert.True(campaign.IsActive);
    }

    [Fact]
    public async Task EnrollCharacterAsync_UpdatesCharacter()
    {
        // Arrange
        using var ctx = CreateContext(nameof(EnrollCharacterAsync_UpdatesCharacter));
        var service = CreateCampaignService(ctx);

        var campaign = new Campaign { Name = "Test", StoryTellerId = "st" };
        ctx.Campaigns.Add(campaign);

        var character = new Character { Name = "Vamp", ApplicationUserId = "user" };
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        // Act
        await service.AddCharacterToCampaignAsync(campaign.Id, character.Id, "user");

        // Assert
        Assert.Equal(campaign.Id, character.CampaignId);
    }

    [Fact]
    public async Task RemoveCharacterAsync_ClearsCampaignId()
    {
        // Arrange
        using var ctx = CreateContext(nameof(RemoveCharacterAsync_ClearsCampaignId));
        var service = CreateCampaignService(ctx);

        var campaign = new Campaign { Name = "Test", StoryTellerId = "st" };
        ctx.Campaigns.Add(campaign);

        var character = new Character { Name = "Vamp", ApplicationUserId = "user", CampaignId = 1 };
        ctx.Characters.Add(character);
        await ctx.SaveChangesAsync();

        // Act
        await service.RemoveCharacterFromCampaignAsync(campaign.Id, character.Id, "st");

        // Assert
        Assert.Null(character.CampaignId);
    }
}
