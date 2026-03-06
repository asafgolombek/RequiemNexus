using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RequiemNexus.Data.Models.Enums;
using RequiemNexus.Domain;
using RequiemNexus.Domain.Contracts;

namespace RequiemNexus.Data.Models;

public class CharacterSkill : IRatedTrait
{
    [Key]
    public int Id { get; set; }

    public int CharacterId { get; set; }

    [ForeignKey(nameof(CharacterId))]
    public virtual Character? Character { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    public TraitCategory Category { get; set; }

    public int Rating { get; set; }

    [MaxLength(100)]
    public string? Specialty { get; set; }

    public int CalculateUpgradeCost(int toRating)
        => ExperienceCostRules.CalculateUpgradeCost(Rating, toRating, costMultiplier: 2);
}
