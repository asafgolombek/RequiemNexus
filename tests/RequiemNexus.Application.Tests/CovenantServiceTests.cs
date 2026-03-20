using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Application.Services;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.Models.Enums;
using Xunit;

namespace RequiemNexus.Application.Tests;

/// <summary>
/// Tests for <see cref="CovenantService"/> — leave request / approval workflow and authorization.
/// </summary>
public class CovenantServiceTests
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

    private static CovenantService CreateService(ApplicationDbContext ctx, IAuthorizationHelper? authHelper = null)
    {
        var auth = authHelper ?? CreatePermissiveAuthMock().Object;
        var sessionService = new Mock<ISessionService>();
        sessionService
            .Setup(s => s.BroadcastCharacterUpdateAsync(It.IsAny<int>()))
            .Returns(Task.CompletedTask);
        var logger = new Mock<ILogger<CovenantService>>().Object;
        return new CovenantService(ctx, auth, sessionService.Object, logger);
    }

    private static ApplicationDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ApplicationDbContext(options);
    }

    // ── RequestLeaveCovenantAsync ──────────────────────────────────────────────

    [Fact]
    public async Task RequestLeave_SetsLeaveRequestedAt()
    {
        using var ctx = CreateContext(nameof(RequestLeave_SetsLeaveRequestedAt));
        ctx.Campaigns.Add(new Campaign { Id = 1, Name = "Campaign", StoryTellerId = "st" });
        ctx.CovenantDefinitions.Add(new CovenantDefinition { Id = 1, Name = "Invictus", Description = string.Empty, IsPlayable = true });
        ctx.Characters.Add(new Character
        {
            Id = 1,
            Name = "Victor",
            ApplicationUserId = "player",
            CampaignId = 1,
            CovenantId = 1,
            CovenantJoinStatus = null, // already approved member
        });
        await ctx.SaveChangesAsync();

        var sut = CreateService(ctx);
        await sut.RequestLeaveCovenantAsync(1, "player");

        var character = await ctx.Characters.FindAsync(1);
        Assert.NotNull(character!.CovenantLeaveRequestedAt);
        Assert.Equal(1, character.CovenantId); // still in covenant until ST approves
    }

    [Fact]
    public async Task RequestLeave_WhenNotInCovenant_Throws()
    {
        using var ctx = CreateContext(nameof(RequestLeave_WhenNotInCovenant_Throws));
        ctx.Campaigns.Add(new Campaign { Id = 1, Name = "Campaign", StoryTellerId = "st" });
        ctx.Characters.Add(new Character
        {
            Id = 1,
            Name = "Victor",
            ApplicationUserId = "player",
            CampaignId = 1,
            CovenantId = null,
        });
        await ctx.SaveChangesAsync();

        var sut = CreateService(ctx);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.RequestLeaveCovenantAsync(1, "player"));
    }

    [Fact]
    public async Task RequestLeave_WhenAlreadyPending_Throws()
    {
        using var ctx = CreateContext(nameof(RequestLeave_WhenAlreadyPending_Throws));
        ctx.Campaigns.Add(new Campaign { Id = 1, Name = "Campaign", StoryTellerId = "st" });
        ctx.CovenantDefinitions.Add(new CovenantDefinition { Id = 1, Name = "Invictus", Description = string.Empty, IsPlayable = true });
        ctx.Characters.Add(new Character
        {
            Id = 1,
            Name = "Victor",
            ApplicationUserId = "player",
            CampaignId = 1,
            CovenantId = 1,
            CovenantLeaveRequestedAt = DateTime.UtcNow,
        });
        await ctx.SaveChangesAsync();

        var sut = CreateService(ctx);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.RequestLeaveCovenantAsync(1, "player"));
    }

    [Fact]
    public async Task RequestLeave_NonOwner_Throws()
    {
        using var ctx = CreateContext(nameof(RequestLeave_NonOwner_Throws));
        var authHelper = new Mock<IAuthorizationHelper>();
        authHelper
            .Setup(a => a.RequireCharacterOwnerAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new UnauthorizedAccessException("denied"));

        var sut = CreateService(ctx, authHelper.Object);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            sut.RequestLeaveCovenantAsync(1, "not-owner"));
    }

    // ── ApproveLeaveRequestAsync ───────────────────────────────────────────────

    [Fact]
    public async Task ApproveLeave_RemovesCharacterFromCovenant()
    {
        using var ctx = CreateContext(nameof(ApproveLeave_RemovesCharacterFromCovenant));
        ctx.Campaigns.Add(new Campaign { Id = 1, Name = "Campaign", StoryTellerId = "st" });
        ctx.CovenantDefinitions.Add(new CovenantDefinition { Id = 1, Name = "Invictus", Description = string.Empty, IsPlayable = true });
        ctx.Characters.Add(new Character
        {
            Id = 1,
            Name = "Victor",
            ApplicationUserId = "player",
            CampaignId = 1,
            CovenantId = 1,
            CovenantLeaveRequestedAt = DateTime.UtcNow,
        });
        await ctx.SaveChangesAsync();

        var sut = CreateService(ctx);
        await sut.ApproveLeaveRequestAsync(1, "st");

        var character = await ctx.Characters.FindAsync(1);
        Assert.Null(character!.CovenantId);
        Assert.Null(character.CovenantLeaveRequestedAt);
    }

    [Fact]
    public async Task ApproveLeave_WhenNoLeaveRequest_Throws()
    {
        using var ctx = CreateContext(nameof(ApproveLeave_WhenNoLeaveRequest_Throws));
        ctx.Campaigns.Add(new Campaign { Id = 1, Name = "Campaign", StoryTellerId = "st" });
        ctx.CovenantDefinitions.Add(new CovenantDefinition { Id = 1, Name = "Invictus", Description = string.Empty, IsPlayable = true });
        ctx.Characters.Add(new Character
        {
            Id = 1,
            Name = "Victor",
            ApplicationUserId = "player",
            CampaignId = 1,
            CovenantId = 1,
            CovenantLeaveRequestedAt = null,
        });
        await ctx.SaveChangesAsync();

        var sut = CreateService(ctx);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ApproveLeaveRequestAsync(1, "st"));
    }

    [Fact]
    public async Task ApproveLeave_NonStoryteller_Throws()
    {
        using var ctx = CreateContext(nameof(ApproveLeave_NonStoryteller_Throws));
        ctx.Campaigns.Add(new Campaign { Id = 1, Name = "Campaign", StoryTellerId = "st" });
        ctx.Characters.Add(new Character
        {
            Id = 1,
            Name = "Victor",
            ApplicationUserId = "player",
            CampaignId = 1,
            CovenantLeaveRequestedAt = DateTime.UtcNow,
        });
        await ctx.SaveChangesAsync();

        var authHelper = new Mock<IAuthorizationHelper>();
        authHelper
            .Setup(a => a.RequireStorytellerAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new UnauthorizedAccessException("denied"));

        var sut = CreateService(ctx, authHelper.Object);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            sut.ApproveLeaveRequestAsync(1, "not-st"));
    }

    // ── RejectLeaveRequestAsync ────────────────────────────────────────────────

    [Fact]
    public async Task RejectLeave_ClearsLeaveRequestedAt_CharacterStaysInCovenant()
    {
        using var ctx = CreateContext(nameof(RejectLeave_ClearsLeaveRequestedAt_CharacterStaysInCovenant));
        ctx.Campaigns.Add(new Campaign { Id = 1, Name = "Campaign", StoryTellerId = "st" });
        ctx.CovenantDefinitions.Add(new CovenantDefinition { Id = 1, Name = "Invictus", Description = string.Empty, IsPlayable = true });
        ctx.Characters.Add(new Character
        {
            Id = 1,
            Name = "Victor",
            ApplicationUserId = "player",
            CampaignId = 1,
            CovenantId = 1,
            CovenantLeaveRequestedAt = DateTime.UtcNow,
        });
        await ctx.SaveChangesAsync();

        var sut = CreateService(ctx);
        await sut.RejectLeaveRequestAsync(1, "st");

        var character = await ctx.Characters.FindAsync(1);
        Assert.Null(character!.CovenantLeaveRequestedAt);
        Assert.Equal(1, character.CovenantId); // still a member
    }

    [Fact]
    public async Task RejectLeave_NonStoryteller_Throws()
    {
        using var ctx = CreateContext(nameof(RejectLeave_NonStoryteller_Throws));
        ctx.Campaigns.Add(new Campaign { Id = 1, Name = "Campaign", StoryTellerId = "st" });
        ctx.Characters.Add(new Character
        {
            Id = 1,
            Name = "Victor",
            ApplicationUserId = "player",
            CampaignId = 1,
            CovenantLeaveRequestedAt = DateTime.UtcNow,
        });
        await ctx.SaveChangesAsync();

        var authHelper = new Mock<IAuthorizationHelper>();
        authHelper
            .Setup(a => a.RequireStorytellerAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new UnauthorizedAccessException("denied"));

        var sut = CreateService(ctx, authHelper.Object);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            sut.RejectLeaveRequestAsync(1, "not-st"));
    }
}
