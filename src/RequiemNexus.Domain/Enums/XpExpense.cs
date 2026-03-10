namespace RequiemNexus.Domain.Enums;

/// <summary>
/// Identifies the trait category on which XP was spent.
/// Used by <c>XpLedgerEntry</c> to build an immutable audit trail.
/// </summary>
public enum XpExpense
{
    /// <summary>XP spent upgrading a core Attribute (e.g., Strength, Wits).</summary>
    Attribute,

    /// <summary>XP spent upgrading a Skill (e.g., Stealth, Persuasion).</summary>
    Skill,

    /// <summary>XP spent purchasing or upgrading a Discipline.</summary>
    Discipline,

    /// <summary>XP spent purchasing or upgrading a Merit.</summary>
    Merit,

    /// <summary>Manual adjustment (e.g., correcting an error).</summary>
    ManualAdjustment,
}
