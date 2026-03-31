namespace RequiemNexus.Domain.Events;

/// <summary>Reasons that can trigger a degeneration check for a character.</summary>
public enum DegenerationReason
{
    /// <summary>Humanity stains have crossed the threshold for the current Humanity dot.</summary>
    StainsThreshold,

    /// <summary>The character has purchased their first dot of Crúac at Humanity 4 or higher.</summary>
    CrúacPurchase,

    /// <summary>The character used a Kindred Necromancy ritual at Humanity 7 or higher.</summary>
    NecromancyActivation,
}

/// <summary>
/// Raised when a character requires a degeneration (Resolve + (7 − Humanity)) check.
/// Phase 17 handles <see cref="DegenerationReason.StainsThreshold"/>; Phase 19 raises <see cref="DegenerationReason.CrúacPurchase"/>.
/// </summary>
/// <param name="CharacterId">The character who must check for degeneration.</param>
/// <param name="Reason">Why the check was triggered.</param>
public record DegenerationCheckRequiredEvent(int CharacterId, DegenerationReason Reason);
