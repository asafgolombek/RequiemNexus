using System.Collections.Generic;

namespace RequiemNexus.Domain.Models;

public class RollResult
{
    public int Successes { get; set; }

    public bool IsExceptionalSuccess => Successes >= 5;

    public bool IsDramaticFailure { get; set; }

    public List<int> DiceRolled { get; set; } = new List<int>();
}
