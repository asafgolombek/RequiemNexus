namespace RequiemNexus.Domain.Enums;

/// <summary>
/// Tags the origin of damage for combat and future tilt hooks (e.g. Rötschreck in Phase 15).
/// </summary>
public enum DamageSource
{
    /// <summary>Blunt or non-lethal trauma.</summary>
    Bashing,

    /// <summary>Cutting, piercing, or lethal trauma.</summary>
    Lethal,

    /// <summary>Supernatural or extreme trauma that resists normal healing.</summary>
    Aggravated,

    /// <summary>Fire exposure (feeds frenzy / tilt logic later).</summary>
    Fire,

    /// <summary>Sunlight exposure (feeds frenzy / tilt logic later).</summary>
    Sunlight,

    /// <summary>Mundane weapon profile damage without a finer classification; treated as lethal on the health track.</summary>
    Weapon,
}
