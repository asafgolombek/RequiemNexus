using RequiemNexus.Application.Models;
using RequiemNexus.Domain.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Touchstone-related mechanics (VtR 2e), including remorse rolls for Humanity stains.
/// </summary>
public interface ITouchstoneService
{
    /// <summary>
    /// Rolls remorse when the character has stains below the degeneration threshold: pool is Humanity dice (chance die at Humanity 0),
    /// +1 die if the character has at least one active Touchstone anchor (defined text and/or Touchstone Merit).
    /// Outcomes mirror degeneration: success clears stains; failure removes one Humanity and clears stains; dramatic failure also applies <c>Guilty</c>.
    /// </summary>
    /// <param name="characterId">Character rolling remorse.</param>
    /// <param name="userId">Authenticated user (owner or Storyteller).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Outcome summary, or failure when guards fail.</returns>
    Task<Result<DegenerationRollOutcome>> RollRemorseAsync(
        int characterId,
        string userId,
        CancellationToken cancellationToken = default);
}
