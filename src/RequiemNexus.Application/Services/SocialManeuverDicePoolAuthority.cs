using Microsoft.EntityFrameworkCore;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain;
using RequiemNexus.Domain.Services;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Server-side bounds for declared Social maneuver dice pools using the initiator's sheet traits.
/// </summary>
public static class SocialManeuverDicePoolAuthority
{
    /// <summary>
    /// Maximum dice in a single Social maneuver roll derived from the largest standard Attribute + Skill pool on the sheet.
    /// Does not model specialty bonuses or situational modifiers; Storyteller-declared pools skip this cap.
    /// </summary>
    /// <param name="character">Initiator with <see cref="Character.Attributes"/> and <see cref="Character.Skills"/> populated.</param>
    /// <returns>Upper bound for player-declared pool (at least 0).</returns>
    public static int GetMaximumSocialDicePool(Character character)
    {
        return SocialManeuveringEngine.ComputeMaximumSocialApproachDicePool(
            character.GetAttributeRating(AttributeId.Intelligence),
            character.GetAttributeRating(AttributeId.Wits),
            character.GetAttributeRating(AttributeId.Manipulation),
            character.GetAttributeRating(AttributeId.Presence),
            character.GetSkillRating(SkillId.Empathy),
            character.GetSkillRating(SkillId.Expression),
            character.GetSkillRating(SkillId.Intimidation),
            character.GetSkillRating(SkillId.Persuasion),
            character.GetSkillRating(SkillId.Socialize),
            character.GetSkillRating(SkillId.Streetwise),
            character.GetSkillRating(SkillId.Subterfuge));
    }

    /// <summary>
    /// Loads the initiator with traits required for <see cref="GetMaximumSocialDicePool"/>.
    /// </summary>
    public static async Task<Character?> LoadInitiatorForDiceCapAsync(ApplicationDbContext db, int characterId)
    {
        return await db.Characters
            .Include(c => c.Attributes)
            .Include(c => c.Skills)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Id == characterId);
    }
}
