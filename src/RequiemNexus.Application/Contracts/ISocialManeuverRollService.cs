using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Dice-pool rolls for Social maneuvering: Open Door and Force Doors.
/// </summary>
public interface ISocialManeuverRollService
{
    /// <summary>
    /// Rolls to open one or two Doors. Initiator owner or Storyteller; enforces impression interval and cumulative failure dice.
    /// </summary>
    Task<(SocialManeuver Updated, RollResult Roll, int DoorsOpened)> RollOpenDoorAsync(
        int maneuverId,
        int dicePool,
        string userId);

    /// <summary>
    /// Forces remaining Doors with pool penalty equal to closed Doors. Optional hard leverage removes Doors first per Humanity gap.
    /// </summary>
    Task<(SocialManeuver Updated, RollResult Roll, bool ForcedSuccess)> RollForceDoorsAsync(
        int maneuverId,
        int dicePool,
        bool applyHardLeverage,
        int breakingPointSeverity,
        string userId);
}
