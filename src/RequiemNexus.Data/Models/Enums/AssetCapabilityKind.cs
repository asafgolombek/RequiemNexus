namespace RequiemNexus.Data.Models.Enums;

/// <summary>
/// Extra mechanical hooks for a single narrative asset (e.g. crowbar as tool + improvised weapon).
/// </summary>
public enum AssetCapabilityKind
{
    /// <summary>Grants equipment bonus dice to a named book skill when the capability applies.</summary>
    SkillAssist,

    /// <summary>Uses combat stats from another catalog asset (weapon profile row).</summary>
    WeaponProfileRef,
}
