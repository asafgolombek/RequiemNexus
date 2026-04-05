using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RequiemNexus.Data.Models;

public class Campaign
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string StoryTellerId { get; set; } = string.Empty;

    [ForeignKey(nameof(StoryTellerId))]
    public virtual ApplicationUser? StoryTeller { get; set; }

    public string Description { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Phase 10.5: Investigation successes required to grant one <see cref="ManeuverClue"/> when banking successes on a Social maneuver (ST-tunable; default 3).
    /// </summary>
    public int SocialManeuverInvestigationSuccessesPerClue { get; set; } = 3;

    /// <summary>
    /// SHA-256 hash (64 hex chars) of the player join invite token. Null when invite links are disabled.
    /// Plaintext token is never stored.
    /// </summary>
    [MaxLength(64)]
    public string? InviteTokenHash { get; set; }

    /// <summary>
    /// Phase 20: Optional Discord incoming webhook URL (HTTPS, discord.com/api/webhooks/...) for session presence posts. ST-only; never log the full value.
    /// </summary>
    [MaxLength(512)]
    public string? DiscordWebhookUrl { get; set; }

    public virtual ICollection<Character> Characters { get; set; } = new List<Character>();

    public virtual ICollection<CityFaction> Factions { get; set; } = [];

    public virtual ICollection<ChronicleNpc> Npcs { get; set; } = [];

    public virtual ICollection<FeedingTerritory> Territories { get; set; } = [];

    public virtual ICollection<FactionRelationship> FactionRelationships { get; set; } = [];

    public virtual ICollection<SocialManeuver> SocialManeuvers { get; set; } = [];

    public virtual ICollection<BloodBond> BloodBonds { get; set; } = [];

    public virtual ICollection<PredatoryAuraContest> PredatoryAuraContests { get; set; } = [];

    public virtual ICollection<Ghoul> Ghouls { get; set; } = [];
}
