using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Domain.Contracts;

/// <summary>
/// Pure domain rules for Conditions and Tilts.
/// Contains no I/O — safe to use in unit tests without a database.
/// </summary>
public interface IConditionRules
{
    /// <summary>Returns the canonical description text for a <see cref="ConditionType"/>.</summary>
    string GetConditionDescription(ConditionType type);

    /// <summary>Returns the canonical description text for a <see cref="TiltType"/>.</summary>
    string GetTiltDescription(TiltType type);

    /// <summary>
    /// Returns <c>true</c> when resolving this Condition awards a Beat.
    /// All canonical Conditions award a Beat on resolution; Custom conditions do not by default.
    /// </summary>
    bool AwardsBeatOnResolve(ConditionType type);

    /// <summary>
    /// Returns a human-readable list of mechanical effects imposed by the given active Tilts.
    /// Used to surface active penalties on the character sheet.
    /// </summary>
    IReadOnlyList<string> GetTiltEffects(IEnumerable<TiltType> activeTilts);
}
