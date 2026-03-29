namespace RequiemNexus.Domain.Models;

/// <summary>
/// Kind of resource spent to activate a Discipline power.
/// </summary>
public enum ActivationCostType
{
    /// <summary>No spend (free power).</summary>
    None,

    /// <summary>Vitae from the character pool.</summary>
    Vitae,

    /// <summary>Willpower dots.</summary>
    Willpower,
}
