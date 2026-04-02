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
/// Tests for BloodlineService — apply, approve, reject workflows and authorization.
/// </summary>
public class BloodlineServiceTests
{
    private static async Task<BloodlineService> CreateServiceAsync(ApplicationDbContext ctx)
    {
        var authHelper = new Mock<IAuthorizationHelper>();
        authHelper
            .Setup(a => a.RequireCharacterOwnerAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        authHelper
            .Setup(a => a.RequireStorytellerAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var sessionService = new Mock<ISessionService>();
        sessionService
            .Setup(s => s.BroadcastCharacterUpdateAsync(It.IsAny<int>()))
            .Returns(Task.CompletedTask);
        sessionService
            .Setup(s => s.BroadcastBloodlineApprovedAsync(It.IsAny<int>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var logger = new Mock<ILogger<BloodlineService>>().Object;
        IReferenceDataCache cache = await ReferenceDataCacheTestDoubles.WarmFromAsync(ctx);
        return new BloodlineService(ctx, authHelper.Object, sessionService.Object, cache, logger);
    }

    private static ApplicationDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ApplicationDbContext(options);
    }

    private static async Task SeedCampaignAndCharacter(ApplicationDbContext ctx, string ownerId = "owner")
    {
        ctx.Campaigns.Add(new Campaign { Id = 1, Name = "Test Campaign", StoryTellerId = "st" });
        ctx.Clans.Add(new Clan { Id = 1, Name = "Mekhet" });
        ctx.Disciplines.Add(new Discipline { Id = 1, Name = "Obtenebration" });
        ctx.BloodlineDefinitions.Add(new BloodlineDefinition
        {
            Id = 1,
            Name = "Khaibit",
            Description = "Shadow bloodline",
            FourthDisciplineId = 1,
            PrerequisiteBloodPotency = 2,
            BaneOverride = "Light sensitivity",
        });
        ctx.BloodlineClans.Add(new BloodlineClan { BloodlineDefinitionId = 1, ClanId = 1 });
        ctx.Characters.Add(new Character
        {
            Id = 1,
            Name = "Test Char",
            ApplicationUserId = ownerId,
            ClanId = 1,
            CampaignId = 1,
            BloodPotency = 2,
        });
        await ctx.SaveChangesAsync();
    }

    [Fact]
    public async Task ApplyForBloodlineAsync_OwnerApplies_CreatesPending()
    {
        using var ctx = CreateContext(nameof(ApplyForBloodlineAsync_OwnerApplies_CreatesPending));
        await SeedCampaignAndCharacter(ctx);
        var service = await CreateServiceAsync(ctx);

        var result = await service.ApplyForBloodlineAsync(1, 1, "owner");

        Assert.NotNull(result);
        Assert.Equal(BloodlineStatus.Pending, result.Status);
        Assert.Equal(1, result.CharacterId);
        Assert.Equal(1, result.BloodlineDefinitionId);

        var saved = await ctx.CharacterBloodlines.FirstOrDefaultAsync(cb => cb.CharacterId == 1);
        Assert.NotNull(saved);
        Assert.Equal(BloodlineStatus.Pending, saved.Status);
    }

    [Fact]
    public async Task ApplyForBloodlineAsync_NonOwner_Throws()
    {
        using var ctx = CreateContext(nameof(ApplyForBloodlineAsync_NonOwner_Throws));
        await SeedCampaignAndCharacter(ctx);

        var authHelper = new Mock<IAuthorizationHelper>();
        authHelper
            .Setup(a => a.RequireCharacterOwnerAsync(1, "other", It.IsAny<string>()))
            .ThrowsAsync(new UnauthorizedAccessException());

        IReferenceDataCache cache = await ReferenceDataCacheTestDoubles.WarmFromAsync(ctx);
        var service = new BloodlineService(
            ctx,
            authHelper.Object,
            new Mock<ISessionService>().Object,
            cache,
            new Mock<ILogger<BloodlineService>>().Object);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.ApplyForBloodlineAsync(1, 1, "other"));
    }

    [Fact]
    public async Task ApplyForBloodlineAsync_AlreadyHasActive_Throws()
    {
        using var ctx = CreateContext(nameof(ApplyForBloodlineAsync_AlreadyHasActive_Throws));
        await SeedCampaignAndCharacter(ctx);
        ctx.CharacterBloodlines.Add(new CharacterBloodline
        {
            CharacterId = 1,
            BloodlineDefinitionId = 1,
            Status = BloodlineStatus.Active,
            AppliedAt = DateTime.UtcNow,
        });
        await ctx.SaveChangesAsync();

        var service = await CreateServiceAsync(ctx);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ApplyForBloodlineAsync(1, 1, "owner"));
    }

    [Fact]
    public async Task ApplyForBloodlineAsync_AlreadyHasPending_Throws()
    {
        using var ctx = CreateContext(nameof(ApplyForBloodlineAsync_AlreadyHasPending_Throws));
        await SeedCampaignAndCharacter(ctx);
        ctx.CharacterBloodlines.Add(new CharacterBloodline
        {
            CharacterId = 1,
            BloodlineDefinitionId = 1,
            Status = BloodlineStatus.Pending,
            AppliedAt = DateTime.UtcNow,
        });
        await ctx.SaveChangesAsync();

        var service = await CreateServiceAsync(ctx);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ApplyForBloodlineAsync(1, 1, "owner"));
    }

