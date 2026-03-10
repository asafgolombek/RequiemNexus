namespace RequiemNexus.Domain.Enums;

/// <summary>Describes the political stance one faction holds toward another.</summary>
public enum FactionStance
{
    /// <summary>The factions cooperate and support each other's goals.</summary>
    Allied,

    /// <summary>The factions have no strong positive or negative relationship.</summary>
    Neutral,

    /// <summary>The factions actively oppose each other.</summary>
    Hostile,

    /// <summary>The relationship is complex, inconsistent, or context-dependent.</summary>
    Complicated,
}
