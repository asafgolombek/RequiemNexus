using System.Collections.Generic;

namespace RequiemNexus.Domain.Services;

public class RollResult
{
    public int Successes { get; set; }
    public bool IsExceptionalSuccess => Successes >= 5;
    public bool IsDramaticFailure { get; set; }
    public List<int> DiceRolled { get; set; } = new List<int>();
}

public static class DiceService
{
    public static RollResult Roll(int dicePool, bool tenAgain = true, bool nineAgain = false, bool eightAgain = false, bool isRote = false, int? seed = null)
    {
#pragma warning disable S2245 // Using pseudorandom number generators (PRNGs) is security-sensitive
        var random = seed.HasValue ? new Random(seed.Value) : Random.Shared;
#pragma warning restore S2245 // Using pseudorandom number generators (PRNGs) is security-sensitive

        if (dicePool <= 0)
        {
            return RollChanceDie(random);
        }

        return RollRegularPool(random, dicePool, tenAgain, nineAgain, eightAgain, isRote);
    }

    private static RollResult RollChanceDie(Random random)
    {
        var result = new RollResult();
        int roll = random.Next(1, 11);
        result.DiceRolled.Add(roll);

        if (roll == 10)
            result.Successes = 1;
        else if (roll == 1)
            result.IsDramaticFailure = true;

        return result;
    }

    private static RollResult RollRegularPool(Random random, int dicePool, bool tenAgain, bool nineAgain, bool eightAgain, bool isRote)
    {
        var result = new RollResult();
        int successes = 0;
        int diceToRoll = dicePool;

        while (diceToRoll > 0)
        {
            var batchResult = ProcessDiceRollBatch(random, diceToRoll, tenAgain, nineAgain, eightAgain, isRote, result);
            diceToRoll = batchResult.AdditionalDice;
            successes += batchResult.NewSuccesses;
            // Rote only applies to the initial pool, not exploding dice
            isRote = false;
        }

        result.Successes = successes;
        return result;
    }

    private static (int AdditionalDice, int NewSuccesses) ProcessDiceRollBatch(Random random, int diceToRoll, bool tenAgain, bool nineAgain, bool eightAgain, bool isRote, RollResult result)
    {
        int additionalDice = 0;
        int newSuccesses = 0;

        for (int i = 0; i < diceToRoll; i++)
        {
            int roll = random.Next(1, 11);
            result.DiceRolled.Add(roll);

            if (roll >= 8)
            {
                newSuccesses++;
            }
            else if (isRote)
            {
                // Rote action: reroll failed dice once
                int reroll = random.Next(1, 11);
                result.DiceRolled.Add(reroll);
                if (reroll >= 8) newSuccesses++;
            }

            // Exploding dice rules
            if (tenAgain && roll == 10) additionalDice++;
            else if (nineAgain && roll >= 9) additionalDice++;
            else if (eightAgain && roll >= 8) additionalDice++;
        }

        return (additionalDice, newSuccesses);
    }
}
