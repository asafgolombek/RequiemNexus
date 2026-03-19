using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RequiemNexus.Data.Models.Enums;

namespace RequiemNexus.Data.Models;

/// <summary>
/// A single prerequisite for a merit. OR groups use the same OrGroupId — satisfy any group.
/// ReferenceId maps to MeritId, AttributeId, SkillId, DisciplineId, ClanId, or CreatureType enum depending on Type.
/// </summary>
public class MeritPrerequisite
{
    [Key]
    public int Id { get; set; }

    public int MeritId { get; set; }

    [ForeignKey(nameof(MeritId))]
    public virtual Merit? Merit { get; set; }

    public MeritPrerequisiteType PrerequisiteType { get; set; }

    /// <summary>
    /// MeritId, AttributeId, SkillId, DisciplineId, ClanId, or CreatureType enum value depending on PrerequisiteType.
    /// </summary>
    public int? ReferenceId { get; set; }

    /// <summary>Minimum level required. 0 for MeritExclusion means "any dots" (excluded if present).</summary>
    public int MinimumRating { get; set; }

    /// <summary>Prerequisites with the same OrGroupId form an OR group — satisfy any.</summary>
    public int OrGroupId { get; set; }
}
