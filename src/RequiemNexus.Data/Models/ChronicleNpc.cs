using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RequiemNexus.Data.Models;

/// <summary>A named NPC within a campaign's chronicle.</summary>
public class ChronicleNpc
{
    [Key]
    public int Id { get; set; }

    public int CampaignId { get; set; }

    [ForeignKey(nameof(CampaignId))]
    public virtual Campaign? Campaign { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Title { get; set; }

    /// <summary>The faction this NPC primarily belongs to.</summary>
    public int? PrimaryFactionId { get; set; }

    [ForeignKey(nameof(PrimaryFactionId))]
    public virtual CityFaction? PrimaryFaction { get; set; }

    [MaxLength(200)]
    public string? RoleInFaction { get; set; }

    public string PublicDescription { get; set; } = string.Empty;

    /// <summary>ST-only internal notes about this NPC.</summary>
    public string StorytellerNotes { get; set; } = string.Empty;

    public bool IsAlive { get; set; } = true;

    /// <summary>Optional link to an <see cref="NpcStatBlock"/> for encounter use.</summary>
    public int? LinkedStatBlockId { get; set; }

    /// <summary>Whether this NPC is a vampire (<c>true</c>) or a mortal (<c>false</c>). Kept for backward compatibility; prefer <see cref="CreatureType"/>.</summary>
    public bool IsVampire { get; set; } = false;

    /// <summary>The creature type. ST can set any type when creating or editing NPCs.</summary>
    public Data.Models.Enums.CreatureType CreatureType { get; set; } = Data.Models.Enums.CreatureType.Mortal;

    /// <summary>JSON object mapping attribute names to dot values (1–5). Defaults all to 2.</summary>
    public string AttributesJson { get; set; } = "{}";

    /// <summary>JSON object mapping skill names to dot values (0–5). Defaults all to 2.</summary>
    public string SkillsJson { get; set; } = "{}";

    public virtual ICollection<SocialManeuver> SocialManeuversTargeted { get; set; } = new List<SocialManeuver>();
}
