using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Application.Services;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using Xunit;

namespace RequiemNexus.Application.Tests;

/// <summary>
/// Tests for <see cref="CoilService"/> — pending Chosen Mystery listing and Storyteller authorization.
/// </summary>
public class CoilServiceTests
{
    private static Mock<IAuthorizationHelper> CreatePermissiveAuthMock()
    {
        var authHelper = new Mock<IAuthorizationHelper>();
        authHelper
            .Setup(a => a.RequireCharacterOwnerAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        authHelper
            .Setup(a => a.RequireStorytellerAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        return authHelper;
    }

    private static CoilService CreateService(ApplicationDbContext ctx, IAuthorizationHelper? authHelper = null)
    {
        var auth = authHelper ?? CreatePermissiveAuthMock().Object;
        var beatLedger = new Mock<IBeatLedgerService>();
        var sessionService = new Mock<ISessionService>();
        sessionService
            .Setup(s => s.BroadcastCharacterUpdateAsync(It.IsAny<int>()))
            .Returns(Task.CompletedTask);
        var logger = new Mock<ILogger<CoilService>>().Object;
        return new CoilService(ctx, auth, beatLedger.Object, sessionService.Object, logger);
    }

    private static ApplicationDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GetPendingChosenMysteryApplicationsAsync_ReturnsPendingRow()
    {
        using var ctx = CreateContext(nameof(GetPendingChosenMysteryApplicationsAsync_ReturnsPendingRow));
        ctx.Campaigns.Add(new Campaign { Id = 1, Name = "Test Campaign", StoryTellerId = "st" });
        ctx.Clans.Add(new Clan { Id = 1, Name = "Daeva" });
        ctx.ScaleDefinitions.Add(new ScaleDefinition
        {
            Id = 1,
            Name = "Coil of the Ascendant",
            Description = string.Empty,
            MysteryName = "Mystery of the Ascendant",
            MaxLevel = 5,
        });
        ctx.Characters.Add(new Character
        {
            Id = 1,
            Name = "Neo",
            ApplicationUserId = "player",
            ClanId = 1,
            CampaignId = 1,
            BloodPotency = 1,
            PendingChosenMysteryScaleId = 1,
        });
        await ctx.SaveChangesAsync();

        var sut = CreateService(ctx);
        var list = await sut.GetPendingChosenMysteryApplicationsAsync(1, "st");

        Assert.Single(list);
        Assert.Equal(1, list[0].CharacterId);
        Assert.Equal("Neo", list[0].CharacterName);
        Assert.Equal(1, list[0].ScaleId);
        Assert.Equal("Coil of the Ascendant", list[0].ScaleName);
    }

    [Fact]
    public async Task GetPendingChosenMysteryApplicationsAsync_NotStoryteller_Throws()
    {
        using var ctx = CreateContext(nameof(GetPendingChosenMysteryApplicationsAsync_NotStoryteller_Throws));
        var authHelper = new Mock<IAuthorizationHelper>();
        authHelper
            .Setup(a => a.RequireStorytellerAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new UnauthorizedAccessException("denied"));

        var sut = CreateService(ctx, authHelper.Object);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            sut.GetPendingChosenMysteryApplicationsAsync(1, "not-st"));
    }
}
