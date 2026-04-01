using RequiemNexus.Data.Models;
using RequiemNexus.Data.Models.Enums;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Pure eligibility helpers for Ordo Dracul Coil learning (membership, Status cap inputs, XP tiers).
/// </summary>
public static class CoilOrdoEligibility
{
    /// <summary>Name of the covenant that grants Coil access.</summary>
    public const string OrdoDraculName = "The Ordo Dracul";

    /// <summary>
    /// Returns whether the character is a full member of the Ordo Dracul (not pending covenant join).
    /// </summary>
    public static bool IsOrdoDraculMember(Character character)
    {
        return character.Covenant?.Name == OrdoDraculName
            && character.CovenantJoinStatus != CovenantJoinStatus.Pending;
    }

    /// <summary>
    /// XP cost per Coil dot: chosen mystery vs other, with optional Crucible Ritual discount.
    /// </summary>
    public static int CalculateXpCost(bool isChosenMystery, bool hasCrucibleAccess)
    {
        return (isChosenMystery, hasCrucibleAccess) switch
        {
            (true, true) => 2,
            (true, false) => 3,
            (false, true) => 3,
            (false, false) => 4,
        };
    }

    /// <summary>
    /// Counts Ordo Dracul Status merit dots from the character sheet (naming convention match).
    /// </summary>
    public static int GetOrdoStatusDots(Character character)
    {
        return character.Merits
            .Where(m => m.Merit != null
                && m.Merit.Name.Contains("Status", StringComparison.OrdinalIgnoreCase)
                && m.Merit.Name.Contains("Ordo", StringComparison.OrdinalIgnoreCase))
            .Sum(m => m.Rating);
    }
}
