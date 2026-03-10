using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RequiemNexus.Data.Models;

public enum AuditEventType
{
    Login = 1,
    PasswordChanged = 2,
    TwoFactorEnabled = 3,
    TwoFactorDisabled = 4,
    AccountDeletionRequested = 5,
    AccountDeletionCancelled = 6,
    PersonalDataDownloaded = 7,
    SecurityKeyAdded = 8,
    SecurityKeyRemoved = 9,
    DisplayNameChanged = 10,
    EmailChangeRequested = 11,
    EmailChanged = 12,
}

public class AuditLog
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey(nameof(UserId))]
    public virtual ApplicationUser? User { get; set; }

    public AuditEventType EventType { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    [MaxLength(45)]
    public string? IpAddress { get; set; }

    [MaxLength(500)]
    public string? Details { get; set; }
}
