namespace RequiemNexus.Domain;

/// <summary>
/// Centralized metadata for all Vampire: The Requiem character traits.
/// Contains only pure string arrays — no model dependencies.
/// </summary>
public static class TraitMetadata
{
    // --- Attributes ---
    public static readonly string[] MentalAttributes = ["Intelligence", "Wits", "Resolve"];
    public static readonly string[] PhysicalAttributes = ["Strength", "Dexterity", "Stamina"];
    public static readonly string[] SocialAttributes = ["Presence", "Manipulation", "Composure"];

    public static readonly string[] AllAttributes =
        [.. MentalAttributes, .. PhysicalAttributes, .. SocialAttributes];

    // --- Skills ---
    public static readonly string[] MentalSkills =
        ["Academics", "Computer", "Crafts", "Investigation", "Medicine", "Occult", "Politics", "Science"];
    public static readonly string[] PhysicalSkills =
        ["Athletics", "Brawl", "Drive", "Firearms", "Larceny", "Stealth", "Survival", "Weaponry"];
    public static readonly string[] SocialSkills =
        ["AnimalKen", "Empathy", "Expression", "Intimidation", "Persuasion", "Socialize", "Streetwise", "Subterfuge"];

    public static readonly string[] AllSkills =
        [.. MentalSkills, .. PhysicalSkills, .. SocialSkills];

    /// <summary>
    /// Returns the display name for a trait (e.g. "AnimalKen" → "Animal Ken").
    /// </summary>
    public static string GetDisplayName(string propertyName)
        => System.Text.RegularExpressions.Regex.Replace(propertyName, "(?<=[a-z])(?=[A-Z])", " ", System.Text.RegularExpressions.RegexOptions.None, TimeSpan.FromMilliseconds(250));


    /// <summary>
    /// Returns true if the given trait name is an Attribute (not a Skill).
    /// </summary>
    public static bool IsAttribute(string traitName) =>
        AllAttributes.Contains(traitName.Replace(" ", ""));
}
