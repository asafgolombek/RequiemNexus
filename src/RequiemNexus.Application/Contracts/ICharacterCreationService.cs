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

    /// <summary>
    /// Validates that every discipline on the character is allowed at chargen (covenant, bloodline-only, Necromancy, mentor-blood gates; no ST overrides).
    /// </summary>
    /// <param name="character">The character with <see cref="Character.Clan"/> populated where possible.</param>
    /// <param name="disciplinesById">All discipline rows referenced by <see cref="Character.Disciplines"/>, keyed by id (include Covenant and Bloodline when loaded).</param>
    /// <returns>Success when every assignment passes; failure with a player-facing message otherwise.</returns>
    Result<bool> ValidateCreationDisciplineEligibility(Character character, IReadOnlyDictionary<int, Discipline> disciplinesById);
}
