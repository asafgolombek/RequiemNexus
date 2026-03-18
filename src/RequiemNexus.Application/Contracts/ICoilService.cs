using RequiemNexus.Application.DTOs;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Application service for Ordo Dracul Coil/Scale management.
/// Enforces Ordo membership, prerequisite chain, XP costs, and ST approval flows.
/// </summary>
public interface ICoilService
{
    /// <summary>Returns all Scale definitions (always available for display).</summary>
    Task<List<ScaleSummaryDto>> GetScalesAsync();

    /// <summary>
    /// Returns Coils the character is eligible to purchase (Ordo member, prerequisite met, not already learned/pending).
    /// Includes XP cost calculated from Chosen Mystery and Crucible Ritual status.
    /// </summary>
    Task<List<CoilSummaryDto>> GetEligibleCoilsAsync(int characterId, string userId);

    /// <summary>
    /// Submits a Coil purchase request. XP is reserved until ST approval.
    /// Enforces Ordo membership, prerequisite chain, Ordo Status cap for non-chosen Coils.
    /// </summary>
    Task<CharacterCoil> RequestLearnCoilAsync(int characterId, int coilDefinitionId, string userId);

    /// <summary>Returns pending Coil purchase requests for characters in the campaign.</summary>
    Task<List<CoilApplicationDto>> GetPendingCoilApplicationsAsync(int campaignId, string storyTellerUserId);

    /// <summary>Approves a Coil purchase request. Deducts XP and sets Status to Approved.</summary>
    Task ApproveCoilLearnAsync(int characterCoilId, string? note, string storyTellerUserId);

    /// <summary>Rejects a Coil purchase request.</summary>
    Task RejectCoilLearnAsync(int characterCoilId, string? note, string storyTellerUserId);

    /// <summary>Player requests a Chosen Mystery selection. Requires Ordo membership. Awaits ST approval.</summary>
    Task RequestChosenMysteryAsync(int characterId, int scaleId, string userId);

    /// <summary>Storyteller approves the pending Chosen Mystery selection.</summary>
    Task ApproveChosenMysteryAsync(int characterId, string storyTellerUserId);

    /// <summary>Storyteller rejects the pending Chosen Mystery selection.</summary>
    Task RejectChosenMysteryAsync(int characterId, string storyTellerUserId);

    /// <summary>Storyteller grants Crucible Ritual access to a character.</summary>
    Task GrantCrucibleRitualAccessAsync(int characterId, string storyTellerUserId);

    /// <summary>Storyteller revokes Crucible Ritual access from a character.</summary>
    Task RevokeCrucibleRitualAccessAsync(int characterId, string storyTellerUserId);
}
