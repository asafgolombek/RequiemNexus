using RequiemNexus.Application.DTOs;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Application service for blood sorcery rite learning and activation.
/// </summary>
public interface ISorceryService
{
    /// <summary>
    /// Returns rites the character is eligible to learn (covenant match, discipline dots, XP, not already learned/pending).
    /// </summary>
    Task<List<SorceryRiteSummaryDto>> GetEligibleRitesAsync(int characterId, string userId);

    /// <summary>
    /// Creates a rite learning request with Status Pending. XP is reserved until ST approval.
    /// </summary>
    Task<CharacterRite> RequestLearnRiteAsync(int characterId, int sorceryRiteDefinitionId, string userId);

    /// <summary>
    /// Returns pending rite learning applications for characters in the campaign.
    /// </summary>
    Task<List<RiteApplicationDto>> GetPendingRiteApplicationsAsync(int campaignId, string storyTellerUserId);

    /// <summary>
    /// Approves a rite learning request. Deducts XP and sets Status to Approved.
    /// </summary>
    Task ApproveRiteLearnAsync(int characterRiteId, string? note, string storyTellerUserId);

    /// <summary>
    /// Rejects a rite learning request.
    /// </summary>
    Task RejectRiteLearnAsync(int characterRiteId, string? note, string storyTellerUserId);

    /// <summary>
    /// Resolves the activation pool for a learned rite and returns the dice count. Does not deduct Vitae/Willpower.
    /// </summary>
    Task<int> ResolveRiteActivationPoolAsync(int characterId, int characterRiteId, string userId);

    /// <summary>
    /// Validates acknowledgments, applies internal activation costs (Vitae, Willpower, stains), then returns the dice pool size.
    /// Costs are not refunded if the roll fails.
    /// </summary>
    Task<int> BeginRiteActivationAsync(
        int characterId,
        int characterRiteId,
        string userId,
        BeginRiteActivationRequest request);
}
