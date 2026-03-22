using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.DTOs;
using RequiemNexus.Application.Services;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.Models.Enums;
using Xunit;

namespace RequiemNexus.Application.Tests;

/// <summary>
/// Phase 11: Resources threshold, procurement roll payload, and illicit pending rows.
/// </summary>
public class AssetProcurementServiceTests
{
    private static ApplicationDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new ApplicationDbContext(options);
    }

    private static Character CreateCharacter(int id, int? campaignId, string userId = "player") =>
        new()
        {
            Id = id,
            ApplicationUserId = userId,
            Name = "PC",
            CampaignId = campaignId,
            MaxHealth = 7,
            CurrentHealth = 7,
            MaxWillpower = 4,
            CurrentWillpower = 4,
            MaxVitae = 10,
            CurrentVitae = 10,
        };

    [Fact]
    public async Task BeginProcurementAsync_ResourcesMeetsAvailability_CallsAddCharacterAsset()
    {
        string db = nameof(BeginProcurementAsync_ResourcesMeetsAvailability_CallsAddCharacterAsset);
        await using var ctx = CreateContext(db);
        var merit = new Merit { Id = 1, Name = "Resources" };
        ctx.Merits.Add(merit);
        ctx.Characters.Add(CreateCharacter(1, null));
        ctx.CharacterMerits.Add(new CharacterMerit
        {
            CharacterId = 1,
            MeritId = 1,
            Rating = 3,
            Merit = merit,
        });
        ctx.Assets.Add(new Asset
        {
            Id = 5,
            Name = "Cheap",
            Kind = AssetKind.General,
            Slug = "test:cheap",
            Availability = 2,
            IsIllicit = false,
            IsListedInCatalog = true,
        });
        await ctx.SaveChangesAsync();

        var auth = new Mock<IAuthorizationHelper>();
        auth.Setup(a => a.RequireCharacterAccessAsync(1, "player", It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        var assetSvc = new Mock<ICharacterAssetService>();
        assetSvc.Setup(s => s.AddCharacterAssetAsync(1, 5, 2, "player"))
            .ReturnsAsync(new CharacterAsset { Id = 99, CharacterId = 1, AssetId = 5, Quantity = 2 });

        var sut = new AssetProcurementService(ctx, auth.Object, assetSvc.Object, NullLogger<AssetProcurementService>.Instance);

        AssetProcurementStartResult result = await sut.BeginProcurementAsync(1, 5, 2, "player", null);

        Assert.Equal(AssetProcurementOutcome.AddedImmediately, result.Outcome);
        assetSvc.Verify(s => s.AddCharacterAssetAsync(1, 5, 2, "player"), Times.Once);
    }

    [Fact]
    public async Task BeginProcurementAsync_BelowResources_ReturnsProcurementPool()
    {
        string db = nameof(BeginProcurementAsync_BelowResources_ReturnsProcurementPool);
        await using var ctx = CreateContext(db);
        var merit = new Merit { Id = 1, Name = "Resources" };
        ctx.Merits.Add(merit);
        ctx.Characters.Add(CreateCharacter(1, null));
        ctx.CharacterMerits.Add(new CharacterMerit { CharacterId = 1, MeritId = 1, Rating = 1, Merit = merit });
        ctx.Assets.Add(new Asset
        {
            Id = 8,
            Name = "Rare",
            Kind = AssetKind.General,
            Slug = "test:rare",
            Availability = 4,
            IsIllicit = false,
            IsListedInCatalog = true,
        });
        await ctx.SaveChangesAsync();

        var auth = new Mock<IAuthorizationHelper>();
        auth.Setup(a => a.RequireCharacterAccessAsync(1, "player", It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        var assetSvc = new Mock<ICharacterAssetService>();

        var sut = new AssetProcurementService(ctx, auth.Object, assetSvc.Object, NullLogger<AssetProcurementService>.Instance);

        AssetProcurementStartResult result = await sut.BeginProcurementAsync(1, 8, 1, "player", null);

        Assert.Equal(AssetProcurementOutcome.RequiresProcurementRoll, result.Outcome);
        Assert.NotNull(result.ProcurementRollPool);
        assetSvc.Verify(
            s => s.AddCharacterAssetAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task BeginProcurementAsync_IllicitWithCampaign_CreatesPendingRow()
    {
        string db = nameof(BeginProcurementAsync_IllicitWithCampaign_CreatesPendingRow);
        await using var ctx = CreateContext(db);
        ctx.Campaigns.Add(new Campaign { Id = 1, Name = "Chronicle", StoryTellerId = "st" });
        ctx.Characters.Add(CreateCharacter(1, 1));
        ctx.Assets.Add(new Asset
        {
            Id = 12,
            Name = "Hot",
            Kind = AssetKind.General,
            Slug = "test:hot",
            Availability = 1,
            IsIllicit = true,
            IsListedInCatalog = true,
        });
        await ctx.SaveChangesAsync();

        var auth = new Mock<IAuthorizationHelper>();
        auth.Setup(a => a.RequireCharacterAccessAsync(1, "player", It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        var assetSvc = new Mock<ICharacterAssetService>();

        var sut = new AssetProcurementService(ctx, auth.Object, assetSvc.Object, NullLogger<AssetProcurementService>.Instance);

        AssetProcurementStartResult result = await sut.BeginProcurementAsync(1, 12, 1, "player", "note");

        Assert.Equal(AssetProcurementOutcome.AwaitingStorytellerApproval, result.Outcome);
        Assert.NotNull(result.PendingProcurementId);
        Assert.Equal(1, await ctx.PendingAssetProcurements.CountAsync());
        assetSvc.Verify(
            s => s.AddCharacterAssetAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task BeginProcurementAsync_IllicitWithoutCampaign_DoesNotCreatePending()
    {
        string db = nameof(BeginProcurementAsync_IllicitWithoutCampaign_DoesNotCreatePending);
        await using var ctx = CreateContext(db);
        ctx.Characters.Add(CreateCharacter(1, null));
        ctx.Assets.Add(new Asset
        {
            Id = 12,
            Name = "Hot",
            Kind = AssetKind.General,
            Slug = "test:hot",
            Availability = 1,
            IsIllicit = true,
            IsListedInCatalog = true,
        });
        await ctx.SaveChangesAsync();

        var auth = new Mock<IAuthorizationHelper>();
        auth.Setup(a => a.RequireCharacterAccessAsync(1, "player", It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        var assetSvc = new Mock<ICharacterAssetService>();

        var sut = new AssetProcurementService(ctx, auth.Object, assetSvc.Object, NullLogger<AssetProcurementService>.Instance);

        AssetProcurementStartResult result = await sut.BeginProcurementAsync(1, 12, 1, "player", null);

        Assert.Equal(0, await ctx.PendingAssetProcurements.CountAsync());
        Assert.Contains("chronicle", result.Message, StringComparison.OrdinalIgnoreCase);
    }
}
