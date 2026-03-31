using System.Collections.Generic;

namespace RequiemNexus.Domain.Models;

public class RollResult
{
    public int Successes { get; set; }

    public bool IsExceptionalSuccess => Successes >= 5;

    public bool IsDramaticFailure { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this roll used a single chance die (pool 0 or less)
    /// instead of a normal dice pool. UI and consumers use this to apply chance-die success rules (10 only).
    /// </summary>
    public bool IsChanceDie { get; set; }

    public List<int> DiceRolled { get; set; } = new List<int>();
}
