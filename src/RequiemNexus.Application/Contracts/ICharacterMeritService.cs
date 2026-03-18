using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Manages merit purchases for characters, including XP deduction and ledger recording.
/// </summary>
public interface ICharacterMeritService
{
    /// <summary>
    /// Returns merits available for purchase by the character. Covenant-gated merits are included
    /// only when the character is an approved member of that covenant.
    /// </summary>
    /// <param name="character">The character (must have CovenantId/CovenantJoinStatus loaded).</param>
    Task<List<Merit>> GetAvailableMeritsAsync(Character character);

    /// <summary>
    /// Purchases a merit for the character. Deducts <paramref name="xpCost"/> XP and records the ledger entry.
    /// </summary>
    /// <param name="character">The character buying the merit (must be tracked by EF).</param>
    /// <param name="meritId">The merit to purchase.</param>
    /// <param name="specification">Optional free-text specifier (e.g. "Fighting Style: Boxing").</param>
    /// <param name="rating">The purchased rating (1–5).</param>
    /// <param name="xpCost">Total XP to deduct.</param>
    Task<CharacterMerit> AddMeritAsync(Character character, int meritId, string? specification, int rating, int xpCost);
}
