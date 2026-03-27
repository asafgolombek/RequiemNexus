using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Application.Models;

/// <summary>
/// Outcome of a frenzy resistance roll.
/// </summary>
/// <param name="Successes">Number of successes on the roll.</param>
/// <param name="Saved">True when the character resisted (at least one success).</param>
/// <param name="WillpowerSpent">True when one Willpower was spent to drop a die from the pool.</param>
/// <param name="Trigger">The trigger that provoked the roll.</param>
/// <param name="TiltApplied">Tilt applied on failure, if any.</param>
/// <param name="SuppressedDueToBeastAlreadyActive">True when an active Beast tilt blocked a new roll.</param>
public record FrenzySaveResult(
    int Successes,
    bool Saved,
    bool WillpowerSpent,
    FrenzyTrigger Trigger,
    TiltType? TiltApplied,
    bool SuppressedDueToBeastAlreadyActive);
