using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.Seeding;

/// <summary>
/// Seeds built-in NPC stat blocks for encounter tooling when none exist.
/// </summary>
public sealed class PrebuiltNpcSeeder : ISeeder
{
    /// <inheritdoc />
    public int Order => 140;

    /// <inheritdoc />
    public async Task SeedAsync(ApplicationDbContext context, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        if (await context.NpcStatBlocks.AnyAsync(s => s.IsPrebuilt))
        {
            return;
        }

        NpcStatBlock[] blocks =
        [
            new()
            {
                Name = "Mortal",
                Concept = "Average human",
                Size = 5,
                Health = 7,
                Willpower = 3,
                BludgeoningArmor = 0,
                LethalArmor = 0,
                AttributesJson = "{\"Strength\":2,\"Dexterity\":2,\"Stamina\":2,\"Intelligence\":2,\"Wits\":2,\"Resolve\":2,\"Presence\":2,\"Manipulation\":2,\"Composure\":2}",
                SkillsJson = "{\"Brawl\":1,\"Athletics\":1,\"Drive\":1}",
                DisciplinesJson = "{}",
                Notes = "Standard human. No supernatural traits.",
                IsPrebuilt = true,
            },
            new()
            {
                Name = "Ghoul",
                Concept = "Vitae-bound servant",
                Size = 5,
                Health = 8,
                Willpower = 4,
                BludgeoningArmor = 0,
                LethalArmor = 0,
                AttributesJson = "{\"Strength\":3,\"Dexterity\":2,\"Stamina\":3,\"Intelligence\":2,\"Wits\":2,\"Resolve\":2,\"Presence\":2,\"Manipulation\":2,\"Composure\":2}",
                SkillsJson = "{\"Brawl\":2,\"Athletics\":2,\"Firearms\":1,\"Stealth\":1}",
                DisciplinesJson = "{\"Vigor\":1}",
                Notes = "Fed vampiric vitae. Has one dot of a regnant's Discipline. Resists frenzy-like impulses.",
                IsPrebuilt = true,
            },
            new()
            {
                Name = "Neonate",
                Concept = "Newly Embraced vampire",
                Size = 5,
                Health = 8,
                Willpower = 5,
                BludgeoningArmor = 1,
                LethalArmor = 0,
                AttributesJson = "{\"Strength\":3,\"Dexterity\":3,\"Stamina\":3,\"Intelligence\":2,\"Wits\":3,\"Resolve\":2,\"Presence\":2,\"Manipulation\":2,\"Composure\":3}",
                SkillsJson = "{\"Brawl\":2,\"Athletics\":2,\"Stealth\":2,\"Intimidation\":1}",
                DisciplinesJson = "{\"Clan Discipline 1\":2,\"Clan Discipline 2\":1}",
                Notes = "Blood Potency 1. Two clan Disciplines at 2 and 1 dots. Replaces clan name with specific one at the table.",
                IsPrebuilt = true,
            },
            new()
            {
                Name = "Elder",
                Concept = "Ancient and formidable vampire",
                Size = 5,
                Health = 10,
                Willpower = 8,
                BludgeoningArmor = 2,
                LethalArmor = 1,
                AttributesJson = "{\"Strength\":4,\"Dexterity\":4,\"Stamina\":4,\"Intelligence\":4,\"Wits\":4,\"Resolve\":4,\"Presence\":4,\"Manipulation\":4,\"Composure\":4}",
                SkillsJson = "{\"Brawl\":4,\"Athletics\":3,\"Intimidation\":4,\"Persuasion\":4,\"Occult\":4,\"Stealth\":3}",
                DisciplinesJson = "{\"Primary Discipline\":5,\"Secondary Discipline\":3,\"Tertiary Discipline\":2}",
                Notes = "Blood Potency 5+. Treat as a major threat. Customize Disciplines to match covenant.",
                IsPrebuilt = true,
            },
            new()
            {
                Name = "Coterie Member",
                Concept = "PC-caliber allied vampire",
                Size = 5,
                Health = 8,
                Willpower = 6,
                BludgeoningArmor = 1,
                LethalArmor = 0,
                AttributesJson = "{\"Strength\":3,\"Dexterity\":3,\"Stamina\":3,\"Intelligence\":3,\"Wits\":3,\"Resolve\":3,\"Presence\":3,\"Manipulation\":3,\"Composure\":3}",
                SkillsJson = "{\"Brawl\":2,\"Athletics\":2,\"Stealth\":2,\"Persuasion\":2,\"Subterfuge\":2}",
                DisciplinesJson = "{\"Clan Discipline\":3}",
                Notes = "Use as a friendly NPC coterie member or as a baseline for PC-equivalent antagonist.",
                IsPrebuilt = true,
            },
            new()
            {
                Name = "Sheriff's Deputy",
                Concept = "Law enforcer of the Danse Macabre",
                Size = 5,
                Health = 9,
                Willpower = 6,
                BludgeoningArmor = 1,
                LethalArmor = 0,
                AttributesJson = "{\"Strength\":4,\"Dexterity\":3,\"Stamina\":4,\"Intelligence\":2,\"Wits\":3,\"Resolve\":3,\"Presence\":3,\"Manipulation\":2,\"Composure\":3}",
                SkillsJson = "{\"Brawl\":3,\"Athletics\":3,\"Intimidation\":3,\"Firearms\":2,\"Stealth\":2}",
                DisciplinesJson = "{\"Vigor\":2,\"Resilience\":2}",
                Notes = "Enforcer of Elysium law. Authorized to use violence. Reports directly to the Sheriff.",
                IsPrebuilt = true,
            },
            new()
            {
                Name = "Primogen",
                Concept = "Covenant representative on the council",
                Size = 5,
                Health = 8,
                Willpower = 7,
                BludgeoningArmor = 1,
                LethalArmor = 0,
                AttributesJson = "{\"Strength\":3,\"Dexterity\":3,\"Stamina\":3,\"Intelligence\":4,\"Wits\":4,\"Resolve\":4,\"Presence\":4,\"Manipulation\":4,\"Composure\":4}",
                SkillsJson = "{\"Persuasion\":4,\"Subterfuge\":4,\"Politics\":4,\"Empathy\":3,\"Intimidation\":3}",
                DisciplinesJson = "{\"Dominate\":3,\"Auspex\":2}",
                Notes = "Political powerhouse. Rarely acts in person — prefers proxies and leverage.",
                IsPrebuilt = true,
            },
            new()
            {
                Name = "Hound",
                Concept = "Prince's investigator and executioner",
                Size = 5,
                Health = 9,
                Willpower = 7,
                BludgeoningArmor = 2,
                LethalArmor = 1,
                AttributesJson = "{\"Strength\":4,\"Dexterity\":4,\"Stamina\":4,\"Intelligence\":3,\"Wits\":4,\"Resolve\":3,\"Presence\":3,\"Manipulation\":3,\"Composure\":4}",
                SkillsJson = "{\"Brawl\":4,\"Athletics\":3,\"Stealth\":3,\"Investigation\":3,\"Intimidation\":3,\"Firearms\":2}",
                DisciplinesJson = "{\"Celerity\":3,\"Vigor\":2,\"Auspex\":2}",
                Notes = "Operates on the Prince's authority. Combines investigator and enforcer roles.",
                IsPrebuilt = true,
            },
            new()
            {
                Name = "Revenant",
                Concept = "Dhampir-like hereditary ghoul",
                Size = 5,
                Health = 9,
                Willpower = 5,
                BludgeoningArmor = 1,
                LethalArmor = 0,
                AttributesJson = "{\"Strength\":4,\"Dexterity\":3,\"Stamina\":4,\"Intelligence\":2,\"Wits\":3,\"Resolve\":3,\"Presence\":2,\"Manipulation\":2,\"Composure\":3}",
                SkillsJson = "{\"Brawl\":3,\"Athletics\":2,\"Stealth\":2,\"Survival\":2}",
                DisciplinesJson = "{\"Vigor\":2,\"Resilience\":1}",
                Notes = "Born of a ghoul bloodline. Generates own vitae slowly. More feral than a standard ghoul.",
                IsPrebuilt = true,
            },
            new()
            {
                Name = "Blood Doll",
                Concept = "Willing human vessel",
                Size = 5,
                Health = 7,
                Willpower = 2,
                BludgeoningArmor = 0,
                LethalArmor = 0,
                AttributesJson = "{\"Strength\":2,\"Dexterity\":2,\"Stamina\":2,\"Intelligence\":2,\"Wits\":2,\"Resolve\":1,\"Presence\":3,\"Manipulation\":2,\"Composure\":1}",
                SkillsJson = "{\"Persuasion\":1,\"Socialize\":2,\"Expression\":1}",
                DisciplinesJson = "{}",
                Notes = "Willingly seeks out vampires for feeding. Low Resolve and Composure — easily Dominated or addicted to the Kiss.",
                IsPrebuilt = true,
            },
            new()
            {
                Name = "Thin-Blood",
                Concept = "Fifteenth generation vampire",
                Size = 5,
                Health = 7,
                Willpower = 4,
                BludgeoningArmor = 0,
                LethalArmor = 0,
                AttributesJson = "{\"Strength\":2,\"Dexterity\":2,\"Stamina\":2,\"Intelligence\":2,\"Wits\":3,\"Resolve\":2,\"Presence\":2,\"Manipulation\":3,\"Composure\":2}",
                SkillsJson = "{\"Athletics\":1,\"Stealth\":2,\"Streetwise\":2,\"Subterfuge\":2}",
                DisciplinesJson = "{\"Thin-Blood Alchemy\":1}",
                Notes = "Blood Potency 0. Weak vampire — not truly Kindred. Can walk in daylight with effort. Often hunted by the city's enforcers.",
                IsPrebuilt = true,
            },
            new()
            {
                Name = "Hunter (Witch)",
                Concept = "Occult vampire hunter",
                Size = 5,
                Health = 7,
                Willpower = 7,
                BludgeoningArmor = 0,
                LethalArmor = 0,
                AttributesJson = "{\"Strength\":2,\"Dexterity\":3,\"Stamina\":2,\"Intelligence\":4,\"Wits\":4,\"Resolve\":4,\"Presence\":2,\"Manipulation\":3,\"Composure\":4}",
                SkillsJson = "{\"Occult\":4,\"Investigation\":3,\"Crafts\":3,\"Stealth\":2,\"Firearms\":2,\"Athletics\":2}",
                DisciplinesJson = "{}",
                Notes = "Mortal hunter with occult expertise. Carries crafted wards, blessed rounds, or alchemical weapons. High Resolve makes Dominate and Majesty harder to land.",
                IsPrebuilt = true,
            },
        ];

        await context.NpcStatBlocks.AddRangeAsync(blocks);
        await context.SaveChangesAsync();
    }
}
