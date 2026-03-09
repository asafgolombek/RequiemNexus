using RequiemNexus.Application.Contracts;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain;
using RequiemNexus.Domain.Contracts;

namespace RequiemNexus.Application.Services;

public class AdvancementService : IAdvancementService
{
    public bool TryUpgradeCoreTrait(Character character, AttributeId id, int currentRating, int newRating)
    {
        string traitName = id.ToString();
        var trait = character.Attributes.FirstOrDefault(a => a.Name == traitName);
        return TryUpgradeTrait(character, trait, newRating);
    }

    public bool TryUpgradeCoreTrait(Character character, SkillId id, int currentRating, int newRating)
    {
        string traitName = id.ToString();
        var trait = character.Skills.FirstOrDefault(s => s.Name == traitName);
        return TryUpgradeTrait(character, trait, newRating);
    }

    public void UpdateCoreTrait(Character character, AttributeId id, int newRating)
    {
        string traitName = id.ToString();
        var trait = character.Attributes.FirstOrDefault(a => a.Name == traitName);
        if (trait != null)
        {
            // Internal bypass for creation/editing where XP isn't involved
            typeof(CharacterAttribute).GetProperty("Rating")?.SetValue(trait, newRating);
        }
    }

    public void UpdateCoreTrait(Character character, SkillId id, int newRating)
    {
        string traitName = id.ToString();
        var trait = character.Skills.FirstOrDefault(s => s.Name == traitName);
        if (trait != null)
        {
            typeof(CharacterSkill).GetProperty("Rating")?.SetValue(trait, newRating);
        }
    }

    public void RecalculateDerivedStats(Character character)
    {
        int stamina = character.GetAttributeRating(AttributeId.Stamina);
        int resolve = character.GetAttributeRating(AttributeId.Resolve);
        int composure = character.GetAttributeRating(AttributeId.Composure);

        int newMaxHealth = character.Size + stamina;
        int newMaxWillpower = resolve + composure;

        int healthDiff = newMaxHealth - character.MaxHealth;
        character.MaxHealth = newMaxHealth;
        character.CurrentHealth = Math.Clamp(character.CurrentHealth + healthDiff, 0, newMaxHealth);

        int willpowerDiff = newMaxWillpower - character.MaxWillpower;
        character.MaxWillpower = newMaxWillpower;
        character.CurrentWillpower = Math.Clamp(character.CurrentWillpower + willpowerDiff, 0, newMaxWillpower);
    }

    private bool TryUpgradeTrait(Character character, IRatedTrait? trait, int newRating)
    {
        if (trait == null || newRating <= trait.Rating || newRating > 5)
        {
            return false;
        }

        int totalCost = trait.CalculateUpgradeCost(newRating);

        if (character.ExperiencePoints >= totalCost)
        {
            character.ExperiencePoints -= totalCost;

            // Use the new Upgrade method which handles Rating update and returns cost
            // (Even though we already calculated it, this preserves domain integrity)
            trait.Upgrade(newRating, new ExperienceCostRules());
            return true;
        }

        return false;
    }
}
