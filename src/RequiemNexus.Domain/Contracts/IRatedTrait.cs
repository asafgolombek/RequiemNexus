namespace RequiemNexus.Domain.Contracts;

/// <summary>
/// Shared interface for any character trait that has a numeric rating and an XP upgrade cost.
/// Implemented by CharacterAttribute, CharacterSkill, CharacterDiscipline, and CharacterMerit.
/// </summary>
public interface IRatedTrait
{
    string Name { get; }

    int Rating { get; }

    /// <summary>
    /// Calculates the total XP cost to upgrade this trait from its current Rating to <paramref name="toRating"/>.
    /// </summary>
    int CalculateUpgradeCost(int toRating);

    /// <summary>
    /// Upgrades the trait to a new rating after validating it is an increase.
    /// Returns the XP cost of the upgrade. Does not deduct XP.
    /// </summary>
    int Upgrade(int toRating, IExperienceCostRules rules);
}
