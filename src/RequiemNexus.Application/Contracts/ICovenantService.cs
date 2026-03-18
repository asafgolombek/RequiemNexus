using RequiemNexus.Application.DTOs;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Application service for Covenant join/leave and Storyteller approval workflow.
/// </summary>
public interface ICovenantService
{
    /// <summary>
    /// Returns playable covenants the character can apply for.
    /// </summary>
    Task<List<CovenantSummaryDto>> GetEligibleCovenantsAsync(int characterId, string userId);

    /// <summary>
    /// Creates a covenant application (Status Pending). Requires character in campaign.
    /// </summary>
    Task ApplyForCovenantAsync(int characterId, int covenantDefinitionId, string userId);

    /// <summary>
    /// Returns pending covenant applications for characters in the campaign.
    /// </summary>
    Task<List<CovenantApplicationDto>> GetPendingCovenantApplicationsAsync(int campaignId, string storyTellerUserId);

    /// <summary>
    /// Approves a covenant application.
    /// </summary>
    Task ApproveCovenantAsync(int characterId, string? note, string storyTellerUserId);

    /// <summary>
    /// Rejects a covenant application.
    /// </summary>
    Task RejectCovenantAsync(int characterId, string? note, string storyTellerUserId);

    /// <summary>
    /// Storyteller kicks a character from their covenant.
    /// </summary>
    Task KickFromCovenantAsync(int characterId, string storyTellerUserId);

    /// <summary>
    /// Player requests to leave their covenant. Requires ST approval.
    /// </summary>
    Task RequestLeaveCovenantAsync(int characterId, string userId);

    /// <summary>
    /// Storyteller approves a leave request.
    /// </summary>
    Task ApproveLeaveRequestAsync(int characterId, string storyTellerUserId);

    /// <summary>
    /// Storyteller rejects a leave request.
    /// </summary>
    Task RejectLeaveRequestAsync(int characterId, string storyTellerUserId);
}
