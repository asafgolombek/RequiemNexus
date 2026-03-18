using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RequiemNexus.Data.Models;

/// <summary>
/// Seed data defining a covenant. Content is data; behavior is in CovenantService.
/// </summary>
public class CovenantDefinition
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>When false, characters cannot join (e.g., VII for antagonist use only).</summary>
    public bool IsPlayable { get; set; } = true;

    /// <summary>When true, this covenant grants access to Blood Sorcery (Crúac or Theban Sorcery).</summary>
    public bool SupportsBloodSorcery { get; set; }

    /// <summary>Merits gated by this covenant (e.g., Covenant Status).</summary>
    public virtual ICollection<CovenantDefinitionMerit> CovenantSpecificMerits { get; set; } = new List<CovenantDefinitionMerit>();
}
