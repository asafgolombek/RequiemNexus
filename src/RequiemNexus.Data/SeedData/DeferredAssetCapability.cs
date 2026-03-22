using RequiemNexus.Data.Models.Enums;

namespace RequiemNexus.Data.SeedData;

/// <summary>
/// Capability seed row before FK ids exist.
/// </summary>
public sealed record DeferredAssetCapability(
    string OwnerAssetSlug,
    AssetCapabilityKind Kind,
    string? AssistsSkillName = null,
    int? DiceBonusMin = null,
    int? DiceBonusMax = null,
    string? WeaponProfileSlug = null);
