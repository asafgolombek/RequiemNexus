using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RequiemNexus.Data.Models;

public class CharacterMerit
{
    [Key]
    public int Id { get; set; }

    public int CharacterId { get; set; }

    [ForeignKey(nameof(CharacterId))]
    public virtual Character? Character { get; set; }

    public int MeritId { get; set; }

    [ForeignKey(nameof(MeritId))]
    public virtual Merit? Merit { get; set; }

    public int Rating { get; set; }

    [MaxLength(100)]
    public string? Specification { get; set; }
}
