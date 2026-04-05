using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.DTOs;

/// <summary>
/// Beats and experience totals after a progression mutation — lets the sheet patch in-memory state without a full character reload.
/// </summary>
/// <param name="Beats">Current beat count.</param>
/// <param name="ExperiencePoints">Unspent experience.</param>
/// <param name="TotalExperiencePoints">Lifetime experience (including spent).</param>
public sealed record CharacterProgressionSnapshotDto(int Beats, int ExperiencePoints, int TotalExperiencePoints)
{
    /// <summary>
    /// Builds a snapshot from the character's current field values.
    /// </summary>
    public static CharacterProgressionSnapshotDto FromCharacter(Character character) =>
        new(character.Beats, character.ExperiencePoints, character.TotalExperiencePoints);
}