    [Fact]
    public async Task ApproveBloodlineAsync_STApproves_StatusBecomesActive()
    {
        using var ctx = CreateContext(nameof(ApproveBloodlineAsync_STApproves_StatusBecomesActive));
        await SeedCampaignAndCharacter(ctx);
        ctx.CharacterBloodlines.Add(new CharacterBloodline
        {
            Id = 10,
            CharacterId = 1,
            BloodlineDefinitionId = 1,
            Status = BloodlineStatus.Pending,
            AppliedAt = DateTime.UtcNow,
        });
        await ctx.SaveChangesAsync();

        var authHelper = new Mock<IAuthorizationHelper>();
        authHelper
            .Setup(a => a.RequireStorytellerAsync(1, "st", It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        IReferenceDataCache cache = await ReferenceDataCacheTestDoubles.WarmFromAsync(ctx);
        var service = new BloodlineService(
            ctx,
            authHelper.Object,
            new Mock<ISessionService>().Object,
            cache,
            new Mock<ILogger<BloodlineService>>().Object);

        await service.ApproveBloodlineAsync(10, null, "st");

        var cb = await ctx.CharacterBloodlines.FindAsync(10);
        Assert.NotNull(cb);
        Assert.Equal(BloodlineStatus.Active, cb.Status);
        Assert.NotNull(cb.ResolvedAt);
    }

    [Fact]
    public async Task RejectBloodlineAsync_STRejects_StatusBecomesRejected()
    {
        using var ctx = CreateContext(nameof(RejectBloodlineAsync_STRejects_StatusBecomesRejected));
        await SeedCampaignAndCharacter(ctx);
        ctx.CharacterBloodlines.Add(new CharacterBloodline
        {
            Id = 20,
            CharacterId = 1,
            BloodlineDefinitionId = 1,
            Status = BloodlineStatus.Pending,
            AppliedAt = DateTime.UtcNow,
        });
        await ctx.SaveChangesAsync();

        var authHelper = new Mock<IAuthorizationHelper>();
        authHelper
            .Setup(a => a.RequireStorytellerAsync(1, "st", It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        IReferenceDataCache cache = await ReferenceDataCacheTestDoubles.WarmFromAsync(ctx);
        var service = new BloodlineService(
            ctx,
            authHelper.Object,
            new Mock<ISessionService>().Object,
            cache,
            new Mock<ILogger<BloodlineService>>().Object);

        await service.RejectBloodlineAsync(20, "Not a fit", "st");

        var cb = await ctx.CharacterBloodlines.FindAsync(20);
        Assert.NotNull(cb);
        Assert.Equal(BloodlineStatus.Rejected, cb.Status);
        Assert.Equal("Not a fit", cb.StorytellerNote);
        Assert.NotNull(cb.ResolvedAt);
    }

    [Fact]
    public async Task RemoveBloodlineAsync_OwnerRemovesActive_RemovesBloodlineAndDevotions()
    {
        using var ctx = CreateContext(nameof(RemoveBloodlineAsync_OwnerRemovesActive_RemovesBloodlineAndDevotions));
        await SeedCampaignAndCharacter(ctx);
        ctx.CharacterBloodlines.Add(new CharacterBloodline
        {
            Id = 30,
            CharacterId = 1,
            BloodlineDefinitionId = 1,
            Status = BloodlineStatus.Active,
            AppliedAt = DateTime.UtcNow,
            ResolvedAt = DateTime.UtcNow,
        });

        ctx.DevotionDefinitions.Add(new DevotionDefinition
        {
            Id = 1,
            Name = "Bloodline Devotion",
            RequiredBloodlineId = 1,
        });
        ctx.DevotionDefinitions.Add(new DevotionDefinition
        {
            Id = 2,
            Name = "Generic Devotion",
            RequiredBloodlineId = null,
        });
        ctx.CharacterDevotions.Add(new CharacterDevotion
        {
            CharacterId = 1,
            DevotionDefinitionId = 1,
        });
        ctx.CharacterDevotions.Add(new CharacterDevotion
        {
            CharacterId = 1,
            DevotionDefinitionId = 2,
        });
        await ctx.SaveChangesAsync();

        var service = await CreateServiceAsync(ctx);

        await service.RemoveBloodlineAsync(30, "owner");

        var cb = await ctx.CharacterBloodlines.FindAsync(30);
        Assert.Null(cb);

        var devotionWithBloodline = await ctx.CharacterDevotions
            .Include(cd => cd.DevotionDefinition)
            .FirstOrDefaultAsync(cd => cd.CharacterId == 1 && cd.DevotionDefinition!.RequiredBloodlineId == 1);
        Assert.Null(devotionWithBloodline);

        var genericDevotion = await ctx.CharacterDevotions
            .Include(cd => cd.DevotionDefinition)
            .FirstOrDefaultAsync(cd => cd.CharacterId == 1 && cd.DevotionDefinition!.RequiredBloodlineId == null);
        Assert.NotNull(genericDevotion);
    }

    [Fact]
    public async Task RemoveBloodlineAsync_NonOwner_Throws()
    {
        using var ctx = CreateContext(nameof(RemoveBloodlineAsync_NonOwner_Throws));
        await SeedCampaignAndCharacter(ctx);
        ctx.CharacterBloodlines.Add(new CharacterBloodline
        {
            Id = 40,
            CharacterId = 1,
            BloodlineDefinitionId = 1,
            Status = BloodlineStatus.Active,
            AppliedAt = DateTime.UtcNow,
            ResolvedAt = DateTime.UtcNow,
        });
        await ctx.SaveChangesAsync();

        var authHelper = new Mock<IAuthorizationHelper>();
        authHelper
            .Setup(a => a.RequireCharacterOwnerAsync(1, "other", It.IsAny<string>()))
            .ThrowsAsync(new UnauthorizedAccessException());

        IReferenceDataCache cache = await ReferenceDataCacheTestDoubles.WarmFromAsync(ctx);
        var service = new BloodlineService(
            ctx,
            authHelper.Object,
            new Mock<ISessionService>().Object,
            cache,
            new Mock<ILogger<BloodlineService>>().Object);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.RemoveBloodlineAsync(40, "other"));
    }

    [Fact]
    public async Task RemoveBloodlineAsync_PendingStatus_Throws()
    {
        using var ctx = CreateContext(nameof(RemoveBloodlineAsync_PendingStatus_Throws));
        await SeedCampaignAndCharacter(ctx);
        ctx.CharacterBloodlines.Add(new CharacterBloodline
        {
            Id = 50,
            CharacterId = 1,
            BloodlineDefinitionId = 1,
            Status = BloodlineStatus.Pending,
            AppliedAt = DateTime.UtcNow,
        });
        await ctx.SaveChangesAsync();

        var service = await CreateServiceAsync(ctx);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RemoveBloodlineAsync(50, "owner"));
    }
}
