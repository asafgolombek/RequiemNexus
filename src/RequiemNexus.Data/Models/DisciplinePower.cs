using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RequiemNexus.Data.Models;

public class DisciplinePower
{
    [Key]
    public int Id { get; set; }

    public int DisciplineId { get; set; }

    [ForeignKey(nameof(DisciplineId))]
    public virtual Discipline? Discipline { get; set; }

    public int Level { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [MaxLength(100)]
    public string DicePool { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Cost { get; set; } = string.Empty;
}
