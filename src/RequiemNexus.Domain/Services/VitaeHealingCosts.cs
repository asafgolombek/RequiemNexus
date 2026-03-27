using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Models;

namespace RequiemNexus.Domain.Services;

/// <summary>
/// Canonical Vitae costs for healing actions. Full resting intervals remain ST-facing until automated.
/// </summary>
public static class VitaeHealingCosts
{
    /// <summary>Vitae per bashing box removed via <see cref="HealingReason.FastHealBashing"/>.</summary>
    public const int VitaePerBashingBox = 1;

    /// <summary>
    /// Returns the Vitae cost for healing <paramref name="boxCount"/> boxes, or a failure message.
    /// </summary>
    /// <param name="reason">Healing path.</param>
    /// <param name="boxCount">Number of health boxes to heal (must be positive).</param>
    public static Result<int> TryGetVitaeCost(HealingReason reason, int boxCount)
    {
        if (boxCount <= 0)
        {
            return Result<int>.Failure("Box count must be positive.");
        }

        return reason switch
        {
            HealingReason.FastHealBashing => Result<int>.Success(boxCount * VitaePerBashingBox),
            HealingReason.HealLethal => Result<int>.Failure(
                "Automated lethal healing is not implemented; resolve with the Storyteller."),
            HealingReason.HealAggravated => Result<int>.Failure(
                "Automated aggravated healing is not implemented; resolve with the Storyteller."),
            _ => Result<int>.Failure("Unknown healing reason."),
        };
    }
}
