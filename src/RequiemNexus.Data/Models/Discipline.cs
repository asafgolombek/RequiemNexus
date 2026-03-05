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

    public virtual ICollection<DisciplinePower> Powers { get; set; } = new List<DisciplinePower>();
}
