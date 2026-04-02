namespace RequiemNexus.Domain.Enums;

/// <summary>
/// Canonical Conditions from Vampire: The Requiem 2nd Edition.
/// Conditions are persistent states that affect the character until resolved.
/// Most resolve for a Beat; some (marked Persistent) resolve only under specific story circumstances.
/// </summary>
public enum ConditionType
{
    // ── Emotional / Social ──────────────────────────────────────────────────

    /// <summary>Character feels responsible for a harm. Resolve: make amends or accept blame publicly.</summary>
    Guilty,

    /// <summary>Character is infatuated with another. Resolve: act against their own interests for the subject.</summary>
    Swooned,

    /// <summary>Character is tempted to act against their Touchstone or Virtue. Resolve: give in or spend Willpower to resist.</summary>
    Tempted,

    /// <summary>Character's confidence is broken. Resolve: succeed at a task without spending Willpower.</summary>
    Shaken,

    /// <summary>Character's reputation suffers publicly. Resolve: perform a significant public act of redemption.</summary>
    Notoriety,

    /// <summary>Someone holds something over the character. Resolve: remove the leverage or give in to the demand.</summary>
    Leveraged,

    // ── Mental ───────────────────────────────────────────────────────────────

    /// <summary>Character is overwhelmed. Suffers −2 to all actions. Resolve: rest or succeed at a Composure roll.</summary>
    Exhausted,

    /// <summary>Character is in a state of hopeless despair. Resolve: receive hope from an outside source.</summary>
    Despondent,

    /// <summary>Character is deeply frightened. Resolve: flee from the source of fear or overcome it.</summary>
    Frightened,

    // ── Physical ─────────────────────────────────────────────────────────────

    /// <summary>Character is bleeding out. Suffers 1 Lethal damage per turn. Resolve: successful Medicine roll.</summary>
    Bleeding,

    /// <summary>Character is on fire. Suffers escalating Aggravated damage. Resolve: extinguish flames.</summary>
    OnFire,

    // ── Vampire-Specific ─────────────────────────────────────────────────────

    /// <summary>Character suffers from the touch of fire or sunlight. Resolves when the source is removed.</summary>
    Immolating,

    /// <summary>Character is in Wassail (frenzy of hunger). Resolve: spend Willpower, regain Humanity, or be restrained.</summary>
    Wassail,

    /// <summary>Character's Beast is close to the surface. −1 to Composure rolls. Resolve: avoid Beast triggers for a scene.</summary>
    Provoked,

    // ── Custom ───────────────────────────────────────────────────────────────

    /// <summary>A Storyteller-defined condition not covered by the canonical list.</summary>
    Custom,

    /// <summary>
    /// Chronicles of Darkness — energized by success. Resolve when you relinquish the bonus this Condition provides.
    /// Appended after <see cref="ConditionType.Custom"/> to preserve stored enum values for existing rows.
    /// </summary>
    Inspired,

    /// <summary>
    /// Vampire Blood Bond Stage 1. The thrall craves the regnant's blood above all else.
    /// Persistent: fades when the Blood Bond drops below Stage 1 (every 30 days if untreated).
    /// Does not award a Beat on removal — it is an addiction, not a drama resolution.
    /// Appended after <see cref="ConditionType.Inspired"/> to preserve stored enum values for existing rows.
    /// </summary>
    Addicted,

    /// <summary>
    /// Vampire Blood Bond Stage 3 (Full Vinculum). The thrall cannot voluntarily act against
    /// the regnant. Persistent Condition — resolves only when the bond fully fades.
    /// Awards a Beat on resolution.
    /// Appended after <see cref="ConditionType.Addicted"/> to preserve stored enum values for existing rows.
    /// </summary>
    Bound,

    /// <summary>
    /// Theban Sorcery — dramatic failure on a miracle. Resolve per Storyteller guidance (V:tR 2e ritual outcomes).
    /// Appended after <see cref="ConditionType.Bound"/> for stable serialization.
    /// </summary>
    Humbled,

    /// <summary>
    /// Crúac — exceptional success during a rite. Resolve per Storyteller guidance (V:tR 2e ritual outcomes).
    /// </summary>
    Ecstatic,

    /// <summary>
    /// Theban Sorcery — exceptional success during a miracle. Resolve per Storyteller guidance (V:tR 2e ritual outcomes).
    /// </summary>
    Raptured,

    /// <summary>
    /// Extended ritual: continued after a failed roll (0 successes). Suffers −2 on the next dice pool; resolve when taken (CoD Stumbled).
    /// </summary>
    Stumbled,
}
