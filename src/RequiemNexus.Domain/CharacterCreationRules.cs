using RequiemNexus.Domain.Contracts;

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

    /// <summary>
    /// Calculates initial Blood Potency and Max Vitae based on the VtR 2e table.
    /// BP 1=10, 2=11, 3=12, 4=13, 5=15, 6=20, 7=25, 8=30, 9=50, 10=75.
    /// </summary>
    public (int BloodPotency, int MaxVitae, int CurrentVitae) CalculateInitialBloodPotencyAndVitae(int bloodPotency = 1)
    {
        int maxVitae = bloodPotency switch
        {
            1 => 10,
            2 => 11,
            3 => 12,
            4 => 13,
            5 => 15,
            6 => 20,
            7 => 25,
            8 => 30,
            9 => 50,
            10 => 75,
            _ => 10, // Default to 10 for safety/neonate
        };

        return (BloodPotency: bloodPotency, MaxVitae: maxVitae, CurrentVitae: maxVitae);
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
