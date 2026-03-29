namespace RequiemNexus.Domain.Models;

/// <summary>
/// Named pool-scope strings for <see cref="ConditionPenaltyModifier.PoolTarget"/>.
/// Interpreted by application services when building <see cref="PassiveModifier"/> rows.
/// </summary>
public static class ConditionPoolTarget
{
    /// <summary>−2 to all dice pools (e.g. Shaken).</summary>
    public const string AllPools = "AllPools";

    /// <summary>−2 to pools that include a Physical skill (e.g. Exhausted).</summary>
    public const string PhysicalPools = "PhysicalPools";

    /// <summary>−2 to pools except those solely for fleeing the source of fear; resolver treats as all pools until pool intent metadata exists.</summary>
    public const string AllExceptFleeing = "AllExceptFleeing";

    /// <summary>−1 when the pool includes Resolve or Composure (e.g. Guilty).</summary>
    public const string ResolveComposure = "ResolveComposure";

    /// <summary>−2 to pools that include a Mental skill (e.g. Despondent).</summary>
    public const string MentalPools = "MentalPools";

    /// <summary>−1 when the pool includes Composure (e.g. Provoked).</summary>
    public const string Composure = "Composure";
}
