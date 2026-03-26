using RequiemNexus.Application.DTOs;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Read-only queries for Blood Bonds (Vinculum): thrall view, chronicle view, and fading alerts.
/// </summary>
public interface IBloodBondQueryService
{
    /// <summary>Returns all bonds where the given character is the thrall.</summary>
    /// <param name="characterId">Thrall character id.</param>
    /// <param name="userId">Owner or Storyteller.</param>
    Task<IReadOnlyList<BloodBondDto>> GetBondsForThrallAsync(int characterId, string userId);

    /// <summary>Returns all bonds in a chronicle (Storyteller view).</summary>
    Task<IReadOnlyList<BloodBondDto>> GetBondsInChronicleAsync(int chronicleId, string userId);

    /// <summary>Returns bonds in the chronicle that are past the fading threshold.</summary>
    Task<IReadOnlyList<BloodBondDto>> GetFadingAlertsAsync(int chronicleId, string userId);
}
