namespace RequiemNexus.Application.DTOs;

/// <summary>
/// Result of starting a blood rite after costs are applied once. Extended casting may use up to
/// <see cref="MaxExtendedRolls"/> attempts (unmodified ritual pool per V:tR 2e p. 152).
/// </summary>
/// <param name="DicePool">Dice to roll each attempt (includes optional Crúac extra Vitae and Blood Sympathy).</param>
/// <param name="MaxExtendedRolls">Maximum number of rolls allowed (trait pool only, before those bonuses).</param>
/// <param name="TargetSuccesses">Total successes required to complete the rite.</param>
/// <param name="MinutesPerRoll">Base interval per roll (30 or 15 minutes per PDF).</param>
/// <param name="RitualDisciplineDots">Dots in the matching ritual Discipline (Crúac, Theban Sorcery, or Necromancy) for optional Potency on exceptional success.</param>
/// <param name="NecromancyDegenerationCheckRaised">
/// True when a <c>DegenerationCheckRequiredEvent</c> was raised for the Necromancy breaking point (Humanity 7+). The Storyteller Glimpse receives a chronicle patch when the character belongs to a campaign.
/// </param>
public record BeginRiteActivationResult(
    int DicePool,
    int MaxExtendedRolls,
    int TargetSuccesses,
    int MinutesPerRoll,
    int RitualDisciplineDots,
    bool NecromancyDegenerationCheckRaised);
