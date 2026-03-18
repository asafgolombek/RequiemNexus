using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RequiemNexus.Data.Models;

/// <summary>
/// Tracks devotions a character has learned.
/// </summary>
public class CharacterDevotion
{
    [Key]
    public int Id { get; set; }

    public int CharacterId { get; set; }

    [ForeignKey(nameof(CharacterId))]
    public virtual Character? Character { get; set; }

    public int DevotionDefinitionId { get; set; }

    [ForeignKey(nameof(DevotionDefinitionId))]
    public virtual DevotionDefinition? DevotionDefinition { get; set; }
}
