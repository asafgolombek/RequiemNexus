namespace RequiemNexus.Domain.Contracts;

/// <summary>
/// Interface for XP cost calculations. Enables DI and testability.
/// </summary>
public interface IExperienceCostRules
{
    int CalculateAttributeUpgradeCost(int fromRating, int toRating);

    int CalculateSkillUpgradeCost(int fromRating, int toRating);

    int CalculateDisciplineUpgradeCost(int fromRating, int toRating, bool isInClan);

    int CalculateMeritCost(int fromRating, int toRating);
}
