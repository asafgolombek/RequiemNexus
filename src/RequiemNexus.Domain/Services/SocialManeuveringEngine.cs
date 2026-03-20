using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Models;

namespace RequiemNexus.Domain.Services;

/// <summary>
/// Pure VtR 2e Social maneuvering math (Doors, intervals, forcing, hard leverage).
/// </summary>
public static class SocialManeuveringEngine
{
    /// <summary>
    /// Computes initial Door count from the victim's Resolve/Composure and stated goal modifiers.
    /// </summary>
    /// <param name="victimResolve">Victim Resolve dots (clamped 1–5).</param>
    /// <param name="victimComposure">Victim Composure dots (clamped 1–5).</param>
    /// <param name="goalWouldBeBreakingPoint">+2 Doors when the goal would be a breaking point for the victim.</param>
    /// <param name="goalPreventsAspiration">+1 when the goal would prevent resolving an Aspiration.</param>
    /// <param name="actsAgainstVirtueOrMask">+1 when acting against the victim's Virtue (or Mask for Kindred).</param>
    /// <returns>Total initial Doors (at least 1).</returns>
    public static int ComputeInitialDoorCount(
        int victimResolve,
        int victimComposure,
        bool goalWouldBeBreakingPoint,
        bool goalPreventsAspiration,
        bool actsAgainstVirtueOrMask)
    {
        int r = ClampDot(victimResolve);
        int c = ClampDot(victimComposure);
        int baseDoors = Math.Min(r, c);
        int doors = baseDoors;
        if (goalWouldBeBreakingPoint)
        {
            doors += 2;
        }

        if (goalPreventsAspiration)
        {
            doors += 1;
        }

        if (actsAgainstVirtueOrMask)
        {
            doors += 1;
        }

        return Math.Max(1, doors);
    }

    /// <summary>
    /// Minimum real-time spacing between open-Door rolls for the current impression (Nexus clock).
    /// </summary>
    /// <returns><see langword="null"/> when no roll is allowed (Hostile).</returns>
    public static TimeSpan? GetMinimumIntervalBetweenOpenDoorRolls(ImpressionLevel impression)
    {
        return impression switch
        {
            ImpressionLevel.Hostile => null,
            ImpressionLevel.Average => TimeSpan.FromDays(7),
            ImpressionLevel.Good => TimeSpan.FromDays(1),
            ImpressionLevel.Excellent => TimeSpan.FromHours(1),
            ImpressionLevel.Perfect => TimeSpan.Zero,
            _ => TimeSpan.FromDays(7),
        };
    }

    /// <summary>
    /// Validates that an open-Door roll may occur at <paramref name="nowUtc"/> given the last roll and impression.
    /// </summary>
    public static Result<bool> ValidateOpenDoorRollTiming(
        DateTimeOffset? lastRollAtUtc,
        ImpressionLevel impression,
        DateTimeOffset nowUtc)
    {
        TimeSpan? interval = GetMinimumIntervalBetweenOpenDoorRolls(impression);
        if (interval is null)
        {
            return Result<bool>.Failure("Cannot roll to open Doors while impression is Hostile.");
        }

        if (lastRollAtUtc is null)
        {
            return Result<bool>.Success(true);
        }

        DateTimeOffset nextEligible = lastRollAtUtc.Value + interval.Value;
        if (nowUtc < nextEligible)
        {
            return Result<bool>.Failure(
                $"Next open-Door roll is not available until {nextEligible:O} (UTC).");
        }

        return Result<bool>.Success(true);
    }

    /// <summary>
    /// Doors opened by a single open-Door attempt from success counts (exceptional = 5+ successes in CoD).
    /// </summary>
    public static int GetDoorsOpenedByOpenDoorRoll(int successCount, bool isExceptionalSuccess, bool isDramaticFailure)
    {
        if (isDramaticFailure)
        {
            return 0;
        }

        if (isExceptionalSuccess)
        {
            return 2;
        }

        if (successCount >= 1)
        {
            return 1;
        }

        return 0;
    }

    /// <summary>
    /// Dice pool penalty when forcing Doors: equal to closed Doors after any hard-leverage removal.
    /// </summary>
    public static int ComputeForceRollPoolPenalty(int remainingClosedDoors) =>
        Math.Max(0, remainingClosedDoors);

    /// <summary>
    /// Doors removed by hard leverage before the force roll, from breaking-point severity vs persuader Humanity.
    /// </summary>
    /// <param name="breakingPointSeverity">ST-assigned severity of the breaking point inflicted on the persuader.</param>
    /// <param name="persuaderHumanity">Persuader's current Humanity.</param>
    /// <returns>1 or 2 Doors to remove, or failure when inputs are invalid.</returns>
    public static Result<int> ComputeHardLeverageDoorsRemoved(int breakingPointSeverity, int persuaderHumanity)
    {
        if (breakingPointSeverity < 0)
        {
            return Result<int>.Failure("Breaking point severity cannot be negative.");
        }

        int diff = Math.Abs(breakingPointSeverity - persuaderHumanity);
        int removed = diff <= 2 ? 1 : 2;
        return Result<int>.Success(removed);
    }

    /// <summary>
    /// One week at Hostile impression ends the maneuver in failure.
    /// </summary>
    public static bool ShouldFailFromHostileWeek(
        DateTimeOffset? hostileSinceUtc,
        ImpressionLevel impression,
        DateTimeOffset nowUtc)
    {
        if (impression != ImpressionLevel.Hostile || hostileSinceUtc is null)
        {
            return false;
        }

        return nowUtc >= hostileSinceUtc.Value.AddDays(7);
    }

    private static int ClampDot(int value) => Math.Clamp(value, 1, 5);
}
