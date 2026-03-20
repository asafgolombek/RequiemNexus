using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Domain.Models;

/// <summary>
/// A single structured cost or sacrifice entry for a <c>SorceryRiteDefinition</c>, serialized in <c>RequirementsJson</c>.
/// </summary>
/// <param name="Type">Kind of sacrifice or resource cost.</param>
/// <param name="Value">Magnitude: Vitae/Willpower/Stain count, or dot/resource level for narrative types.</param>
/// <param name="IsConsumed">When false, the entry is informational for narrative foci (still may require acknowledgment).</param>
/// <param name="DisplayHint">Optional seed-only text for future UI (e.g. item name).</param>
public record RiteRequirement(
    SacrificeType Type,
    int Value = 1,
    bool IsConsumed = true,
    string? DisplayHint = null);
