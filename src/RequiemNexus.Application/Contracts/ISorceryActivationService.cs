using RequiemNexus.Application.DTOs;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Handles rite activation pool resolution and cost application.
/// </summary>
public interface ISorceryActivationService
{
    /// <summary>
    /// Validates acknowledgments, applies internal activation costs (Vitae, Willpower, stains),
    /// then returns the dice pool size. Costs are not refunded if the roll fails.
    /// </summary>
    Task<int> BeginRiteActivationAsync(
        int characterId,
        int characterRiteId,
        string userId,
        BeginRiteActivationRequest request);
}
