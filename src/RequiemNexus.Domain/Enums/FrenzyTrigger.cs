namespace RequiemNexus.Domain.Enums;

/// <summary>
/// What provoked a frenzy resistance roll (Vampire: The Requiem 2e).
/// </summary>
public enum FrenzyTrigger
{
    /// <summary>Vitae reaches 0 during active play. Fired via VitaeDepletedEvent (automatic) or ST manual trigger (edge case).</summary>
    Hunger,

    /// <summary>Provocation during combat. Storyteller-triggered.</summary>
    Rage,

    /// <summary>Exposure to fire or sunlight. Player- or Storyteller-triggered.</summary>
    Rotschreck,

    /// <summary>Torpor hunger escalation. BackgroundService / Advance Time path only. Never a manual UI trigger.</summary>
    Starvation,
}
