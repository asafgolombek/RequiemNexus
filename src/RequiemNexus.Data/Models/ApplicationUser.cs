using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace RequiemNexus.Data.Models;

public class ApplicationUser : IdentityUser
{
    // A player or storyteller can have multiple characters
    public virtual ICollection<Character> Characters { get; set; } = [];

    // A storyteller can run multiple campaigns
    [InverseProperty("StoryTeller")]
    public virtual ICollection<Campaign> StoryToldCampaigns { get; set; } = [];

    public DateOnly? Birthday { get; set; }

    public DateOnly? MemberSince { get; set; }

    // FIDO2 WebAuthn stored credentials for physical security keys
    public virtual ICollection<FidoStoredCredential> FidoStoredCredentials { get; set; } = [];

    // Audit logs for security events
    public virtual ICollection<AuditLog> AuditLogs { get; set; } = [];

    // Display name shown in the UI instead of email
    public string? DisplayName { get; set; }

    // Avatar image URL
    public string? AvatarUrl { get; set; }

    // Soft-delete: when set, the account is scheduled for permanent deletion after the grace period.
    public DateTimeOffset? DeletionScheduledAt { get; set; }

    // Notification preferences
    public bool NotifyOnSecurityEvents { get; set; } = true;

    public bool NotifyOnAccountChanges { get; set; } = true;

    public bool NotifyOnNewsletter { get; set; } = false;

    // Consent logs for Terms of Service / Privacy Policy acceptance
    public virtual ICollection<ConsentLog> ConsentLogs { get; set; } = [];
}
