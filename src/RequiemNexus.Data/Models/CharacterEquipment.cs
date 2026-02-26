using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RequiemNexus.Data.Models;

public class CharacterEquipment
{
    [Key]
    public int Id { get; set; }

    public int CharacterId { get; set; }

    [ForeignKey(nameof(CharacterId))]
    public virtual Character? Character { get; set; }

    public int EquipmentId { get; set; }

    [ForeignKey(nameof(EquipmentId))]
    public virtual Equipment? Equipment { get; set; }

    public int Quantity { get; set; } = 1;

    [MaxLength(200)]
    public string? Notes { get; set; }
}
