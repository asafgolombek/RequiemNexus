namespace RequiemNexus.Application.DTOs;

/// <summary>
/// Snapshot of a character's vital stats for the Storyteller Glimpse dashboard.
/// Contains only the fields needed for the overview cards; no PII beyond the character name.
/// </summary>
/// <param name="CharacterId">Primary key of the <c>Character</c> row.</param>
/// <param name="Name">Character's in-game name.</param>
/// <param name="PlayerUserId">AspNetUsers Id of the player who owns this character.</param>
/// <param name="CurrentHealth">Current undamaged Health boxes.</param>
/// <param name="MaxHealth">Total Health track length.</param>
/// <param name="CurrentWillpower">Current Willpower.</param>
/// <param name="MaxWillpower">Maximum Willpower.</param>
/// <param name="CurrentVitae">Current Vitae in the blood pool.</param>
/// <param name="MaxVitae">Maximum blood pool capacity.</param>
/// <param name="Humanity">Current Humanity rating.</param>
/// <param name="Beats">Current Beat count.</param>
/// <param name="ExperiencePoints">Current unspent XP.</param>
/// <param name="ActiveConditionCount">Number of unresolved Conditions.</param>
public record CharacterVitalsDto(
    int CharacterId,
    string Name,
    string PlayerUserId,
    int CurrentHealth,
    int MaxHealth,
    int CurrentWillpower,
    int MaxWillpower,
    int CurrentVitae,
    int MaxVitae,
    int Humanity,
    int Beats,
    int ExperiencePoints,
    int ActiveConditionCount);
