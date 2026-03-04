using RequiemNexus.Data.Models;
using RequiemNexus.Domain;

namespace RequiemNexus.Web.Helpers;

/// <summary>
/// Provides reflection-based get/set for Character trait properties using TraitMetadata names.
/// Lives in the Web layer since it depends on the Character model from Data.
/// </summary>
public static class CharacterTraitHelper
{
    /// <summary>
    /// Gets the integer value of a trait from a Character by property name.
    /// "Animal Ken" is normalized to "AnimalKen" before lookup.
    /// </summary>
    public static int GetTraitValue(Character character, string traitName)
    {
        var propName = traitName.Replace(" ", "");
        var prop = typeof(Character).GetProperty(propName);
        return (int)(prop?.GetValue(character) ?? 0);
    }

    /// <summary>
    /// Sets the integer value of a trait on a Character by property name.
    /// </summary>
    public static void SetTraitValue(Character character, string traitName, int value)
    {
        var propName = traitName.Replace(" ", "");
        typeof(Character).GetProperty(propName)?.SetValue(character, value);
    }
}
