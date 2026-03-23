namespace RequiemNexus.Data.Models.Enums;

/// <summary>
/// Catalog asset subtype (TPT discriminator). Replaces legacy <c>EquipmentType</c>.
/// </summary>
public enum AssetKind
{
    Weapon,

    Armor,

    General,

    /// <summary>Professional services (book Services list).</summary>
    Service,
}
