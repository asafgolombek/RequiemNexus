using RequiemNexus.Domain.Models;

namespace RequiemNexus.Domain.Contracts;

public interface IDiceService
{
    RollResult Roll(int dicePool, bool tenAgain = true, bool nineAgain = false, bool eightAgain = false, bool isRote = false, int? seed = null);
}
