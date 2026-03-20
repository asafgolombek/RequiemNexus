namespace RequiemNexus.Domain.Enums;

/// <summary>
/// Victim's impression of the persuader; sets the minimum interval between rolls to open a Door (VtR 2e, Social Maneuvering).
/// </summary>
public enum ImpressionLevel
{
    /// <summary>Cannot roll to open Doors.</summary>
    Hostile = 0,

    /// <summary>One roll per week.</summary>
    Average = 1,

    /// <summary>One roll per day.</summary>
    Good = 2,

    /// <summary>One roll per hour.</summary>
    Excellent = 3,

    /// <summary>One roll per turn.</summary>
    Perfect = 4,
}
