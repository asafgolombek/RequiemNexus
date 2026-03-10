namespace RequiemNexus.Domain.Enums;

/// <summary>
/// Identifies the in-game event that caused a Beat to be awarded.
/// Used by <c>BeatLedgerEntry</c> to build an immutable audit trail.
/// </summary>
public enum BeatSource
{
    /// <summary>A dramatic failure on a dice roll.</summary>
    DramaticFailure,

    /// <summary>A Condition was resolved, which automatically awards a Beat.</summary>
    ConditionResolved,

    /// <summary>An Aspiration was fulfilled.</summary>
    AspirationFulfilled,

    /// <summary>The Storyteller awarded a Beat manually (e.g., for good roleplay).</summary>
    StorytellerAward,

    /// <summary>A session-end award granted by the Storyteller to the whole coterie.</summary>
    SessionAward,

    /// <summary>Manual adjustment (e.g., correcting an error).</summary>
    ManualAdjustment,
}
