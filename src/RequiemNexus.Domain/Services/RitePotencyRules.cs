namespace RequiemNexus.Domain.Services;

/// <summary>
/// Pure rules for ritual Potency after extended casting (V:tR 2e p. 152 sidebar).
/// Exceptional-success addition of ritual Discipline dots is a player choice handled in the UI.
/// </summary>
public static class RitePotencyRules
{
    /// <summary>
    /// Computes base potency when accumulated successes meet or exceed the rite's target:
    /// 1 plus each success beyond the target. Returns 0 if the target is not yet reached.
    /// </summary>
    /// <param name="accumulatedSuccesses">Total successes across all extended rolls so far.</param>
    /// <param name="targetSuccesses">The rite's required success total.</param>
    /// <returns>0 while below target; otherwise 1 + (accumulated − target).</returns>
    public static int ComputeBasePotency(int accumulatedSuccesses, int targetSuccesses)
    {
        if (targetSuccesses <= 0)
        {
            return 0;
        }

        if (accumulatedSuccesses < targetSuccesses)
        {
            return 0;
        }

        return 1 + (accumulatedSuccesses - targetSuccesses);
    }
}
