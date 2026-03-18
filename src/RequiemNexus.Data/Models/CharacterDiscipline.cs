using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using RequiemNexus.Domain;
using RequiemNexus.Domain.Contracts;

namespace RequiemNexus.Data.Models;

public class CharacterDiscipline : IRatedTrait
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

    [NotMapped]
    public string Name => Discipline?.Name ?? string.Empty;

    public int CalculateUpgradeCost(int toRating)
        => ExperienceCostRules.CalculateUpgradeCost(Rating, toRating, (Character?.IsDisciplineInClan(DisciplineId) ?? false) ? 4 : 5);

    public int Upgrade(int toRating, IExperienceCostRules rules)
    {
        if (toRating <= Rating)
        {
            throw new ArgumentException("Upgrade must be to a higher rating.", nameof(toRating));
        }

        bool isInClan = Character?.IsDisciplineInClan(DisciplineId) ?? false;
        int cost = rules.CalculateDisciplineUpgradeCost(Rating, toRating, isInClan);
        Rating = toRating;
        return cost;
    }
}
