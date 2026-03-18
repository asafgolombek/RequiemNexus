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
    /// Purchases a discipline rating for the character. Calculates XP cost based on in-clan status.
    /// </summary>
    /// <param name="character">The character buying the discipline (must be tracked by EF).</param>
    /// <param name="disciplineId">The discipline to purchase.</param>
    /// <param name="rating">The purchased rating (1–5).</param>
    /// <param name="userId">The ID of the user performing the purchase.</param>
    Task<CharacterDiscipline> AddDisciplineAsync(Character character, int disciplineId, int rating, string? userId);

    /// <summary>
    /// Attempts to upgrade a character's discipline to a higher rating.
    /// Deducts XP based on in-clan status.
    /// </summary>
    /// <returns>True if upgrade successful; false if insufficient XP.</returns>
    Task<bool> TryUpgradeDisciplineAsync(Character character, int characterDisciplineId, int newRating, string? userId);
}
