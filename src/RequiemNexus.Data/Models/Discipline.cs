using System.ComponentModel.DataAnnotations;

namespace RequiemNexus.Data.Models;

public class Discipline
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether this is a user-created homebrew Discipline.</summary>
    public bool IsHomebrew { get; set; }

    /// <summary>Gets or sets the user ID of the homebrew author, or null for official Disciplines.</summary>
    public string? HombrewAuthorUserId { get; set; }

    public virtual ICollection<DisciplinePower> Powers { get; set; } = new List<DisciplinePower>();
}
