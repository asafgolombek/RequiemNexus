using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>Validates discipline assignments during character creation.</summary>
public interface ICharacterCreationService
{
    /// <summary>
    /// Validates that at least 2 of the 3 creation Discipline dots are in-clan.
    /// Call on each discipline change during creation, and again on final submit.
    /// </summary>
    /// <param name="character">The in-progress character with Disciplines populated.</param>
    /// <returns>Success when the rule is satisfied or total dots are still below 3.</returns>
    Result<bool> ValidateCreationDisciplines(Character character);
}
