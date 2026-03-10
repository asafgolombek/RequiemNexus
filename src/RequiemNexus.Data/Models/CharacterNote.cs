namespace RequiemNexus.Data.Models;

/// <summary>
/// A note attached to a character, optionally scoped to a campaign.
/// Notes flagged <see cref="IsStorytellerPrivate"/> are only visible to the Storyteller of
/// the associated campaign; all other notes are visible to the character's owner.
/// </summary>
public class CharacterNote
{
    /// <summary>Gets or sets the primary key.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the FK to the character this note belongs to.</summary>
    public int CharacterId { get; set; }

    /// <summary>Gets or sets the optional FK to the campaign this note is scoped to.
    /// Null for private player notes not tied to a campaign.</summary>
    public int? CampaignId { get; set; }

    /// <summary>Gets or sets the user ID of the author (player or Storyteller).</summary>
    public string AuthorUserId { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether this note is visible only to the Storyteller.
    /// When true, the note is hidden from the character owner in campaign view.</summary>
    public bool IsStorytellerPrivate { get; set; }

    /// <summary>Gets or sets the note title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the note body (plain text or Markdown).</summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC creation timestamp.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the UTC last-update timestamp.</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the navigation property to the owning character.</summary>
    public virtual Character? Character { get; set; }
}
