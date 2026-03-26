using RequiemNexus.Application.DTOs;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Handles rite activation pool resolution and cost application.
/// </summary>
public interface ISorceryActivationService
{
    /// <summary>
    /// Resolves the activation pool for a learned rite and returns the dice count.
    /// Does not deduct Vitae or Willpower.
    /// </summary>
    Task<int> ResolveRiteActivationPoolAsync(int characterId, int characterRiteId, string userId);

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
