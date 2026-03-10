namespace RequiemNexus.Domain.Enums;

/// <summary>
/// Identifies how an XP credit was earned.
/// Used by <c>XpLedgerEntry</c> to build an immutable audit trail.
/// </summary>
public enum XpSource
{
    /// <summary>Five Beats were converted into one XP.</summary>
    BeatConversion,

    /// <summary>The Storyteller awarded XP directly (e.g., for exceptional roleplay or a milestone).</summary>
    StorytellerAward,

    /// <summary>Manual adjustment (e.g., correcting an error).</summary>
    ManualAdjustment,
}
