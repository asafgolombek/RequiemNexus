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
