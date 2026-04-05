namespace RequiemNexus.Data.RealTime;

/// <summary>
/// A delta representing real-time changes to a character's state.
/// Fields are optional to allow for targeted broadcasts.
/// </summary>
/// <param name="CharacterId">Primary key of the character being updated.</param>
/// <param name="CurrentHealth">Updated current undamaged Health boxes.</param>
/// <param name="MaxHealth">Updated total Health track length.</param>
/// <param name="CurrentWillpower">Updated current Willpower.</param>
/// <param name="MaxWillpower">Updated maximum Willpower.</param>
/// <param name="CurrentVitae">Updated current Vitae.</param>
/// <param name="MaxVitae">Updated maximum Vitae.</param>
/// <param name="Humanity">Updated Humanity rating.</param>
/// <param name="Armor">Updated total Armor value.</param>
/// <param name="ActiveConditions">Full list of current Condition names.</param>
/// <param name="HealthDamage">Updated health track string (bashing/lethal/aggravated symbols).</param>
/// <param name="Beats">Current Beat pool (toward XP conversion).</param>
/// <param name="ExperiencePoints">Spendable experience points.</param>
/// <param name="TotalExperiencePoints">Lifetime XP earned (including spent).</param>
public record CharacterUpdateDto(
    int CharacterId,
    int? CurrentHealth = null,
    int? MaxHealth = null,
    int? CurrentWillpower = null,
    int? MaxWillpower = null,
    int? CurrentVitae = null,
    int? MaxVitae = null,
    int? Humanity = null,
    int? Armor = null,
    IEnumerable<string>? ActiveConditions = null,
    string? HealthDamage = null,
    int? Beats = null,
    int? ExperiencePoints = null,
    int? TotalExperiencePoints = null);
