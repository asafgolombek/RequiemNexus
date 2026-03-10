using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Data.Models;

/// <summary>Represents a political faction within a campaign's city power structure.</summary>
public class CityFaction
{
    [Key]
    public int Id { get; set; }

    public int CampaignId { get; set; }

    [ForeignKey(nameof(CampaignId))]
    public virtual Campaign? Campaign { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public FactionType FactionType { get; set; }

    /// <summary>Relative political power of this faction (1–5).</summary>
    public int InfluenceRating { get; set; }

    public string PublicDescription { get; set; } = string.Empty;

    /// <summary>ST-only internal notes about this faction's hidden agenda or secrets.</summary>
    public string StorytellerNotes { get; set; } = string.Empty;

    public string Agenda { get; set; } = string.Empty;

    /// <summary>Optional FK to the NPC who leads this faction.</summary>
    public int? LeaderNpcId { get; set; }

    [ForeignKey(nameof(LeaderNpcId))]
    public virtual ChronicleNpc? LeaderNpc { get; set; }

    public virtual ICollection<ChronicleNpc> Members { get; set; } = [];

    public virtual ICollection<FeedingTerritory> ControlledTerritories { get; set; } = [];
}
