using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RequiemNexus.Data.Models;

public class UserSession
{
    [Key]
    public required string Id { get; set; }

    [Required]
    public required string ApplicationUserId { get; set; }

    [ForeignKey(nameof(ApplicationUserId))]
    public ApplicationUser User { get; set; } = null!;

    [Required]
    public required byte[] Value { get; set; }

    public DateTimeOffset LastActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }

    [MaxLength(512)]
    public string? UserAgent { get; set; }

    [MaxLength(45)] // Max length of IPv6 string
    public string? IpAddress { get; set; }
}
