using RequiemNexus.Application.DTOs;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Application service for bloodline applications and Storyteller approval workflow.
/// </summary>
public interface IBloodlineService
{
    /// <summary>
    /// Returns bloodlines the character is eligible to apply for (clan match, BP 2+, no existing Active or Pending).
    /// </summary>
    /// <param name="characterId">The character.</param>
    /// <param name="userId">Must be the character's owner.</param>
    Task<List<BloodlineSummaryDto>> GetEligibleBloodlinesAsync(int characterId, string userId);

    /// <summary>
    /// Creates a bloodline application with Status Pending.
    /// Requires character in campaign, owner only, no Active or Pending.
    /// </summary>
    /// <param name="characterId">The character.</param>
    /// <param name="bloodlineDefinitionId">The bloodline to apply for.</param>
    /// <param name="userId">Must be the character's owner.</param>
    Task<CharacterBloodline> ApplyForBloodlineAsync(int characterId, int bloodlineDefinitionId, string userId);

    /// <summary>
    /// Returns pending bloodline applications for characters in the campaign.
    /// </summary>
    /// <param name="campaignId">The campaign.</param>
    /// <param name="storyTellerUserId">Must be the campaign Storyteller.</param>
    Task<List<BloodlineApplicationDto>> GetPendingBloodlineApplicationsAsync(int campaignId, string storyTellerUserId);

    /// <summary>
    /// Approves a bloodline application. Rejects any other Pending for that character.
    /// </summary>
    /// <param name="characterBloodlineId">The application to approve.</param>
    /// <param name="note">Optional note from the Storyteller.</param>
    /// <param name="storyTellerUserId">Must be the campaign Storyteller.</param>
    Task ApproveBloodlineAsync(int characterBloodlineId, string? note, string storyTellerUserId);

    /// <summary>
    /// Rejects a bloodline application.
    /// </summary>
    /// <param name="characterBloodlineId">The application to reject.</param>
    /// <param name="note">Optional note from the Storyteller.</param>
    /// <param name="storyTellerUserId">Must be the campaign Storyteller.</param>
    Task RejectBloodlineAsync(int characterBloodlineId, string? note, string storyTellerUserId);

    /// <summary>
    /// Removes an active bloodline from a character. Cleans up devotions that require the bloodline.
    /// </summary>
    /// <param name="characterBloodlineId">The active bloodline membership to remove.</param>
    /// <param name="userId">Must be the character's owner.</param>
    Task RemoveBloodlineAsync(int characterBloodlineId, string userId);
}
