using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RequiemNexus.Data.Models;

public class CharacterAspiration
{
    [Key]
    public int Id { get; set; }

    public int CharacterId { get; set; }

    [ForeignKey(nameof(CharacterId))]
    public virtual Character? Character { get; set; }

    [Required]
    [MaxLength(255)]
    public string Description { get; set; } = string.Empty;

    public bool IsLongTerm { get; set; } = false;
}
