using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RequiemNexus.Domain.Enums;

namespace RequiemNexus.Data.Models;

/// <summary>Records the political stance between two factions within a campaign.</summary>
public class FactionRelationship
{
    [Key]
    public int Id { get; set; }

    public int CampaignId { get; set; }

    [ForeignKey(nameof(CampaignId))]
    public virtual Campaign? Campaign { get; set; }

    public int FactionAId { get; set; }

    [ForeignKey(nameof(FactionAId))]
    public virtual CityFaction? FactionA { get; set; }

    public int FactionBId { get; set; }

    [ForeignKey(nameof(FactionBId))]
    public virtual CityFaction? FactionB { get; set; }

    /// <summary>The stance that Faction A holds toward Faction B.</summary>
    public FactionStance StanceFromA { get; set; }

    public string Notes { get; set; } = string.Empty;
}
