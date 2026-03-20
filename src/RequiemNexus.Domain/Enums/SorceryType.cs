namespace RequiemNexus.Domain.Enums;

/// <summary>
/// Type of blood sorcery. Crúac belongs to the Circle of the Crone; Theban Sorcery to the Lancea et Sanctum.
/// </summary>
public enum SorceryType
{
    /// <summary>Crúac — blood rites of the Circle of the Crone.</summary>
    Cruac,

    /// <summary>Theban Sorcery — sacraments of the Lancea et Sanctum.</summary>
    Theban,

    /// <summary>Necromancy — Mekhet-associated death sorcery (Phase 9.6).</summary>
    Necromancy,

    /// <summary>Ordo Dracul covenant rituals (Phase 9.6).</summary>
    OrdoDraculRitual,
}
