using RequiemNexus.Data.Models;
using RequiemNexus.Web.Contracts;

namespace RequiemNexus.Web.Services;

public class AdvancementService : IAdvancementService
{
    private static readonly HashSet<string> AttributeNames =
    [
        "Intelligence", "Wits", "Resolve",
        "Strength", "Dexterity", "Stamina",
        "Presence", "Manipulation", "Composure"
    ];

    public bool TryUpgradeCoreTrait(Character character, string traitName, int currentRating, int newRating)
    {
        if (AttributeNames.Contains(traitName))
            return TryUpgradeAttribute(character, traitName, currentRating, newRating);
        else
            return TryUpgradeSkill(character, traitName, currentRating, newRating);
    }

    public void UpdateCoreTrait(Character character, string traitName, int newRating)
    {
        SetTrait(character, traitName, newRating);
    }

    /// <summary>
    /// Recalculates MaxHealth and MaxWillpower after attributes change.
    /// Applies the delta to Current values to preserve existing injuries/willpower spend.
    /// </summary>
    public void RecalculateDerivedStats(Character character)
    {
        int newMaxHealth = character.Size + character.Stamina;
        int newMaxWillpower = character.Resolve + character.Composure;

        int healthDiff = newMaxHealth - character.MaxHealth;
        character.MaxHealth = newMaxHealth;
        character.CurrentHealth = Math.Clamp(character.CurrentHealth + healthDiff, 0, newMaxHealth);

        int willpowerDiff = newMaxWillpower - character.MaxWillpower;
        character.MaxWillpower = newMaxWillpower;
        character.CurrentWillpower = Math.Clamp(character.CurrentWillpower + willpowerDiff, 0, newMaxWillpower);
    }

    private bool TryUpgradeAttribute(Character character, string traitName, int currentRating, int newRating)
    {
        if (newRating <= currentRating || newRating > 5) return false;

        // Cost is New Dots x 4
        int totalCost = 0;
        for (int i = currentRating + 1; i <= newRating; i++)
            totalCost += i * 4;

        if (character.ExperiencePoints >= totalCost)
        {
            character.ExperiencePoints -= totalCost;
            SetTrait(character, traitName, newRating);
            return true;
        }

        return false;
    }

    private bool TryUpgradeSkill(Character character, string traitName, int currentRating, int newRating)
    {
        if (newRating <= currentRating || newRating > 5) return false;

        // Cost is New Dots x 2
        int totalCost = 0;
        for (int i = currentRating + 1; i <= newRating; i++)
            totalCost += i * 2;

        if (character.ExperiencePoints >= totalCost)
        {
            character.ExperiencePoints -= totalCost;
            SetTrait(character, traitName, newRating);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Uses reflection to set a trait value on the character by property name.
    /// "Animal Ken" → AnimalKen (spaces stripped), then resolved via reflection.
    /// </summary>
    private static void SetTrait(Character character, string traitName, int value)
    {
        var propName = traitName.Replace(" ", "");
        typeof(Character).GetProperty(propName)?.SetValue(character, value);
    }
}
