namespace RequiemNexus.Data.Models;

/// <summary>
/// A private Storyteller session-prep note for a campaign. Visible only to the campaign Storyteller.
/// </summary>
public class SessionPrepNote
{
    /// <summary>Gets or sets the primary key.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the FK to the owning campaign.</summary>
    public int CampaignId { get; set; }

    /// <summary>Gets or sets the note title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the note body.</summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC creation timestamp.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the UTC last-update timestamp.</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the navigation property to the owning campaign.</summary>
    public virtual Campaign? Campaign { get; set; }
}
