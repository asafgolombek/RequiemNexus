namespace RequiemNexus.Domain.Contracts;

/// <summary>
/// Interface for character creation rule calculations. Enables DI and testability.
/// </summary>
public interface ICharacterCreationRules
{
    (int MaxHealth, int CurrentHealth) CalculateInitialHealth(int size, int stamina);

    (int MaxWillpower, int CurrentWillpower) CalculateInitialWillpower(int resolve, int composure);

    (int BloodPotency, int MaxVitae, int CurrentVitae) CalculateInitialBloodPotencyAndVitae(int bloodPotency = 1);

    bool TryConvertBeats(int beats, out int newBeats, out int xpGained);
}
