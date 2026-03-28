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

    /// <summary>Gets or sets a value indicating whether this Discipline can be learned without a teacher.</summary>
    public bool CanLearnIndependently { get; set; }

    /// <summary>Gets or sets a value indicating whether learning this Discipline out-of-clan requires drinking the mentor's Vitae.</summary>
    public bool RequiresMentorBloodToLearn { get; set; }

    /// <summary>Gets or sets a value indicating whether this Discipline is restricted to a specific Covenant.</summary>
    public bool IsCovenantDiscipline { get; set; }

    /// <summary>Gets or sets the Covenant that gates access to this Discipline. Null unless IsCovenantDiscipline is true.</summary>
    public int? CovenantId { get; set; }

    /// <summary>Gets or sets the CovenantDefinition navigation property.</summary>
    public virtual CovenantDefinition? Covenant { get; set; }

    /// <summary>Gets or sets a value indicating whether this Discipline is restricted to a specific Bloodline.</summary>
    public bool IsBloodlineDiscipline { get; set; }

    /// <summary>Gets or sets the Bloodline that gates access to this Discipline. Null unless IsBloodlineDiscipline is true.</summary>
    public int? BloodlineId { get; set; }

    /// <summary>Gets or sets the BloodlineDefinition navigation property.</summary>
    public virtual BloodlineDefinition? Bloodline { get; set; }

    /// <summary>Gets or sets a value indicating whether this Discipline is Necromancy (requires Mekhet-clan, Necromancy-linked bloodline, or ST acknowledgment).</summary>
    public bool IsNecromancy { get; set; }

    public virtual ICollection<DisciplinePower> Powers { get; set; } = new List<DisciplinePower>();
}
