using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Manages discipline purchases for characters, including XP deduction and ledger recording.
/// </summary>
public interface ICharacterDisciplineService
{
    /// <summary>Returns all disciplines available in the catalogue, ordered by name.</summary>
    Task<List<Discipline>> GetAvailableDisciplinesAsync();

    /// <summary>
    /// Purchases a discipline rating for the character. Deducts <paramref name="xpCost"/> XP and records the ledger entry.
    /// </summary>
    /// <param name="character">The character buying the discipline (must be tracked by EF).</param>
    /// <param name="disciplineId">The discipline to purchase.</param>
    /// <param name="rating">The purchased rating (1–5).</param>
    /// <param name="xpCost">Total XP to deduct.</param>
    Task<CharacterDiscipline> AddDisciplineAsync(Character character, int disciplineId, int rating, int xpCost);
}
