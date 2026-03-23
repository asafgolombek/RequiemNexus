using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Domain;

/// <summary>
/// Stateless rules for the Blood Bond (Vinculum) per V:tR 2e p. 154.
/// </summary>
public static class BloodBondRules
{
    /// <summary>
    /// The bond begins fading if the thrall has not fed from the regnant
    /// for longer than <see cref="FadingThreshold"/>.
    /// Interpretation: fixed 30-day interval (see rules-interpretations.md Phase 12).
    /// </summary>
    public static readonly TimeSpan FadingThreshold = TimeSpan.FromDays(30);

    /// <summary>Returns true when the bond is past the fading threshold.</summary>
    /// <param name="lastFedAt">Last time the thrall fed from this regnant, if recorded.</param>
    /// <param name="now">Current instant (UTC).</param>
    /// <returns>True when never fed or elapsed time strictly exceeds <see cref="FadingThreshold"/>.</returns>
    public static bool IsFading(DateTime? lastFedAt, DateTime now) =>
        lastFedAt is null || (now - lastFedAt.Value) > FadingThreshold;

    /// <summary>
    /// Returns the <see cref="ConditionType"/> that should be active for a given bond stage.
    /// </summary>
    /// <param name="stage">Bond stage from 1 to 3.</param>
    /// <returns>The condition type for that stage.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Stage is not 1, 2, or 3.</exception>
    public static ConditionType ConditionForStage(int stage) => stage switch
    {
        1 => ConditionType.Addicted,
        2 => ConditionType.Swooned,
        3 => ConditionType.Bound,
        _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, "Stage must be 1, 2, or 3."),
    };

    /// <summary>Whether resolving the Condition for a given stage awards a Beat when the bond fades.</summary>
    /// <param name="stage">The stage being left (resolved).</param>
    /// <returns>True only for Stage 3 (<see cref="ConditionType.Bound"/>).</returns>
    public static bool StageResolutionAwardsBeat(int stage) => stage == 3;
}
