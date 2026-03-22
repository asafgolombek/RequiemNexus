using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.DTOs;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.Models.Enums;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Listed asset procurement and illicit Storyteller approval (Phase 11).
/// </summary>
public class AssetProcurementService(
    ApplicationDbContext dbContext,
    IAuthorizationHelper authHelper,
    ICharacterAssetService characterAssetService,
    ILogger<AssetProcurementService> logger) : IAssetProcurementService
{
    private const string _resourcesMeritName = "Resources";

    /// <inheritdoc />
    public async Task<IReadOnlyList<PendingAssetProcurementDto>> GetPendingForCampaignAsync(
        int campaignId,
        string storyTellerUserId)
    {
        await authHelper.RequireStorytellerAsync(campaignId, storyTellerUserId, "view pending procurements");

        return await dbContext.PendingAssetProcurements
            .AsNoTracking()
            .Where(p => p.Status == PendingAssetProcurementStatus.Pending
                        && p.Character != null
                        && p.Character.CampaignId == campaignId)
            .OrderBy(p => p.RequestedAt)
            .Select(p => new PendingAssetProcurementDto(
                p.Id,
                p.Character!.Name,
                p.Asset!.Name,
                p.Quantity,
                p.RequestedAt,
                p.PlayerNote))
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<AssetProcurementStartResult> BeginProcurementAsync(
        int characterId,
        int assetId,
        int quantity,
        string userId,
        string? playerNote)
    {
        await authHelper.RequireCharacterAccessAsync(characterId, userId, "procure asset");

        Character character = await dbContext.Characters
            .Include(c => c.Merits).ThenInclude(m => m.Merit)
            .FirstAsync(c => c.Id == characterId);

        Asset asset = await dbContext.Assets.AsNoTracking().FirstAsync(a => a.Id == assetId);

        if (!asset.IsListedInCatalog)
        {
            return new AssetProcurementStartResult(
                AssetProcurementOutcome.Blocked,
                null,
                "This asset is not available for direct procurement.");
        }

        if (asset.IsIllicit)
        {
            if (character.CampaignId is not int campaignId)
            {
                return new AssetProcurementStartResult(
                    AssetProcurementOutcome.Blocked,
                    null,
                    "Illicit items require a chronicle and Storyteller approval.");
            }

            PendingAssetProcurement pending = new()
            {
                CharacterId = characterId,
                AssetId = assetId,
                Quantity = quantity,
                Status = PendingAssetProcurementStatus.Pending,
                PlayerNote = playerNote,
            };
            dbContext.PendingAssetProcurements.Add(pending);
            await dbContext.SaveChangesAsync();

            logger.LogInformation(
                "Illicit procurement pending {PendingId} for character {CharacterId} asset {AssetId} by {UserId}",
                pending.Id,
                characterId,
                assetId,
                userId);

            return new AssetProcurementStartResult(
                AssetProcurementOutcome.AwaitingStorytellerApproval,
                pending.Id,
                "Request submitted to the Storyteller.");
        }

        int resourcesDots = character.Merits
            .Where(m => m.Merit != null && string.Equals(m.Merit.Name, _resourcesMeritName, StringComparison.OrdinalIgnoreCase))
            .Sum(m => m.Rating);

        if (resourcesDots >= asset.Availability)
        {
            await characterAssetService.AddCharacterAssetAsync(characterId, assetId, quantity, userId);
            return new AssetProcurementStartResult(
                AssetProcurementOutcome.AddedImmediately,
                null,
                "Item acquired using your Resources.");
        }

        await characterAssetService.AddCharacterAssetAsync(characterId, assetId, quantity, userId);
        logger.LogInformation(
            "Procurement granted below Resources threshold for character {CharacterId} asset {AssetId} qty {Quantity} by {UserId}",
            characterId,
            assetId,
            quantity,
            userId);

        return new AssetProcurementStartResult(
            AssetProcurementOutcome.AddedImmediately,
            null,
            "Item added to your inventory. (Availability is higher than your Resources dots — no table roll required.)");
    }

    /// <inheritdoc />
    public async Task ApprovePendingAsync(int pendingId, string storyTellerUserId, string? note)
    {
        PendingAssetProcurement? pending = await dbContext.PendingAssetProcurements
            .Include(p => p.Character)
            .FirstOrDefaultAsync(p => p.Id == pendingId)
            ?? throw new InvalidOperationException($"Pending procurement {pendingId} was not found.");

        if (pending.Status != PendingAssetProcurementStatus.Pending)
        {
            throw new InvalidOperationException("This request was already resolved.");
        }

        Character? ch = pending.Character;
        if (ch?.CampaignId is not int campaignId)
        {
            throw new InvalidOperationException("Character is not attached to a campaign.");
        }

        await authHelper.RequireStorytellerAsync(campaignId, storyTellerUserId, "approve procurement");

        await characterAssetService.AddCharacterAssetAsync(ch.Id, pending.AssetId, pending.Quantity, storyTellerUserId);
        pending.Status = PendingAssetProcurementStatus.Approved;
        pending.StorytellerNote = note;
        pending.ResolvedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync();

        logger.LogInformation(
            "ST {UserId} approved procurement {PendingId} for character {CharacterId}",
            storyTellerUserId,
            pendingId,
            ch.Id);
    }

    /// <inheritdoc />
    public async Task RejectPendingAsync(int pendingId, string storyTellerUserId, string? note)
    {
        PendingAssetProcurement? pending = await dbContext.PendingAssetProcurements
            .Include(p => p.Character)
            .FirstOrDefaultAsync(p => p.Id == pendingId)
            ?? throw new InvalidOperationException($"Pending procurement {pendingId} was not found.");

        if (pending.Status != PendingAssetProcurementStatus.Pending)
        {
            throw new InvalidOperationException("This request was already resolved.");
        }

        Character? ch = pending.Character;
        if (ch?.CampaignId is not int campaignId)
        {
            throw new InvalidOperationException("Character is not attached to a campaign.");
        }

        await authHelper.RequireStorytellerAsync(campaignId, storyTellerUserId, "reject procurement");

        pending.Status = PendingAssetProcurementStatus.Rejected;
        pending.StorytellerNote = note;
        pending.ResolvedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync();

        logger.LogInformation(
            "ST {UserId} rejected procurement {PendingId} for character {CharacterId}",
            storyTellerUserId,
            pendingId,
            ch.Id);
    }
}
