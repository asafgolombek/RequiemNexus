using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RequiemNexus.Data.Models;

public class Character
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;

    [ForeignKey(nameof(ApplicationUserId))]
    public virtual ApplicationUser? User { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public int? ClanId { get; set; }

    [ForeignKey(nameof(ClanId))]
    public virtual Clan? Clan { get; set; }

    // Core specific stats for the Neonate Phase
    public int Health { get; set; }
    public int Willpower { get; set; }
    public int BloodPotency { get; set; }
    public int Vitae { get; set; }
}
