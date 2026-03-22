using System.ComponentModel.DataAnnotations;

namespace RequiemNexus.Data.Models;

/// <summary>
/// Weapon statistics (TPT extension).
/// </summary>
public class WeaponAsset : Asset
{
    public int Damage { get; set; }

    public int? InitiativeModifier { get; set; }

    public int? StrengthRequirement { get; set; }

    [MaxLength(40)]
    public string? Ranges { get; set; }

    [MaxLength(32)]
    public string? ClipInfo { get; set; }

    public bool IsRangedWeapon { get; set; }

    public bool UsesBrawlForAttacks { get; set; }

    [MaxLength(500)]
    public string? WeaponSpecialNotes { get; set; }

    public bool HasAutofire { get; set; }

    public bool HasNineAgain { get; set; }

    public int ArmorPiercingRating { get; set; }

    public bool HasStun { get; set; }

    /// <summary>Optional weapon Size from the book.</summary>
    public int? ItemSize { get; set; }
}
