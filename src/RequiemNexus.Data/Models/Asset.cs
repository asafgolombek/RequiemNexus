using System.ComponentModel.DataAnnotations;
using RequiemNexus.Data.Models.Enums;

namespace RequiemNexus.Data.Models;

/// <summary>
/// Root catalog row for gear, weapons, armor, and services (Phase 11 TPT).
/// </summary>
public class Asset
{
    [Key]
    public int Id { get; set; }

    /// <summary>Stable idempotency key for JSON seeds (e.g. <c>vtm2e:general:lockpicking-kit</c>).</summary>
    [MaxLength(160)]
    public string? Slug { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public AssetKind Kind { get; set; } = AssetKind.General;

    [MaxLength(4000)]
    public string? Description { get; set; }

    /// <summary>Legacy weight field; retained for future encumbrance rules.</summary>
    public float Weight { get; set; }

    /// <summary>Resources dot threshold (Availability •) for procurement.</summary>
    public int Cost { get; set; }

    /// <summary>Same semantic as book Availability; seeded from JSON.</summary>
    public int Availability { get; set; }

    /// <summary>When true, procurement requires Storyteller approval.</summary>
    public bool IsIllicit { get; set; }

    /// <summary>
    /// When false, the row exists for weapon profiles / capability targets only and is omitted from player catalog pickers.
    /// </summary>
    public bool IsListedInCatalog { get; set; } = true;

    public virtual ICollection<CharacterAsset> CharacterAssets { get; set; } = new List<CharacterAsset>();

    public virtual ICollection<AssetCapability> Capabilities { get; set; } = new List<AssetCapability>();
}
