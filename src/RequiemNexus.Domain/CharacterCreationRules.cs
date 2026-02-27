namespace RequiemNexus.Domain;

/// <summary>
/// Encapsulates the deterministic rules for Vampire: The Requiem character creation and stat derivation.
/// This fulfills the Antigravity requirement of keeping the Domain layer pure and stateless.
/// </summary>
public static class CharacterCreationRules
{
    public static (int MaxHealth, int CurrentHealth) CalculateInitialHealth(int size, int stamina)
    {
        var maxHealth = size + stamina;
        return (maxHealth, maxHealth); // Neonates start at full health
    }

    public static (int MaxWillpower, int CurrentWillpower) CalculateInitialWillpower(int resolve, int composure)
    {
        var maxWillpower = resolve + composure;
        return (maxWillpower, maxWillpower); // Neonates start at full willpower
    }

    public static (int BloodPotency, int MaxVitae, int CurrentVitae) CalculateInitialBloodPotencyAndVitae()
    {
        // A standard starting neonate has BP 1 and a Max Vitae pool of 10.
        return (BloodPotency: 1, MaxVitae: 10, CurrentVitae: 10);
    }
}
