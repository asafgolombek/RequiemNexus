using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Dice-pool rolls for Social maneuvering: Open Door and Force Doors.
/// </summary>
public interface ISocialManeuverRollService
{
    /// <summary>
    /// Rolls to open one or two Doors. Initiator owner or Storyteller; enforces impression interval, cumulative failure dice, and player-declared pool cap vs sheet.
    /// </summary>
    /// <param name="maneuverId">Maneuver id.</param>
    /// <param name="dicePool">Declared pool before penalties (server-validated for non–Storyteller callers).</param>
    /// <param name="userId">Authenticated user id.</param>
    /// <returns>Updated maneuver, roll details, and doors opened; or a failure message.</returns>
    Task<Result<(SocialManeuver Updated, RollResult Roll, int DoorsOpened)>> RollOpenDoorAsync(
        int maneuverId,
        int dicePool,
        string userId);

    /// <summary>
    /// Forces remaining Doors with pool penalty equal to closed Doors. Optional hard leverage removes Doors first per Humanity gap.
    /// </summary>
    /// <param name="maneuverId">Maneuver id.</param>
    /// <param name="dicePool">Declared pool before penalties (server-validated for non–Storyteller callers).</param>
    /// <param name="applyHardLeverage">When true, <paramref name="breakingPointSeverity"/> adjusts closed Doors before the roll.</param>
    /// <param name="breakingPointSeverity">Breaking point severity used for hard leverage (0–10).</param>
    /// <param name="userId">Authenticated user id.</param>
    /// <returns>Updated maneuver, roll details, and whether forcing succeeded; or a failure message.</returns>
    Task<Result<(SocialManeuver Updated, RollResult Roll, bool ForcedSuccess)>> RollForceDoorsAsync(
        int maneuverId,
        int dicePool,
        bool applyHardLeverage,
        int breakingPointSeverity,
        string userId);
}
