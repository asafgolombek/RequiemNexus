using RequiemNexus.Data.Models;
using RequiemNexus.Domain;

namespace RequiemNexus.Application.Contracts;

public interface IAdvancementService
{
    /// <summary>
    /// Attempts to upgrade an Attribute from <paramref name="currentRating"/> to <paramref name="newRating"/>,
    /// deducting XP from the character and writing an immutable XP ledger entry on success.
    /// </summary>
    /// <returns><c>true</c> when the upgrade succeeded; <c>false</c> when XP was insufficient.</returns>
    Task<bool> TryUpgradeCoreTrait(Character character, AttributeId id, int currentRating, int newRating, string? actingUserId = null);

    /// <summary>
    /// Attempts to upgrade a Skill from <paramref name="currentRating"/> to <paramref name="newRating"/>,
    /// deducting XP from the character and writing an immutable XP ledger entry on success.
    /// </summary>
    /// <returns><c>true</c> when the upgrade succeeded; <c>false</c> when XP was insufficient.</returns>
    Task<bool> TryUpgradeCoreTrait(Character character, SkillId id, int currentRating, int newRating, string? actingUserId = null);

    void UpdateCoreTrait(Character character, AttributeId id, int newRating);

    void UpdateCoreTrait(Character character, SkillId id, int newRating);

    void RecalculateDerivedStats(Character character);
}
