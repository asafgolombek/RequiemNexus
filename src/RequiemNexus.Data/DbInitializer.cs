using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.SeedData;

namespace RequiemNexus.Data;

public static class DbInitializer
{
    private const string _clanDaeva = "Daeva";
    private const string _clanGangrel = "Gangrel";
    private const string _clanMekhet = "Mekhet";
    private const string _clanNosferatu = "Nosferatu";
    private const string _clanVentrue = "Ventrue";
    private const string _costOneVitae = "1 Vitae";
    private const string _costOneWillpower = "1 Willpower";

    public static async Task InitializeAsync(ApplicationDbContext context, RoleManager<IdentityRole> roleManager, bool runMigrations = false)
    {
        if (runMigrations)
        {
            await context.Database.MigrateAsync();
        }

        await SeedRolesAsync(roleManager);
        await SeedClansAndDisciplinesAsync(context);
        await SeedMeritsAsync(context);
        await SeedBloodlinesAsync(context);
        await SeedDevotionsAsync(context);
        await SeedPrebuiltStatBlocksAsync(context);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        string[] roles = ["Player", "Storyteller", "Admin"];
        foreach (string role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    private static async Task SeedClansAndDisciplinesAsync(ApplicationDbContext context)
    {
        bool hasClansAndDisciplines = await context.Clans.AnyAsync() && await context.Disciplines.AnyAsync();

        if (!hasClansAndDisciplines)
        {
            // 1. Seed Clans
            var clans = new List<Clan>
        {
            new() { Name = _clanDaeva, Description = "The succubi, masters of passion and manipulation." },
            new() { Name = _clanGangrel, Description = "The savages, predators closer to the Beast than man." },
            new() { Name = _clanMekhet, Description = "The shadows, secretive keepers of occult knowledge." },
            new() { Name = _clanNosferatu, Description = "The haunts, terrifying monsters twisted by the Curse." },
            new() { Name = _clanVentrue, Description = "The lords, aristocratic tyrants who demand fealty." },
        };

            await context.Clans.AddRangeAsync(clans);
            await context.SaveChangesAsync();

            // 2. Seed Disciplines
            var animalism = new Discipline { Name = "Animalism", Description = "Dominion over beasts and the feral nature." };
            animalism.Powers.Add(new DisciplinePower { Name = "Feral Whispers", Level = 1, Description = "Speak with and command animals.", DicePool = "Manipulation + Animal Ken + Animalism" });
            animalism.Powers.Add(new DisciplinePower { Name = "Raise the Familiar", Level = 2, Description = "Turn dead animal into a loyal proto-vampire.", Cost = _costOneVitae });
            animalism.Powers.Add(new DisciplinePower { Name = "Summon the Hunt", Level = 3, Description = "Call animals to a location or target with spilled blood.", DicePool = "Presence + Animal Ken + Animalism" });
            animalism.Powers.Add(new DisciplinePower { Name = "Feral Infection", Level = 4, Description = "Drives animals, humans and vampires into a frenzy.", DicePool = "Presence + Intimidation + Animalism" });
            animalism.Powers.Add(new DisciplinePower { Name = "Lord of the Land", Level = 5, Description = "Mark territory as own, intruders take penalties.", Cost = _costOneWillpower });

            var auspex = new Discipline { Name = "Auspex", Description = "Preternatural perception and psychic awareness." };
            auspex.Powers.Add(new DisciplinePower { Name = "Beast's Senses", Level = 1, Description = "Heighten senses to supernatural levels." });
            auspex.Powers.Add(new DisciplinePower { Name = "Aura Perception", Level = 2, Description = "Read the emotional resonance and nature of a subject.", DicePool = "Wits + Empathy + Auspex" });
            auspex.Powers.Add(new DisciplinePower { Name = "The Spirit's Touch", Level = 3, Description = "Read psychic residue from objects.", DicePool = "Wits + Occult + Auspex" });
            auspex.Powers.Add(new DisciplinePower { Name = "Lay Open the Mind", Level = 4, Description = "Read the surface thoughts of a target.", DicePool = "Intelligence + Investigation + Auspex vs Resolve + Blood Potency" });
            auspex.Powers.Add(new DisciplinePower { Name = "Twilight Projection", Level = 5, Description = "Project awareness out of the body in twilight state.", Cost = _costOneWillpower });

            var celerity = new Discipline { Name = "Celerity", Description = "Supernatural speed and reflexes." };
            celerity.Powers.Add(new DisciplinePower { Name = "Celerity 1", Level = 1, Description = "Add dots to Initiative, subtract from attack pools.", Cost = _costOneVitae });
            celerity.Powers.Add(new DisciplinePower { Name = "Celerity 2", Level = 2, Description = "Can take a reflexive dash action.", Cost = _costOneVitae });
            celerity.Powers.Add(new DisciplinePower { Name = "Celerity 3", Level = 3, Description = "Subtract from all incoming attack pools.", Cost = _costOneVitae });
            celerity.Powers.Add(new DisciplinePower { Name = "Celerity 4", Level = 4, Description = "Can ignore minor environmental hazards.", Cost = _costOneVitae });
            celerity.Powers.Add(new DisciplinePower { Name = "Celerity 5", Level = 5, Description = "Take two actions instead of one.", Cost = _costOneVitae });

            var dominate = new Discipline { Name = "Dominate", Description = "Crushing mental control over others." };
            dominate.Powers.Add(new DisciplinePower { Name = "Mesmerize", Level = 1, Description = "Plant a single command in a hypnotic trance.", DicePool = "Intelligence + Expression + Dominate vs Resolve + Blood Potency" });
            dominate.Powers.Add(new DisciplinePower { Name = "Command", Level = 2, Description = "Give a simple, immediate order.", DicePool = "Manipulation + Intimidation + Dominate vs Resolve + Blood Potency" });
            dominate.Powers.Add(new DisciplinePower { Name = "The Forgetful Mind", Level = 3, Description = "Alter or erase recent memories.", DicePool = "Wits + Subterfuge + Dominate vs Resolve + Blood Potency" });
            dominate.Powers.Add(new DisciplinePower { Name = "Conditioning", Level = 4, Description = "Long-term programming and susceptibility.", Cost = _costOneWillpower });
            dominate.Powers.Add(new DisciplinePower { Name = "Possession", Level = 5, Description = "Take direct control of a mortal's body.", Cost = _costOneWillpower, DicePool = "Intelligence + Intimidation + Dominate vs Resolve + Blood Potency" });

            var majesty = new Discipline { Name = "Majesty", Description = "Supernatural allure and emotional manipulation." };
            majesty.Powers.Add(new DisciplinePower { Name = "Awe", Level = 1, Description = "Draw all eyes and fascinate onlookers.", DicePool = "Presence + Expression + Majesty vs Composure + Blood Potency" });
            majesty.Powers.Add(new DisciplinePower { Name = "Confidant", Level = 2, Description = "Make a target view you as an intimately trusted friend.", DicePool = "Manipulation + Empathy + Majesty vs Composure + Blood Potency" });
            majesty.Powers.Add(new DisciplinePower { Name = "Entrancement", Level = 3, Description = "Inspire terrifying devotion.", DicePool = "Manipulation + Persuasion + Majesty vs Composure + Blood Potency" });
            majesty.Powers.Add(new DisciplinePower { Name = "Summoning", Level = 4, Description = "Call a person to your side.", DicePool = "Presence + Persuasion + Majesty vs Composure + Blood Potency" });
            majesty.Powers.Add(new DisciplinePower { Name = "Sovereignty", Level = 5, Description = "Paralyze others with submissive fear and worship.", DicePool = "Presence + Intimidation + Majesty vs Composure + Blood Potency" });

            var nightmare = new Discipline { Name = "Nightmare", Description = "Weaponized terror." };
            nightmare.Powers.Add(new DisciplinePower { Name = "Dread", Level = 1, Description = "Elicit creeping paranoia and fear.", DicePool = "Presence + Empathy + Nightmare vs Composure + Blood Potency" });
            nightmare.Powers.Add(new DisciplinePower { Name = "Face of the Beast", Level = 2, Description = "Reveal a monstrous visage.", Cost = _costOneVitae });
            nightmare.Powers.Add(new DisciplinePower { Name = "Aura of Terror", Level = 3, Description = "Radiate overwhelming fear.", Cost = _costOneVitae, DicePool = "Presence + Intimidation + Nightmare vs Composure + Blood Potency" });
            nightmare.Powers.Add(new DisciplinePower { Name = "Waking Nightmare", Level = 4, Description = "Force a target's worst fears to hallucinate.", Cost = _costOneWillpower });
            nightmare.Powers.Add(new DisciplinePower { Name = "Mortal Fear", Level = 5, Description = "A terrifying shock to the system.", Cost = _costOneVitae, DicePool = "Presence + Intimidation + Nightmare vs Composure + Blood Potency" });

            var obfuscate = new Discipline { Name = "Obfuscate", Description = "The power to remain unseen and ignored." };
            obfuscate.Powers.Add(new DisciplinePower { Name = "Touch of Shadow", Level = 1, Description = "Hide an object on your person.", DicePool = "Wits + Larceny + Obfuscate" });
            obfuscate.Powers.Add(new DisciplinePower { Name = "Mask of Tranquility", Level = 2, Description = "Hide your Predatory Aura and emotions.", Cost = _costOneVitae });
            obfuscate.Powers.Add(new DisciplinePower { Name = "Cloak of Night", Level = 3, Description = "Turn utterly invisible.", Cost = _costOneVitae, DicePool = "Intelligence + Stealth + Obfuscate" });
            obfuscate.Powers.Add(new DisciplinePower { Name = "The Familiar Stranger", Level = 4, Description = "Disguise yourself as whoever the target expects.", Cost = _costOneVitae });
            obfuscate.Powers.Add(new DisciplinePower { Name = "Oubliette", Level = 5, Description = "Erase a person or place from perception entirely.", Cost = _costOneWillpower });

            var protean = new Discipline { Name = "Protean", Description = "Shape-shifting and bestial adaptation." };
            protean.Powers.Add(new DisciplinePower { Name = "Unnatural Aspect", Level = 1, Description = "Gain feral eyes, or retractible claws.", Cost = _costOneVitae });
            protean.Powers.Add(new DisciplinePower { Name = "Haven of Soil", Level = 2, Description = "Meld into the earth to rest safely.", Cost = _costOneVitae });
            protean.Powers.Add(new DisciplinePower { Name = "Beast's Skin", Level = 3, Description = "Transform into a predatory animal (wolf, bat).", Cost = _costOneVitae });
            protean.Powers.Add(new DisciplinePower { Name = "Shape of the Beast", Level = 4, Description = "Transform into a swarm or combat-beast.", Cost = _costOneVitae });
            protean.Powers.Add(new DisciplinePower { Name = "Primeval Miasma", Level = 5, Description = "Transform into mist.", Cost = _costOneVitae });

            var resilience = new Discipline { Name = "Resilience", Description = "Supernatural toughness." };
            resilience.Powers.Add(new DisciplinePower { Name = "Resilience 1", Level = 1, Description = "Add to Stamina and downgrade agg damage.", Cost = _costOneVitae });
            resilience.Powers.Add(new DisciplinePower { Name = "Resilience 2", Level = 2, Description = "Ignore wound penalties temporarily.", Cost = _costOneVitae });
            resilience.Powers.Add(new DisciplinePower { Name = "Resilience 3", Level = 3, Description = "Downgrade lethal damage to bashing.", Cost = _costOneVitae });
            resilience.Powers.Add(new DisciplinePower { Name = "Resilience 4", Level = 4, Description = "Resist all mundane sources of damage.", Cost = _costOneVitae });
            resilience.Powers.Add(new DisciplinePower { Name = "Resilience 5", Level = 5, Description = "Shrug off almost anything.", Cost = _costOneWillpower });

            var vigor = new Discipline { Name = "Vigor", Description = "Supernatural strength." };
            vigor.Powers.Add(new DisciplinePower { Name = "Vigor 1", Level = 1, Description = "Add dots to Strength and jumping.", Cost = _costOneVitae });
            vigor.Powers.Add(new DisciplinePower { Name = "Vigor 2", Level = 2, Description = "Increase carrying capacity massively.", Cost = _costOneVitae });
            vigor.Powers.Add(new DisciplinePower { Name = "Vigor 3", Level = 3, Description = "Break down doors easily.", Cost = _costOneVitae });
            vigor.Powers.Add(new DisciplinePower { Name = "Vigor 4", Level = 4, Description = "Leap incredible distances.", Cost = _costOneVitae });
            vigor.Powers.Add(new DisciplinePower { Name = "Vigor 5", Level = 5, Description = "Strike with earth-shattering force.", Cost = _costOneVitae });

            var disciplinesList = new List<Discipline> { animalism, auspex, celerity, dominate, majesty, nightmare, obfuscate, protean, resilience, vigor };
            await context.Disciplines.AddRangeAsync(disciplinesList);
            await context.SaveChangesAsync();

            // 3. Map Clans to Disciplines
            var clanDisciplines = new List<ClanDiscipline>
            {
                // Daeva: Celerity, Majesty, Vigor
                new() { ClanId = clans.First(c => c.Name == _clanDaeva).Id, DisciplineId = disciplinesList.First(d => d.Name == "Celerity").Id },
                new() { ClanId = clans.First(c => c.Name == _clanDaeva).Id, DisciplineId = disciplinesList.First(d => d.Name == "Majesty").Id },
                new() { ClanId = clans.First(c => c.Name == _clanDaeva).Id, DisciplineId = disciplinesList.First(d => d.Name == "Vigor").Id },

                // Gangrel: Animalism, Protean, Resilience
                new() { ClanId = clans.First(c => c.Name == _clanGangrel).Id, DisciplineId = disciplinesList.First(d => d.Name == "Animalism").Id },
                new() { ClanId = clans.First(c => c.Name == _clanGangrel).Id, DisciplineId = disciplinesList.First(d => d.Name == "Protean").Id },
                new() { ClanId = clans.First(c => c.Name == _clanGangrel).Id, DisciplineId = disciplinesList.First(d => d.Name == "Resilience").Id },

                // Mekhet: Auspex, Celerity, Obfuscate
                new() { ClanId = clans.First(c => c.Name == _clanMekhet).Id, DisciplineId = disciplinesList.First(d => d.Name == "Auspex").Id },
                new() { ClanId = clans.First(c => c.Name == _clanMekhet).Id, DisciplineId = disciplinesList.First(d => d.Name == "Celerity").Id },
                new() { ClanId = clans.First(c => c.Name == _clanMekhet).Id, DisciplineId = disciplinesList.First(d => d.Name == "Obfuscate").Id },

                // Nosferatu: Nightmare, Obfuscate, Vigor
                new() { ClanId = clans.First(c => c.Name == _clanNosferatu).Id, DisciplineId = disciplinesList.First(d => d.Name == "Nightmare").Id },
                new() { ClanId = clans.First(c => c.Name == _clanNosferatu).Id, DisciplineId = disciplinesList.First(d => d.Name == "Obfuscate").Id },
                new() { ClanId = clans.First(c => c.Name == _clanNosferatu).Id, DisciplineId = disciplinesList.First(d => d.Name == "Vigor").Id },

                // Ventrue: Animalism, Dominate, Resilience
                new() { ClanId = clans.First(c => c.Name == _clanVentrue).Id, DisciplineId = disciplinesList.First(d => d.Name == "Animalism").Id },
                new() { ClanId = clans.First(c => c.Name == _clanVentrue).Id, DisciplineId = disciplinesList.First(d => d.Name == "Dominate").Id },
                new() { ClanId = clans.First(c => c.Name == _clanVentrue).Id, DisciplineId = disciplinesList.First(d => d.Name == "Resilience").Id },
            };

            await context.ClanDisciplines.AddRangeAsync(clanDisciplines);
        }
    }

    private static async Task SeedMeritsAsync(ApplicationDbContext context)
    {
        var existingNames = await context.Merits.Select(m => m.Name).ToListAsync();
        var existingSet = new HashSet<string>(existingNames, StringComparer.Ordinal);

        var toAdd = MeritSeedData.GetAllMerits()
            .Where(m => !existingSet.Contains(m.Name))
            .ToList();

        if (toAdd.Count > 0)
        {
            await context.Merits.AddRangeAsync(toAdd);
            await context.SaveChangesAsync();
        }
    }

    private static async Task SeedBloodlinesAsync(ApplicationDbContext context)
    {
        if (await context.BloodlineDefinitions.AnyAsync())
        {
            return;
        }

        var clans = await context.Clans.ToListAsync();
        var disciplines = await context.Disciplines.ToListAsync();
        var bloodlines = BloodlineSeedData.GetAllBloodlines(clans, disciplines);
        await context.BloodlineDefinitions.AddRangeAsync(bloodlines);
        await context.SaveChangesAsync();
    }

    private static async Task SeedDevotionsAsync(ApplicationDbContext context)
    {
        if (await context.DevotionDefinitions.AnyAsync())
        {
            return;
        }

        var disciplines = await context.Disciplines.ToListAsync();
        var devotions = DevotionSeedData.GetSampleDevotions(disciplines);
        await context.DevotionDefinitions.AddRangeAsync(devotions);
        await context.SaveChangesAsync();
    }

    private static async Task SeedPrebuiltStatBlocksAsync(ApplicationDbContext context)
    {
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
