using System.ComponentModel.DataAnnotations;

namespace RequiemNexus.Data.Models;

public class Clan
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(250)]
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether this is a user-created homebrew Clan/Bloodline.</summary>
    public bool IsHomebrew { get; set; }

    /// <summary>Gets or sets the user ID of the homebrew author, or null for official Clans.</summary>
    public string? HombrewAuthorUserId { get; set; }

    public virtual ICollection<ClanDiscipline> ClanDisciplines { get; set; } = new List<ClanDiscipline>();
}
