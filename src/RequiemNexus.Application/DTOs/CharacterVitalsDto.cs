namespace RequiemNexus.Application.DTOs;

/// <summary>
/// Snapshot of a character's vital stats for the Storyteller Glimpse dashboard.
/// Contains only the fields needed for the overview cards; no PII beyond the character name.
/// </summary>
public record CharacterVitalsDto
{
    /// <summary>Primary key of the <c>Character</c> row.</summary>
    public required int CharacterId { get; init; }

    /// <summary>Character's in-game name.</summary>
    public required string Name { get; init; }

    /// <summary>AspNetUsers Id of the player who owns this character.</summary>
    public required string PlayerUserId { get; init; }

    /// <summary>Current undamaged Health boxes.</summary>
    public required int CurrentHealth { get; init; }

    /// <summary>Total Health track length.</summary>
    public required int MaxHealth { get; init; }

    /// <summary>Current Willpower.</summary>
    public required int CurrentWillpower { get; init; }

    /// <summary>Maximum Willpower.</summary>
    public required int MaxWillpower { get; init; }

    /// <summary>Current Vitae in the blood pool.</summary>
    public required int CurrentVitae { get; init; }

    /// <summary>Maximum blood pool capacity.</summary>
    public required int MaxVitae { get; init; }

    /// <summary>Current Humanity rating.</summary>
    public required int Humanity { get; init; }

    /// <summary>Current Beat count.</summary>
    public required int Beats { get; init; }

    /// <summary>Current unspent XP.</summary>
    public required int ExperiencePoints { get; init; }

    /// <summary>Number of unresolved Conditions.</summary>
    public required int ActiveConditionCount { get; init; }
}
