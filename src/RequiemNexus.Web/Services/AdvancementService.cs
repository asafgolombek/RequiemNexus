using RequiemNexus.Data.Models;
using RequiemNexus.Domain;
using RequiemNexus.Web.Contracts;

namespace RequiemNexus.Web.Services;

public class AdvancementService : IAdvancementService
{
    public bool TryUpgradeCoreTrait(Character character, string traitName, int currentRating, int newRating)
    {
        // Find the trait in Attributes or Skills collections
        IRatedTrait? trait = TraitMetadata.IsAttribute(traitName)
            ? character.Attributes.FirstOrDefault(a => a.Name == traitName)
            : character.Skills.FirstOrDefault(s => s.Name == traitName);

        if (trait == null || newRating <= currentRating || newRating > 5) return false;

        int totalCost = trait.CalculateUpgradeCost(newRating);

        if (character.ExperiencePoints >= totalCost)
        {
            character.ExperiencePoints -= totalCost;
            trait.Rating = newRating;
            return true;
        }

        return false;
    }

    public void UpdateCoreTrait(Character character, string traitName, int newRating)
    {
        IRatedTrait? trait = TraitMetadata.IsAttribute(traitName)
            ? character.Attributes.FirstOrDefault(a => a.Name == traitName)
            : character.Skills.FirstOrDefault(s => s.Name == traitName);

        if (trait != null)
            trait.Rating = newRating;
    }

    /// <summary>
    /// Recalculates MaxHealth and MaxWillpower after attributes change.
    /// Applies the delta to Current values to preserve existing injuries/willpower spend.
    /// </summary>
    public void RecalculateDerivedStats(Character character)
    {
        int stamina = character.GetAttributeRating("Stamina");
        int resolve = character.GetAttributeRating("Resolve");
        int composure = character.GetAttributeRating("Composure");

        int newMaxHealth = character.Size + stamina;
        int newMaxWillpower = resolve + composure;

        int healthDiff = newMaxHealth - character.MaxHealth;
        character.MaxHealth = newMaxHealth;
        character.CurrentHealth = Math.Clamp(character.CurrentHealth + healthDiff, 0, newMaxHealth);

        int willpowerDiff = newMaxWillpower - character.MaxWillpower;
        character.MaxWillpower = newMaxWillpower;
        character.CurrentWillpower = Math.Clamp(character.CurrentWillpower + willpowerDiff, 0, newMaxWillpower);
    }
}

