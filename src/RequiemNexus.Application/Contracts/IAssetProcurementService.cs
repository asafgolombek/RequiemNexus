using RequiemNexus.Application.DTOs;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Resources-based procurement and illicit approval workflow.
/// </summary>
public interface IAssetProcurementService
{
    /// <summary>Pending illicit procurement rows for a campaign (Storyteller only).</summary>
    Task<IReadOnlyList<PendingAssetProcurementDto>> GetPendingForCampaignAsync(int campaignId, string storyTellerUserId);

    /// <summary>
    /// Attempts procurement: auto-add, roll template, or illicit pending row.
    /// </summary>
    Task<AssetProcurementStartResult> BeginProcurementAsync(
        int characterId,
        int assetId,
        int quantity,
        string userId,
        string? playerNote);

    /// <summary>After a successful non-illicit procurement roll, grants the asset.</summary>
    Task CompleteProcurementRollAsync(int characterId, int assetId, int quantity, string userId);

    /// <summary>Storyteller approves a pending illicit (or gated) request.</summary>
    Task ApprovePendingAsync(int pendingId, string storyTellerUserId, string? note);

    /// <summary>Storyteller rejects a pending request.</summary>
    Task RejectPendingAsync(int pendingId, string storyTellerUserId, string? note);
}
