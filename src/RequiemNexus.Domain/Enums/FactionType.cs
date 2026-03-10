namespace RequiemNexus.Domain.Enums;

/// <summary>The political role or classification of a vampire faction within a city's power structure.</summary>
public enum FactionType
{
    /// <summary>The Prince's court and direct loyalists.</summary>
    Prince,

    /// <summary>The council of clan representatives.</summary>
    Primogen,

    /// <summary>A covenant-aligned political bloc.</summary>
    Covenant,

    /// <summary>Vampires operating outside established power structures.</summary>
    Independent,

    /// <summary>Any other faction type not covered by the canonical categories.</summary>
    Other,
}
