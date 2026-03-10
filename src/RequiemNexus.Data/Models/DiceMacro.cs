namespace RequiemNexus.Data.Models;

/// <summary>
/// A saved dice-pool preset owned by a character. Lets the player one-tap a common pool
/// (e.g. "Dexterity + Stealth") from the dice roller modal.
/// </summary>
public class DiceMacro
{
    /// <summary>Gets or sets the primary key.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the FK to the owning character.</summary>
    public int CharacterId { get; set; }

    /// <summary>Gets or sets the display label for this macro (e.g. "Dex + Stealth").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the number of dice in the pool.</summary>
    public int DicePool { get; set; }

    /// <summary>Gets or sets an optional free-text description of when to use this macro.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC creation timestamp.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the navigation property to the owning character.</summary>
    public virtual Character? Character { get; set; }
}
