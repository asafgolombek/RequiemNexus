namespace RequiemNexus.Data.RealTime;

/// <summary>Discriminator for <see cref="RelationshipUpdateDto"/> to avoid stringly-typed hub events.</summary>
public enum RelationshipUpdateType
{
    /// <summary>A Blood Bond stage changed for a character in this session.</summary>
    BloodBond,

    /// <summary>A Predatory Aura contest was resolved involving a character in this session.</summary>
    PredatoryAura,

    /// <summary>A character's sire or childer linkage changed.</summary>
    Lineage,

    /// <summary>A ghoul retainer bound to a PC regnant was created, fed, released, or had Discipline access changed.</summary>
    Ghoul,
}
