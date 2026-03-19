namespace RequiemNexus.Data.Models.Enums;

/// <summary>
/// The supernatural or mortal nature of a character or NPC.
/// Player-created characters default to Vampire. ST-created NPCs may be any type.
/// </summary>
public enum CreatureType
{
    /// <summary>Kindred—an undead vampire.</summary>
    Vampire = 0,

    /// <summary>A mortal bound to a vampire's blood.</summary>
    Ghoul = 1,

    /// <summary>An unbound mortal.</summary>
    Mortal = 2,
}
