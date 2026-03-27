using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using RequiemNexus.Application.Services;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Enums;
using Xunit;

namespace RequiemNexus.Data.Tests;

public class BeatLedgerServiceTests
{
    private static ApplicationDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task RecordBeatAsync_CreatesEntry()
    {
        // Arrange
        using var ctx = CreateContext(nameof(RecordBeatAsync_CreatesEntry));
        var service = new BeatLedgerService(ctx);

        // Act
        await service.RecordBeatAsync(1, 10, BeatSource.ManualAdjustment, "Test Beat", "st-user");

        // Assert
        var entry = await ctx.BeatLedger.FirstAsync();
        Assert.Equal(1, entry.CharacterId);
        Assert.Equal(10, entry.CampaignId);
        Assert.Equal(BeatSource.ManualAdjustment, entry.Source);
        Assert.Equal("Test Beat", entry.Reason);
        Assert.Equal("st-user", entry.AwardedByUserId);
    }

    [Fact]
    public async Task RecordXpCreditAsync_CreatesEntry()
    {
        // Arrange
        using var ctx = CreateContext(nameof(RecordXpCreditAsync_CreatesEntry));
        var service = new BeatLedgerService(ctx);

        // Act
        await service.RecordXpCreditAsync(1, 10, 2, XpSource.StorytellerAward, "Story Award", "st-user");

        // Assert
        var entry = await ctx.XpLedger.FirstAsync();
        Assert.Equal(1, entry.CharacterId);
        Assert.Equal(10, entry.CampaignId);
        Assert.Equal(2, entry.Delta);
        Assert.Equal(XpSource.StorytellerAward, entry.Source);
        Assert.Equal("Story Award", entry.Reason);
    }
}
