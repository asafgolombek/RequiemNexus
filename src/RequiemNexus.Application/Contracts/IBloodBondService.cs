using RequiemNexus.Application.DTOs;
using RequiemNexus.Domain.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Storyteller and player-facing orchestration for Blood Bonds (Vinculum), Conditions, and fading alerts.
/// </summary>
public interface IBloodBondService
{
    /// <summary>
    /// Records a feeding event. If no bond exists, creates Stage 1.
    /// If a Stage 1 or 2 bond exists, escalates by one stage.
    /// Stage 3 bonds only refresh the bond's last-fed timestamp.
    /// </summary>
    /// <param name="request">Thrall and regnant identity.</param>
    /// <param name="userId">Authenticated user (must be Storyteller).</param>
    /// <returns>The bond after the operation, or a failure message.</returns>
    Task<Result<BloodBondDto>> RecordFeedingAsync(RecordFeedingRequest request, string userId);

    /// <summary>Returns all bonds where the given character is the thrall.</summary>
    /// <param name="characterId">Thrall character id.</param>
    /// <param name="userId">Owner or Storyteller.</param>
    Task<IReadOnlyList<BloodBondDto>> GetBondsForThrallAsync(int characterId, string userId);

    /// <summary>Returns all bonds in a chronicle (Storyteller view).</summary>
    Task<IReadOnlyList<BloodBondDto>> GetBondsInChronicleAsync(int chronicleId, string userId);

    /// <summary>
    /// Manually decrements a bond by one stage (e.g., story resolution, time passing).
    /// At Stage 1, removes the bond entirely.
    /// </summary>
    Task<Result<Unit>> FadeBondAsync(int bondId, string userId);

    /// <summary>Returns bonds in the chronicle that are past the fading threshold.</summary>
    Task<IReadOnlyList<BloodBondDto>> GetFadingAlertsAsync(int chronicleId, string userId);
}
