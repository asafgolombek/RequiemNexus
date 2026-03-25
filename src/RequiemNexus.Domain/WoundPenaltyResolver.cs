namespace RequiemNexus.Domain;

/// <summary>
/// Pure wound-penalty rules from the health track (matches vitals tooltips: last three boxes).
/// </summary>
public static class WoundPenaltyResolver
{
    /// <summary>
    /// Returns true when every health box in the normalized track is marked with damage.
    /// </summary>
    /// <param name="healthDamage">Raw <c>Character.HealthDamage</c> string.</param>
    /// <param name="maxHealth">Length of the track (typically <c>CalculatedMaxHealth</c>).</param>
    public static bool IsIncapacitated(string? healthDamage, int maxHealth)
    {
        if (maxHealth <= 0)
        {
            return false;
        }

        string track = HealthTrackMutator.NormalizeTrack(healthDamage, maxHealth);
        for (int i = 0; i < maxHealth; i++)
        {
            if (track[i] == ' ')
            {
                return false;
            }
        }

        return maxHealth > 0;
    }

    /// <summary>
    /// Dice penalty applied to Physical skill pools: 0, −1, −2, or −3 (always non-positive).
    /// </summary>
    /// <param name="healthDamage">Raw health track string.</param>
    /// <param name="maxHealth">Track length.</param>
    public static int GetWoundPenaltyDice(string? healthDamage, int maxHealth)
    {
        if (maxHealth <= 0)
        {
            return 0;
        }

        string track = HealthTrackMutator.NormalizeTrack(healthDamage, maxHealth);

        bool Filled(int index) => index >= 0 && index < track.Length && track[index] != ' ';

        if (Filled(maxHealth - 1))
        {
            return -3;
        }

        if (Filled(maxHealth - 2))
        {
            return -2;
        }

        if (Filled(maxHealth - 3))
        {
            return -1;
        }

        return 0;
    }
}
