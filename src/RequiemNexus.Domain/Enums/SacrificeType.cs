namespace RequiemNexus.Domain.Enums;

/// <summary>
/// Categories of ritual cost or sacrifice for blood sorcery activation (Phase 9.5).
/// Internal types are enforced against character resources; external types require UI acknowledgment.
/// </summary>
public enum SacrificeType
{
    /// <summary>Vitae spent from the character's blood pool (standard cost).</summary>
    InternalVitae,

    /// <summary>Vitae lost to the pool (e.g. spilled); same mechanical deduction as <see cref="InternalVitae"/>, distinct for logging.</summary>
    SpilledVitae,

    /// <summary>Willpower points spent from current Willpower.</summary>
    Willpower,

    /// <summary>Theban-style sacrament consumed (narrative; requires acknowledgment).</summary>
    PhysicalSacrament,

    /// <summary>Heart or similar visceral offering (narrative; requires acknowledgment).</summary>
    Heart,

    /// <summary>Material offering or resource (narrative; requires acknowledgment).</summary>
    MaterialOffering,

    /// <summary>Humanity stain inflicted or required (applied to <c>HumanityStains</c> when automated).</summary>
    HumanityStain,

    /// <summary>Item or focus required but not consumed (e.g. Necromancy focus); requires acknowledgment.</summary>
    MaterialFocus,
}
