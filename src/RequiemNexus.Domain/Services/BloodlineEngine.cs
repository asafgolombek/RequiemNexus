using RequiemNexus.Domain.Models;

namespace RequiemNexus.Domain.Services;

/// <summary>
/// Stateless domain service that validates bloodline join prerequisites.
/// Never knows a bloodline by name; works purely from definition data.
/// </summary>
public static class BloodlineEngine
{
    /// <summary>
    /// Validates whether a character meets the prerequisites to apply for a bloodline.
    /// </summary>
    /// <param name="characterClanId">The character's current clan ID (nullable if no clan).</param>
    /// <param name="characterBloodPotency">The character's Blood Potency.</param>
    /// <param name="allowedParentClanIds">The bloodline's allowed parent clan IDs.</param>
    /// <param name="prerequisiteBloodPotency">The bloodline's required Blood Potency (default 2).</param>
    /// <returns>Success if valid; Failure with message if prerequisites not met.</returns>
    public static Result<bool> ValidateJoinPrerequisites(
        int? characterClanId,
        int characterBloodPotency,
        IReadOnlyList<int> allowedParentClanIds,
        int prerequisiteBloodPotency = 2)
    {
        if (!characterClanId.HasValue)
        {
            return Result<bool>.Failure("Character must belong to a clan to join a bloodline.");
        }

        if (characterBloodPotency < prerequisiteBloodPotency)
        {
            return Result<bool>.Failure($"Blood Potency {characterBloodPotency} is below the required {prerequisiteBloodPotency}.");
        }

        if (allowedParentClanIds.Count == 0)
        {
            return Result<bool>.Failure("Bloodline has no defined parent clans.");
        }

        if (!allowedParentClanIds.Contains(characterClanId.Value))
        {
            return Result<bool>.Failure("Character's clan is not an allowed parent clan for this bloodline.");
        }

        return Result<bool>.Success(true);
    }
}
