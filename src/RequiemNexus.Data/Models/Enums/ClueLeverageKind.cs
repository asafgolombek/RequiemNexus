namespace RequiemNexus.Data.Models.Enums;

/// <summary>
/// Nexus extension: how a discovered clue is intended to apply (ST-facing; not core VtR Social Maneuvering text).
/// </summary>
public enum ClueLeverageKind
{
    /// <summary>Impression or narrative soft pressure (ST discretion).</summary>
    Soft = 0,

    /// <summary>Hard leverage (threats, blackmail) — only valid when forcing Doors per book.</summary>
    Hard = 1,
}
