using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RequiemNexus.Domain;
using RequiemNexus.Domain.Contracts;

namespace RequiemNexus.Data.Models;

public class CharacterMerit : IRatedTrait
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

    [NotMapped]
    public string Name => Merit?.Name ?? string.Empty;

    public int CalculateUpgradeCost(int toRating) => toRating;
}

