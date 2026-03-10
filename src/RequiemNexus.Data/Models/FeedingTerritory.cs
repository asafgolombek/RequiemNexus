using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RequiemNexus.Data.Models;

/// <summary>A feeding ground within a campaign's city, optionally controlled by a faction.</summary>
public class FeedingTerritory
{
    [Key]
    public int Id { get; set; }

    public int CampaignId { get; set; }

    [ForeignKey(nameof(CampaignId))]
    public virtual Campaign? Campaign { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>Quality/richness of the hunting ground (1–5).</summary>
    public int Rating { get; set; }

    public int? ControlledByFactionId { get; set; }

    [ForeignKey(nameof(ControlledByFactionId))]
    public virtual CityFaction? ControlledByFaction { get; set; }
}
