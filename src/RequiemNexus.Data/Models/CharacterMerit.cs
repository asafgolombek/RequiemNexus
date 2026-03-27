using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RequiemNexus.Domain.Contracts;
using RequiemNexus.Domain.Services;

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

    public int CalculateUpgradeCost(int toRating)
        => ExperienceCostRules.CalculateUpgradeCost(Rating, toRating, costMultiplier: 1);

    public int Upgrade(int toRating, IExperienceCostRules rules)
    {
        if (toRating <= Rating)
        {
            throw new ArgumentException("Upgrade must be to a higher rating.", nameof(toRating));
        }

        int cost = rules.CalculateMeritCost(Rating, toRating);
        Rating = toRating;
        return cost;
    }
}
