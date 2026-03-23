using RequiemNexus.Application.DTOs;
using RequiemNexus.Domain.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Application orchestration for ghoul retainers: feeding, aging alerts, Discipline access, and release.
/// </summary>
public interface IGhoulManagementService
{
    /// <summary>Creates a new ghoul record for the chronicle.</summary>
    Task<Result<GhoulDto>> CreateGhoulAsync(CreateGhoulRequest request, string userId);

    /// <summary>Updates a ghoul's details (name, notes, apparent/actual age).</summary>
    Task<Result<GhoulDto>> UpdateGhoulAsync(UpdateGhoulRequest request, string userId);

    /// <summary>
    /// Records a feeding event (regnant provides Vitae to ghoul).
    /// Updates LastFedAt to now. Sets VitaeInSystem to 1.
    /// </summary>
    Task<Result<GhoulDto>> FeedGhoulAsync(int ghoulId, string userId);

    /// <summary>Releases the ghoul from service. Sets IsReleased = true and ReleasedAt = now.</summary>
    Task<Result<Unit>> ReleaseGhoulAsync(int ghoulId, string userId);

    /// <summary>
    /// Sets the Discipline IDs the ghoul can access (at rating 1).
    /// When the regnant is a linked PC, each ID must be in-clan for that character and the count must not exceed Blood Potency.
    /// NPC or display-name regnants skip validation (ST-trusted).
    /// </summary>
    Task<Result<Unit>> SetDisciplineAccessAsync(int ghoulId, IReadOnlyList<int> disciplineIds, string userId);

    /// <summary>Returns all active (non-released) ghouls in the chronicle.</summary>
    Task<IReadOnlyList<GhoulDto>> GetGhoulsForChronicleAsync(int chronicleId, string userId);

    /// <summary>Returns ghouls that are overdue for feeding (aging alerts).</summary>
    Task<IReadOnlyList<GhoulAgingAlertDto>> GetAgingAlertsAsync(int chronicleId, string userId);

    /// <summary>
    /// Returns active ghouls bound to the given regnant character (player or ST read via character access).
    /// </summary>
    Task<IReadOnlyList<GhoulDto>> GetGhoulsForRegnantAsync(int regnantCharacterId, string userId);
}
