using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RequiemNexus.Data.Models;

/// <summary>
/// Join table: CovenantDefinition to Merit. Merits gated by covenant membership.
/// </summary>
public class CovenantDefinitionMerit
{
    [Key]
    public int Id { get; set; }

    public int CovenantDefinitionId { get; set; }

    [ForeignKey(nameof(CovenantDefinitionId))]
    public virtual CovenantDefinition? CovenantDefinition { get; set; }

    public int MeritId { get; set; }

    [ForeignKey(nameof(MeritId))]
    public virtual Merit? Merit { get; set; }
}
