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
/// Phase 11 Refined: Automatic, Reach (once per chapter), and direct catalog purchase when below Availability or after Reach.
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
    public async Task BeginProcurementAsync_ResourcesAboveAvailability_AddsImmediately()
    {
        string db = nameof(BeginProcurementAsync_ResourcesAboveAvailability_AddsImmediately);
        await using var ctx = CreateContext(db);
        var merit = new Merit { Id = 1, Name = "Resources" };
        ctx.Merits.Add(merit);
        ctx.Characters.Add(CreateCharacter(1, null));
        ctx.CharacterMerits.Add(new CharacterMerit { CharacterId = 1, MeritId = 1, Rating = 3, Merit = merit });
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
    public async Task BeginProcurementAsync_ResourcesEqualsAvailability_AddsByReach()
    {
        string db = nameof(BeginProcurementAsync_ResourcesEqualsAvailability_AddsByReach);
        await using var ctx = CreateContext(db);
        var merit = new Merit { Id = 1, Name = "Resources" };
        ctx.Merits.Add(merit);
        ctx.Characters.Add(CreateCharacter(1, null));
        ctx.CharacterMerits.Add(new CharacterMerit { CharacterId = 1, MeritId = 1, Rating = 2, Merit = merit });
        ctx.Assets.Add(new Asset
        {
            Id = 5,
            Name = "Equal",
            Kind = AssetKind.General,
            Slug = "test:equal",
            Availability = 2,
            IsIllicit = false,
            IsListedInCatalog = true,
        });
        await ctx.SaveChangesAsync();

        var auth = new Mock<IAuthorizationHelper>();
        auth.Setup(a => a.RequireCharacterAccessAsync(1, "player", It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var assetRow = new CharacterAsset { Id = 99, CharacterId = 1, AssetId = 5, Quantity = 1 };
        ctx.CharacterAssets.Add(assetRow);
        await ctx.SaveChangesAsync();

        var assetSvc = new Mock<ICharacterAssetService>();
        assetSvc.Setup(s => s.AddCharacterAssetAsync(1, 5, 1, "player"))
            .ReturnsAsync(assetRow);

        var sut = new AssetProcurementService(ctx, auth.Object, assetSvc.Object, NullLogger<AssetProcurementService>.Instance);

        AssetProcurementStartResult result = await sut.BeginProcurementAsync(1, 5, 1, "player", null);

        Assert.Equal(AssetProcurementOutcome.AddedByReach, result.Outcome);
        assetSvc.Verify(s => s.AddCharacterAssetAsync(1, 5, 1, "player"), Times.Once);

        var row = await ctx.CharacterAssets.FirstAsync(ca => ca.Id == 99);
        Assert.NotNull(row.LastProcurementDate);
    }

    [Fact]
    public async Task BeginProcurementAsync_ReachAlreadyUsed_AddsImmediately()
    {
        string db = nameof(BeginProcurementAsync_ReachAlreadyUsed_AddsImmediately);
        await using var ctx = CreateContext(db);
        var merit = new Merit { Id = 1, Name = "Resources" };
        ctx.Merits.Add(merit);
        ctx.Characters.Add(CreateCharacter(1, null));
        ctx.CharacterMerits.Add(new CharacterMerit { CharacterId = 1, MeritId = 1, Rating = 2, Merit = merit });
        ctx.Assets.Add(new Asset
        {
            Id = 5,
            Name = "Equal",
            Kind = AssetKind.General,
            Slug = "test:equal",
            Availability = 2,
            IsIllicit = false,
            IsListedInCatalog = true,
        });
        // Add an existing "Reach" item from 1 hour ago.
        ctx.CharacterAssets.Add(new CharacterAsset
        {
            Id = 50,
            CharacterId = 1,
            AssetId = 10,
            WasAcquiredViaReach = true,
            LastProcurementDate = DateTimeOffset.UtcNow.AddHours(-1),
        });
        await ctx.SaveChangesAsync();

        var auth = new Mock<IAuthorizationHelper>();
        auth.Setup(a => a.RequireCharacterAccessAsync(1, "player", It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        var assetSvc = new Mock<ICharacterAssetService>();
        assetSvc.Setup(s => s.AddCharacterAssetAsync(1, 5, 1, "player"))
            .ReturnsAsync(new CharacterAsset { Id = 101, CharacterId = 1, AssetId = 5, Quantity = 1 });

        var sut = new AssetProcurementService(ctx, auth.Object, assetSvc.Object, NullLogger<AssetProcurementService>.Instance);

        AssetProcurementStartResult result = await sut.BeginProcurementAsync(1, 5, 1, "player", null);

        Assert.Equal(AssetProcurementOutcome.AddedImmediately, result.Outcome);
        Assert.Contains("Reach", result.Message, StringComparison.OrdinalIgnoreCase);
        assetSvc.Verify(s => s.AddCharacterAssetAsync(1, 5, 1, "player"), Times.Once);
    }

    [Fact]
    public async Task BeginProcurementAsync_ResourcesBelowAvailability_AddsImmediately()
    {
        string db = nameof(BeginProcurementAsync_ResourcesBelowAvailability_AddsImmediately);
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
        assetSvc.Setup(s => s.AddCharacterAssetAsync(1, 8, 1, "player"))
            .ReturnsAsync(new CharacterAsset { Id = 102, CharacterId = 1, AssetId = 8, Quantity = 1 });

        var sut = new AssetProcurementService(ctx, auth.Object, assetSvc.Object, NullLogger<AssetProcurementService>.Instance);

        AssetProcurementStartResult result = await sut.BeginProcurementAsync(1, 8, 1, "player", null);

        Assert.Equal(AssetProcurementOutcome.AddedImmediately, result.Outcome);
        Assert.Contains("Availability", result.Message, StringComparison.OrdinalIgnoreCase);
        assetSvc.Verify(s => s.AddCharacterAssetAsync(1, 8, 1, "player"), Times.Once);
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

        Assert.Equal(AssetProcurementOutcome.Blocked, result.Outcome);
        Assert.Equal(0, await ctx.PendingAssetProcurements.CountAsync());
    }

    [Fact]
    public async Task BeginProcurementAsync_NotListedInCatalog_ReturnsBlocked()
    {
        string db = nameof(BeginProcurementAsync_NotListedInCatalog_ReturnsBlocked);
        await using var ctx = CreateContext(db);
        ctx.Characters.Add(CreateCharacter(1, null));
        ctx.Assets.Add(new Asset
        {
            Id = 20,
            Name = "Hidden",
            Kind = AssetKind.General,
            Slug = "test:hidden",
            Availability = 1,
            IsIllicit = false,
            IsListedInCatalog = false,
        });
        await ctx.SaveChangesAsync();

        var auth = new Mock<IAuthorizationHelper>();
        auth.Setup(a => a.RequireCharacterAccessAsync(1, "player", It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        var assetSvc = new Mock<ICharacterAssetService>();

        var sut = new AssetProcurementService(ctx, auth.Object, assetSvc.Object, NullLogger<AssetProcurementService>.Instance);

        AssetProcurementStartResult result = await sut.BeginProcurementAsync(1, 20, 1, "player", null);

        Assert.Equal(AssetProcurementOutcome.Blocked, result.Outcome);
    }

    [Fact]
    public async Task ApprovePendingAsync_HappyPath_AddsAssetAndSetsApproved()
    {
        string db = nameof(ApprovePendingAsync_HappyPath_AddsAssetAndSetsApproved);
        await using var ctx = CreateContext(db);
        ctx.Campaigns.Add(new Campaign { Id = 1, Name = "Chronicle", StoryTellerId = "st" });
        ctx.Characters.Add(CreateCharacter(1, 1));
        ctx.Assets.Add(new Asset { Id = 5, Name = "Blade", Kind = AssetKind.Weapon, Slug = "test:blade", Availability = 2, IsIllicit = true, IsListedInCatalog = true });
        ctx.PendingAssetProcurements.Add(new PendingAssetProcurement
        {
            Id = 10,
            CharacterId = 1,
            AssetId = 5,
            Quantity = 1,
            Status = PendingAssetProcurementStatus.Pending,
        });
        await ctx.SaveChangesAsync();

        var auth = new Mock<IAuthorizationHelper>();
        auth.Setup(a => a.RequireStorytellerAsync(1, "st", It.IsAny<string>())).Returns(Task.CompletedTask);
        var assetSvc = new Mock<ICharacterAssetService>();
        assetSvc.Setup(s => s.AddCharacterAssetAsync(1, 5, 1, "st"))
            .ReturnsAsync(new CharacterAsset { Id = 99, CharacterId = 1, AssetId = 5, Quantity = 1 });

        var sut = new AssetProcurementService(ctx, auth.Object, assetSvc.Object, NullLogger<AssetProcurementService>.Instance);

        await sut.ApprovePendingAsync(10, "st", "Approved by ST");

        var pending = await ctx.PendingAssetProcurements.FindAsync(10);
        Assert.Equal(PendingAssetProcurementStatus.Approved, pending!.Status);
        Assert.Equal("Approved by ST", pending.StorytellerNote);
        Assert.NotNull(pending.ResolvedAt);
        assetSvc.Verify(s => s.AddCharacterAssetAsync(1, 5, 1, "st"), Times.Once);
    }

    [Fact]
    public async Task ApprovePendingAsync_AlreadyResolved_Throws()
    {
        string db = nameof(ApprovePendingAsync_AlreadyResolved_Throws);
        await using var ctx = CreateContext(db);
        ctx.Campaigns.Add(new Campaign { Id = 1, Name = "Chronicle", StoryTellerId = "st" });
        ctx.Characters.Add(CreateCharacter(1, 1));
        ctx.PendingAssetProcurements.Add(new PendingAssetProcurement
        {
            Id = 10,
            CharacterId = 1,
            AssetId = 5,
            Quantity = 1,
            Status = PendingAssetProcurementStatus.Approved,
            ResolvedAt = DateTimeOffset.UtcNow,
        });
        await ctx.SaveChangesAsync();

        var auth = new Mock<IAuthorizationHelper>();
        var assetSvc = new Mock<ICharacterAssetService>();
        var sut = new AssetProcurementService(ctx, auth.Object, assetSvc.Object, NullLogger<AssetProcurementService>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ApprovePendingAsync(10, "st", null));
    }

    [Fact]
    public async Task RejectPendingAsync_HappyPath_SetsRejected()
    {
        string db = nameof(RejectPendingAsync_HappyPath_SetsRejected);
        await using var ctx = CreateContext(db);
        ctx.Campaigns.Add(new Campaign { Id = 1, Name = "Chronicle", StoryTellerId = "st" });
        ctx.Characters.Add(CreateCharacter(1, 1));
        ctx.PendingAssetProcurements.Add(new PendingAssetProcurement
        {
            Id = 11,
            CharacterId = 1,
            AssetId = 5,
            Quantity = 1,
            Status = PendingAssetProcurementStatus.Pending,
        });
        await ctx.SaveChangesAsync();

        var auth = new Mock<IAuthorizationHelper>();
        auth.Setup(a => a.RequireStorytellerAsync(1, "st", It.IsAny<string>())).Returns(Task.CompletedTask);
        var assetSvc = new Mock<ICharacterAssetService>();

        var sut = new AssetProcurementService(ctx, auth.Object, assetSvc.Object, NullLogger<AssetProcurementService>.Instance);

        await sut.RejectPendingAsync(11, "st", "Not approved.");

        var pending = await ctx.PendingAssetProcurements.FindAsync(11);
        Assert.Equal(PendingAssetProcurementStatus.Rejected, pending!.Status);
        Assert.Equal("Not approved.", pending.StorytellerNote);
        Assert.NotNull(pending.ResolvedAt);
        assetSvc.Verify(s => s.AddCharacterAssetAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetPendingForCampaignAsync_ReturnsPendingForCampaignOrderedByDate()
    {
        string db = nameof(GetPendingForCampaignAsync_ReturnsPendingForCampaignOrderedByDate);
        await using var ctx = CreateContext(db);
        ctx.Campaigns.Add(new Campaign { Id = 1, Name = "Chronicle", StoryTellerId = "st" });
        ctx.Campaigns.Add(new Campaign { Id = 2, Name = "Other", StoryTellerId = "st" });
        ctx.Characters.Add(CreateCharacter(1, 1, "player1"));
        ctx.Characters.Add(CreateCharacter(2, 2, "player2"));
        ctx.Assets.Add(new Asset { Id = 5, Name = "Blade", Kind = AssetKind.Weapon, Slug = "t:b", Availability = 2, IsIllicit = true, IsListedInCatalog = true });

        var earlier = DateTimeOffset.UtcNow.AddMinutes(-30);
        var later = DateTimeOffset.UtcNow;

        ctx.PendingAssetProcurements.Add(new PendingAssetProcurement { Id = 1, CharacterId = 1, AssetId = 5, Quantity = 1, Status = PendingAssetProcurementStatus.Pending, RequestedAt = later });
        ctx.PendingAssetProcurements.Add(new PendingAssetProcurement { Id = 2, CharacterId = 1, AssetId = 5, Quantity = 1, Status = PendingAssetProcurementStatus.Pending, RequestedAt = earlier });
        // Different campaign — should NOT appear.
        ctx.PendingAssetProcurements.Add(new PendingAssetProcurement { Id = 3, CharacterId = 2, AssetId = 5, Quantity = 1, Status = PendingAssetProcurementStatus.Pending, RequestedAt = later });
        // Already resolved — should NOT appear.
        ctx.PendingAssetProcurements.Add(new PendingAssetProcurement { Id = 4, CharacterId = 1, AssetId = 5, Quantity = 1, Status = PendingAssetProcurementStatus.Approved, RequestedAt = earlier });
        await ctx.SaveChangesAsync();

        var auth = new Mock<IAuthorizationHelper>();
        auth.Setup(a => a.RequireStorytellerAsync(1, "st", It.IsAny<string>())).Returns(Task.CompletedTask);
        var assetSvc = new Mock<ICharacterAssetService>();

        var sut = new AssetProcurementService(ctx, auth.Object, assetSvc.Object, NullLogger<AssetProcurementService>.Instance);

        IReadOnlyList<PendingAssetProcurementDto> result = await sut.GetPendingForCampaignAsync(1, "st");

        Assert.Equal(2, result.Count);
        // Ordered by RequestedAt ascending.
        Assert.Equal(2, result[0].Id);
        Assert.Equal(1, result[1].Id);
    }
}
