namespace RequiemNexus.Domain;

/// <summary>
/// Encapsulates the deterministic rules for Vampire: The Requiem character creation and stat derivation.
/// Pure, stateless — register as Singleton in DI.
/// </summary>
public class CharacterCreationRules : ICharacterCreationRules
{
    public (int MaxHealth, int CurrentHealth) CalculateInitialHealth(int size, int stamina)
    {
        var maxHealth = size + stamina;
        return (maxHealth, maxHealth); // Neonates start at full health
    }

    public (int MaxWillpower, int CurrentWillpower) CalculateInitialWillpower(int resolve, int composure)
    {
        var maxWillpower = resolve + composure;
        return (maxWillpower, maxWillpower); // Neonates start at full willpower
    }

    public (int BloodPotency, int MaxVitae, int CurrentVitae) CalculateInitialBloodPotencyAndVitae()
    {
        // A standard starting neonate has BP 1 and a Max Vitae pool of 10.
        return (BloodPotency: 1, MaxVitae: 10, CurrentVitae: 10);
    }

    /// <summary>
    /// Converts 5 Beats into 1 Experience Point (and increments Total XP earned).
    /// Returns true if a conversion occurred.
    /// </summary>
    public bool TryConvertBeats(int beats, out int newBeats, out int xpGained)
    {
        if (beats >= 5)
        {
            newBeats = beats - 5;
            xpGained = 1;
            return true;
        }

        newBeats = beats;
        xpGained = 0;
        return false;
    }
}
