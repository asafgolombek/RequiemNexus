using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RequiemNexus.Data.Models;

public class ConsentLog
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey(nameof(UserId))]
    public virtual ApplicationUser? User { get; set; }

    public DateTimeOffset ConsentedAt { get; set; }

    [MaxLength(20)]
    public string DocumentVersion { get; set; } = string.Empty;

    [MaxLength(45)]
    public string? IpAddress { get; set; }

    [MaxLength(50)]
    public string ConsentType { get; set; } = "TermsAndPrivacy";
}
