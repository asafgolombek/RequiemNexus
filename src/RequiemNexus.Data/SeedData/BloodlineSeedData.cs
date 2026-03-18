using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.SeedData;

/// <summary>
/// Seed data for bloodline definitions. Maps from bloodlines.json structure.
/// FourthDisciplineId is derived: the discipline in the bloodline's 4 that is not in the parent clan's 3.
/// </summary>
public static class BloodlineSeedData
{
    /// <summary>
    /// Creates bloodline definitions with their allowed parent clans.
    /// Requires clans and disciplines to be seeded first (have IDs).
    /// </summary>
    public static List<BloodlineDefinition> GetAllBloodlines(
        List<Clan> clans,
        List<Discipline> disciplines)
    {
        Clan Clan(string name) => clans.First(c => c.Name == name);
        Discipline Disc(string name) => disciplines.First(d => d.Name == name);

        var bloodlines = new List<BloodlineDefinition>();

        // Ankou (Mekhet): 4th = Vigor (Mekhet has Auspex, Celerity, Obfuscate)
        bloodlines.Add(new BloodlineDefinition
        {
            Name = "Ankou",
            Description = "Killers who keep the living separate from the dead for the greater good.",
            FourthDisciplineId = Disc("Vigor").Id,
            PrerequisiteBloodPotency = 2,
            BaneOverride = "Must roll Humanity to recover Willpower from Mask",
            AllowedParentClans =
            [
                new BloodlineClan { ClanId = Clan("Mekhet").Id },
            ],
        });

        // Icelus (Mekhet, Ventrue): 4th = Dominate for Mekhet; Auspex for Ventrue — use Dominate as canonical
        bloodlines.Add(new BloodlineDefinition
        {
            Name = "Icelus",
            Description = "Manipulators disguised as hypnotherapists plumbing the collective unconscious.",
            FourthDisciplineId = Disc("Dominate").Id,
            PrerequisiteBloodPotency = 2,
            BaneOverride = "Detachment inflicts a Mesmerized state",
            CustomRuleOverride = true,
            CustomRuleOverrideDescription = "Fourth discipline varies by parent clan per VtR 2e.",
            AllowedParentClans =
            [
                new BloodlineClan { ClanId = Clan("Mekhet").Id },
                new BloodlineClan { ClanId = Clan("Ventrue").Id },
            ],
        });

        // Khaibit (Mekhet): 4th = Vigor
        bloodlines.Add(new BloodlineDefinition
        {
            Name = "Khaibit",
            Description = "An ancient line of soldiers who fight darkness by embracing it.",
            FourthDisciplineId = Disc("Vigor").Id,
            PrerequisiteBloodPotency = 2,
            BaneOverride = "Temporarily blinded by any light bright enough to inhibit normal vision",
            AllowedParentClans =
            [
                new BloodlineClan { ClanId = Clan("Mekhet").Id },
            ],
        });

        // Kerberos (Gangrel): 4th = Majesty
        bloodlines.Add(new BloodlineDefinition
        {
            Name = "Kerberos",
            Description = "Self-reinventing social predators who excel at projecting the Beast.",
            FourthDisciplineId = Disc("Majesty").Id,
            PrerequisiteBloodPotency = 2,
            BaneOverride = "Lose 10-again to oppose characters without the Predatory Aura",
            CustomRuleOverride = true,
            CustomRuleOverrideDescription = "Hounds of Hell special feature.",
            AllowedParentClans =
            [
                new BloodlineClan { ClanId = Clan("Gangrel").Id },
            ],
        });

        // Lidérc (Daeva): 4th = Obfuscate
        bloodlines.Add(new BloodlineDefinition
        {
            Name = "Lidérc",
            Description = "Passionate lovers who drink from the ardor they incite.",
            FourthDisciplineId = Disc("Obfuscate").Id,
            PrerequisiteBloodPotency = 2,
            BaneOverride = "The Wanton Curse also disrupts Touchstones",
            CustomRuleOverride = true,
            CustomRuleOverrideDescription = "Siphon Devotions special feature.",
            AllowedParentClans =
            [
                new BloodlineClan { ClanId = Clan("Daeva").Id },
            ],
        });

        // Nosoi (Gangrel): 4th = Dominate
        bloodlines.Add(new BloodlineDefinition
        {
            Name = "Nosoi",
            Description = "Plague farmers who cultivate blood-borne disease in the herd.",
            FourthDisciplineId = Disc("Dominate").Id,
            PrerequisiteBloodPotency = 2,
            BaneOverride = "Imbibed Vitae untainted by your disease is capped nightly by Humanity",
            CustomRuleOverride = true,
            CustomRuleOverrideDescription = "Bloodline Devotions special feature.",
            AllowedParentClans =
            [
                new BloodlineClan { ClanId = Clan("Gangrel").Id },
            ],
        });

        // Vardyvle (Ventrue): 4th = Protean
        bloodlines.Add(new BloodlineDefinition
        {
            Name = "Vardyvle",
            Description = "Jealous dreamers who yearn for what they are not and lose themselves in others' lives.",
            FourthDisciplineId = Disc("Protean").Id,
            PrerequisiteBloodPotency = 2,
            BaneOverride = "Feeding from those living your dreams risks False Memories",
            CustomRuleOverride = true,
            CustomRuleOverrideDescription = "Shapeshifting Devotion special feature.",
            AllowedParentClans =
            [
                new BloodlineClan { ClanId = Clan("Ventrue").Id },
            ],
        });

        // Vilseduire (Mekhet, Nosferatu): 4th = Majesty (works for both)
        bloodlines.Add(new BloodlineDefinition
        {
            Name = "Vilseduire",
            Description = "Narcissistic rebels who revel in glamorous transgression.",
            FourthDisciplineId = Disc("Majesty").Id,
            PrerequisiteBloodPotency = 2,
            BaneOverride = "Only one Touchstone, and risk detachment at lower Humanity",
            AllowedParentClans =
            [
                new BloodlineClan { ClanId = Clan("Mekhet").Id },
                new BloodlineClan { ClanId = Clan("Nosferatu").Id },
            ],
        });

        return bloodlines;
    }
}
