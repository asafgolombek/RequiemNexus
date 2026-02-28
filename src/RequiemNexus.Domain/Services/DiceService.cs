using System.Collections.Generic;

namespace RequiemNexus.Domain.Services;

public class RollResult
{
    public int Successes { get; set; }
    public bool IsExceptionalSuccess => Successes >= 5;
    public bool IsDramaticFailure { get; set; }
    public List<int> DiceRolled { get; set; } = new List<int>();
}

public class DiceService
{
    public RollResult Roll(int dicePool, bool tenAgain = true, bool nineAgain = false, bool eightAgain = false, bool isRote = false, int? seed = null)
    {
        var random = seed.HasValue ? new Random(seed.Value) : Random.Shared;
        var result = new RollResult();

        if (dicePool <= 0)
        {
            // Chance die
            int roll = random.Next(1, 11);
            result.DiceRolled.Add(roll);

            if (roll == 10)
                result.Successes = 1;
            else if (roll == 1)
                result.IsDramaticFailure = true;

            return result;
        }

        int successes = 0;
        int diceToRoll = dicePool;

        while (diceToRoll > 0)
        {
            int additionalDice = 0;

            for (int i = 0; i < diceToRoll; i++)
            {
                int roll = random.Next(1, 11);
                result.DiceRolled.Add(roll);

                bool isSuccess = roll >= 8;
                if (isSuccess)
                {
                    successes++;
                }
                else if (isRote)
                {
                    // Rote action: reroll failed dice once
                    int reroll = random.Next(1, 11);
                    result.DiceRolled.Add(reroll);
                    if (reroll >= 8) successes++;
                }

                // Exploding dice rules
                if (tenAgain && roll == 10) additionalDice++;
                else if (nineAgain && roll >= 9) additionalDice++;
                else if (eightAgain && roll >= 8) additionalDice++;
            }

            diceToRoll = additionalDice;
            // Rote only applies to the initial pool, not exploding dice
            isRote = false;
        }

        result.Successes = successes;
        return result;
    }
}
