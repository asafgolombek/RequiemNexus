using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RequiemNexus.Data.Models;

public class CharacterDiscipline
{
    [Key]
    public int Id { get; set; }

    public int CharacterId { get; set; }

    [ForeignKey(nameof(CharacterId))]
    public virtual Character? Character { get; set; }

    public int DisciplineId { get; set; }

    [ForeignKey(nameof(DisciplineId))]
    public virtual Discipline? Discipline { get; set; }

    public int Rating { get; set; }
}
