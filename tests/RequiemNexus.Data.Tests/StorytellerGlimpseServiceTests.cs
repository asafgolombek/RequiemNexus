using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.DTOs;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Application.Services;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Contracts;
using Xunit;

namespace RequiemNexus.Data.Tests;

public class StorytellerGlimpseServiceTests
{
    private static StorytellerGlimpseService CreateService(ApplicationDbContext ctx)
    {
        var beatLedger = new Mock<IBeatLedgerService>().Object;
        var creationRules = new Mock<ICharacterCreationRules>().Object;
        var logger = new Mock<ILogger<StorytellerGlimpseService>>().Object;

        return new StorytellerGlimpseService(ctx, beatLedger, creationRules, logger, new Mock<ISessionService>().Object);
    }

    private static ApplicationDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GetCampaignVitalsAsync_ReturnsAllCharacters()
    {
        // Arrange
        using var ctx = CreateContext(nameof(GetCampaignVitalsAsync_ReturnsAllCharacters));
        var service = CreateService(ctx);

        ctx.Campaigns.Add(new Campaign { Id = 1, Name = "Test", StoryTellerId = "st" });
        ctx.Characters.Add(new Character { Id = 1, Name = "Char 1", CampaignId = 1, ApplicationUserId = "u1" });
        ctx.Characters.Add(new Character { Id = 2, Name = "Char 2", CampaignId = 1, ApplicationUserId = "u2" });
        await ctx.SaveChangesAsync();

        // Act
        var vitals = await service.GetCampaignVitalsAsync(1, "st");

        // Assert
        Assert.Equal(2, vitals.Count);
        Assert.Contains(vitals, v => v.Name == "Char 1");
        Assert.Contains(vitals, v => v.Name == "Char 2");
    }
}
