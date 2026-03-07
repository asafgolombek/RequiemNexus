using RequiemNexus.Domain.Contracts;

namespace RequiemNexus.Domain;

/// <summary>
/// Pure, stateless XP cost calculations for Vampire: The Requiem 2e advancement.
/// All methods are deterministic and free of side effects.
/// Register as Singleton in DI.
/// </summary>
public class ExperienceCostRules : IExperienceCostRules
{
    /// <summary>
    /// Generic upgrade cost calculation: sum of (dot × multiplier) for each new dot.
    /// Returns 0 if the upgrade is invalid (newRating &lt;= currentRating).
    /// This is kept static so entity classes can call it without DI.
    /// </summary>
    public static int CalculateUpgradeCost(int fromRating, int toRating, int costMultiplier)
    {
        if (toRating <= fromRating)
        {
            return 0;
        }

        int totalCost = 0;
        for (int i = fromRating + 1; i <= toRating; i++)
        {
            totalCost += i * costMultiplier;
        }

        return totalCost;
    }

    /// <summary>
    /// Attribute upgrade cost: each new dot costs (dot level × 4).
    /// Example: upgrading from 2 → 4 costs (3×4)+(4×4) = 28 XP.
    /// </summary>
    public int CalculateAttributeUpgradeCost(int fromRating, int toRating)
        => CalculateUpgradeCost(fromRating, toRating, costMultiplier: 4);

    /// <summary>
    /// Skill upgrade cost: each new dot costs (dot level × 2).
    /// </summary>
    public int CalculateSkillUpgradeCost(int fromRating, int toRating)
        => CalculateUpgradeCost(fromRating, toRating, costMultiplier: 2);

    /// <summary>
    /// Discipline upgrade cost: each new dot costs (dot level × 5).
    /// </summary>
    public int CalculateDisciplineUpgradeCost(int fromRating, int toRating)
        => CalculateUpgradeCost(fromRating, toRating, costMultiplier: 5);

    /// <summary>
    /// Merit purchase/upgrade cost: 1 XP per dot.
    /// Example: upgrading from 2 → 4 costs 2 XP.
    /// </summary>
    public int CalculateMeritCost(int fromRating, int toRating)
        => Math.Max(0, toRating - fromRating);
}
