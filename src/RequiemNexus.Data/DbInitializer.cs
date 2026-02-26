using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(ApplicationDbContext context)
    {
        await context.Database.MigrateAsync();

        if (await context.Clans.AnyAsync() || await context.Disciplines.AnyAsync() || await context.Merits.AnyAsync())
        {
            return; // DB has been seeded
        }

        // 1. Seed Clans
        var clans = new List<Clan>
        {
            new() { Name = "Daeva", Description = "The succubi, masters of passion and manipulation." },
            new() { Name = "Gangrel", Description = "The savages, predators closer to the Beast than man." },
            new() { Name = "Mekhet", Description = "The shadows, secretive keepers of occult knowledge." },
            new() { Name = "Nosferatu", Description = "The haunts, terrifying monsters twisted by the Curse." },
            new() { Name = "Ventrue", Description = "The lords, aristocratic tyrants who demand fealty." }
        };

        await context.Clans.AddRangeAsync(clans);
        await context.SaveChangesAsync();

        // 2. Seed Disciplines
        var animalism = new Discipline { Name = "Animalism", Description = "Dominion over beasts and the feral nature." };
        animalism.Powers.Add(new DisciplinePower { Name = "Feral Whispers", Level = 1, Description = "Speak with and command animals.", DicePool = "Manipulation + Animal Ken + Animalism" });
        animalism.Powers.Add(new DisciplinePower { Name = "Raise the Familiar", Level = 2, Description = "Turn dead animal into a loyal proto-vampire.", Cost = "1 Vitae" });
        animalism.Powers.Add(new DisciplinePower { Name = "Summon the Hunt", Level = 3, Description = "Call animals to a location or target with spilled blood.", DicePool = "Presence + Animal Ken + Animalism" });
        animalism.Powers.Add(new DisciplinePower { Name = "Feral Infection", Level = 4, Description = "Drives animals, humans and vampires into a frenzy.", DicePool = "Presence + Intimidation + Animalism" });
        animalism.Powers.Add(new DisciplinePower { Name = "Lord of the Land", Level = 5, Description = "Mark territory as own, intruders take penalties.", Cost = "1 Willpower" });

        var auspex = new Discipline { Name = "Auspex", Description = "Preternatural perception and psychic awareness." };
        auspex.Powers.Add(new DisciplinePower { Name = "Beast's Senses", Level = 1, Description = "Heighten senses to supernatural levels." });
        auspex.Powers.Add(new DisciplinePower { Name = "Aura Perception", Level = 2, Description = "Read the emotional resonance and nature of a subject.", DicePool = "Wits + Empathy + Auspex" });
        auspex.Powers.Add(new DisciplinePower { Name = "The Spirit's Touch", Level = 3, Description = "Read psychic residue from objects.", DicePool = "Wits + Occult + Auspex" });
        auspex.Powers.Add(new DisciplinePower { Name = "Lay Open the Mind", Level = 4, Description = "Read the surface thoughts of a target.", DicePool = "Intelligence + Investigation + Auspex vs Resolve + Blood Potency" });
        auspex.Powers.Add(new DisciplinePower { Name = "Twilight Projection", Level = 5, Description = "Project awareness out of the body in twilight state.", Cost = "1 Willpower" });

        var celerity = new Discipline { Name = "Celerity", Description = "Supernatural speed and reflexes." };
        celerity.Powers.Add(new DisciplinePower { Name = "Celerity 1", Level = 1, Description = "Add dots to Initiative, subtract from attack pools.", Cost = "1 Vitae" });
        celerity.Powers.Add(new DisciplinePower { Name = "Celerity 2", Level = 2, Description = "Can take a reflexive dash action.", Cost = "1 Vitae" });
        celerity.Powers.Add(new DisciplinePower { Name = "Celerity 3", Level = 3, Description = "Subtract from all incoming attack pools.", Cost = "1 Vitae" });
        celerity.Powers.Add(new DisciplinePower { Name = "Celerity 4", Level = 4, Description = "Can ignore minor environmental hazards.", Cost = "1 Vitae" });
        celerity.Powers.Add(new DisciplinePower { Name = "Celerity 5", Level = 5, Description = "Take two actions instead of one.", Cost = "1 Vitae" });

        var dominate = new Discipline { Name = "Dominate", Description = "Crushing mental control over others." };
        dominate.Powers.Add(new DisciplinePower { Name = "Mesmerize", Level = 1, Description = "Plant a single command in a hypnotic trance.", DicePool = "Intelligence + Expression + Dominate vs Resolve + Blood Potency" });
        dominate.Powers.Add(new DisciplinePower { Name = "Command", Level = 2, Description = "Give a simple, immediate order.", DicePool = "Manipulation + Intimidation + Dominate vs Resolve + Blood Potency" });
        dominate.Powers.Add(new DisciplinePower { Name = "The Forgetful Mind", Level = 3, Description = "Alter or erase recent memories.", DicePool = "Wits + Subterfuge + Dominate vs Resolve + Blood Potency" });
        dominate.Powers.Add(new DisciplinePower { Name = "Conditioning", Level = 4, Description = "Long-term programming and susceptibility.", Cost = "1 Willpower" });
        dominate.Powers.Add(new DisciplinePower { Name = "Possession", Level = 5, Description = "Take direct control of a mortal's body.", Cost = "1 Willpower", DicePool = "Intelligence + Intimidation + Dominate vs Resolve + Blood Potency" });

        var majesty = new Discipline { Name = "Majesty", Description = "Supernatural allure and emotional manipulation." };
        majesty.Powers.Add(new DisciplinePower { Name = "Awe", Level = 1, Description = "Draw all eyes and fascinate onlookers.", DicePool = "Presence + Expression + Majesty vs Composure + Blood Potency" });
        majesty.Powers.Add(new DisciplinePower { Name = "Confidant", Level = 2, Description = "Make a target view you as an intimately trusted friend.", DicePool = "Manipulation + Empathy + Majesty vs Composure + Blood Potency" });
        majesty.Powers.Add(new DisciplinePower { Name = "Entrancement", Level = 3, Description = "Inspire terrifying devotion.", DicePool = "Manipulation + Persuasion + Majesty vs Composure + Blood Potency" });
        majesty.Powers.Add(new DisciplinePower { Name = "Summoning", Level = 4, Description = "Call a person to your side.", DicePool = "Presence + Persuasion + Majesty vs Composure + Blood Potency" });
        majesty.Powers.Add(new DisciplinePower { Name = "Sovereignty", Level = 5, Description = "Paralyze others with submissive fear and worship.", DicePool = "Presence + Intimidation + Majesty vs Composure + Blood Potency" });

        var nightmare = new Discipline { Name = "Nightmare", Description = "Weaponized terror." };
        nightmare.Powers.Add(new DisciplinePower { Name = "Dread", Level = 1, Description = "Elicit creeping paranoia and fear.", DicePool = "Presence + Empathy + Nightmare vs Composure + Blood Potency" });
        nightmare.Powers.Add(new DisciplinePower { Name = "Face of the Beast", Level = 2, Description = "Reveal a monstrous visage.", Cost = "1 Vitae" });
        nightmare.Powers.Add(new DisciplinePower { Name = "Aura of Terror", Level = 3, Description = "Radiate overwhelming fear.", Cost = "1 Vitae", DicePool = "Presence + Intimidation + Nightmare vs Composure + Blood Potency" });
        nightmare.Powers.Add(new DisciplinePower { Name = "Waking Nightmare", Level = 4, Description = "Force a target's worst fears to hallucinate.", Cost = "1 Willpower" });
        nightmare.Powers.Add(new DisciplinePower { Name = "Mortal Fear", Level = 5, Description = "A terrifying shock to the system.", Cost = "1 Vitae", DicePool = "Presence + Intimidation + Nightmare vs Composure + Blood Potency" });

        var obfuscate = new Discipline { Name = "Obfuscate", Description = "The power to remain unseen and ignored." };
        obfuscate.Powers.Add(new DisciplinePower { Name = "Touch of Shadow", Level = 1, Description = "Hide an object on your person.", DicePool = "Wits + Larceny + Obfuscate" });
        obfuscate.Powers.Add(new DisciplinePower { Name = "Mask of Tranquility", Level = 2, Description = "Hide your Predatory Aura and emotions.", Cost = "1 Vitae" });
        obfuscate.Powers.Add(new DisciplinePower { Name = "Cloak of Night", Level = 3, Description = "Turn utterly invisible.", Cost = "1 Vitae", DicePool = "Intelligence + Stealth + Obfuscate" });
        obfuscate.Powers.Add(new DisciplinePower { Name = "The Familiar Stranger", Level = 4, Description = "Disguise yourself as whoever the target expects.", Cost = "1 Vitae" });
        obfuscate.Powers.Add(new DisciplinePower { Name = "Oubliette", Level = 5, Description = "Erase a person or place from perception entirely.", Cost = "1 Willpower" });

        var protean = new Discipline { Name = "Protean", Description = "Shape-shifting and bestial adaptation." };
        protean.Powers.Add(new DisciplinePower { Name = "Unnatural Aspect", Level = 1, Description = "Gain feral eyes, or retractible claws.", Cost = "1 Vitae" });
        protean.Powers.Add(new DisciplinePower { Name = "Haven of Soil", Level = 2, Description = "Meld into the earth to rest safely.", Cost = "1 Vitae" });
        protean.Powers.Add(new DisciplinePower { Name = "Beast's Skin", Level = 3, Description = "Transform into a predatory animal (wolf, bat).", Cost = "1 Vitae" });
        protean.Powers.Add(new DisciplinePower { Name = "Shape of the Beast", Level = 4, Description = "Transform into a swarm or combat-beast.", Cost = "1 Vitae" });
        protean.Powers.Add(new DisciplinePower { Name = "Primeval Miasma", Level = 5, Description = "Transform into mist.", Cost = "1 Vitae" });

        var resilience = new Discipline { Name = "Resilience", Description = "Supernatural toughness." };
        resilience.Powers.Add(new DisciplinePower { Name = "Resilience 1", Level = 1, Description = "Add to Stamina and downgrade agg damage.", Cost = "1 Vitae" });
        resilience.Powers.Add(new DisciplinePower { Name = "Resilience 2", Level = 2, Description = "Ignore wound penalties temporarily.", Cost = "1 Vitae" });
        resilience.Powers.Add(new DisciplinePower { Name = "Resilience 3", Level = 3, Description = "Downgrade lethal damage to bashing.", Cost = "1 Vitae" });
        resilience.Powers.Add(new DisciplinePower { Name = "Resilience 4", Level = 4, Description = "Resist all mundane sources of damage.", Cost = "1 Vitae" });
        resilience.Powers.Add(new DisciplinePower { Name = "Resilience 5", Level = 5, Description = "Shrug off almost anything.", Cost = "1 Willpower" });

        var vigor = new Discipline { Name = "Vigor", Description = "Supernatural strength." };
        vigor.Powers.Add(new DisciplinePower { Name = "Vigor 1", Level = 1, Description = "Add dots to Strength and jumping.", Cost = "1 Vitae" });
        vigor.Powers.Add(new DisciplinePower { Name = "Vigor 2", Level = 2, Description = "Increase carrying capacity massively.", Cost = "1 Vitae" });
        vigor.Powers.Add(new DisciplinePower { Name = "Vigor 3", Level = 3, Description = "Break down doors easily.", Cost = "1 Vitae" });
        vigor.Powers.Add(new DisciplinePower { Name = "Vigor 4", Level = 4, Description = "Leap incredible distances.", Cost = "1 Vitae" });
        vigor.Powers.Add(new DisciplinePower { Name = "Vigor 5", Level = 5, Description = "Strike with earth-shattering force.", Cost = "1 Vitae" });

        var disciplinesList = new List<Discipline> { animalism, auspex, celerity, dominate, majesty, nightmare, obfuscate, protean, resilience, vigor };
        await context.Disciplines.AddRangeAsync(disciplinesList);
        await context.SaveChangesAsync();

        // 3. Map Clans to Disciplines
        var clanDisciplines = new List<ClanDiscipline>
        {
            // Daeva: Celerity, Majesty, Vigor
            new() { ClanId = clans.First(c => c.Name == "Daeva").Id, DisciplineId = disciplinesList.First(d => d.Name == "Celerity").Id },
            new() { ClanId = clans.First(c => c.Name == "Daeva").Id, DisciplineId = disciplinesList.First(d => d.Name == "Majesty").Id },
            new() { ClanId = clans.First(c => c.Name == "Daeva").Id, DisciplineId = disciplinesList.First(d => d.Name == "Vigor").Id },

            // Gangrel: Animalism, Protean, Resilience
            new() { ClanId = clans.First(c => c.Name == "Gangrel").Id, DisciplineId = disciplinesList.First(d => d.Name == "Animalism").Id },
            new() { ClanId = clans.First(c => c.Name == "Gangrel").Id, DisciplineId = disciplinesList.First(d => d.Name == "Protean").Id },
            new() { ClanId = clans.First(c => c.Name == "Gangrel").Id, DisciplineId = disciplinesList.First(d => d.Name == "Resilience").Id },

            // Mekhet: Auspex, Celerity, Obfuscate
            new() { ClanId = clans.First(c => c.Name == "Mekhet").Id, DisciplineId = disciplinesList.First(d => d.Name == "Auspex").Id },
            new() { ClanId = clans.First(c => c.Name == "Mekhet").Id, DisciplineId = disciplinesList.First(d => d.Name == "Celerity").Id },
            new() { ClanId = clans.First(c => c.Name == "Mekhet").Id, DisciplineId = disciplinesList.First(d => d.Name == "Obfuscate").Id },

            // Nosferatu: Nightmare, Obfuscate, Vigor
            new() { ClanId = clans.First(c => c.Name == "Nosferatu").Id, DisciplineId = disciplinesList.First(d => d.Name == "Nightmare").Id },
            new() { ClanId = clans.First(c => c.Name == "Nosferatu").Id, DisciplineId = disciplinesList.First(d => d.Name == "Obfuscate").Id },
            new() { ClanId = clans.First(c => c.Name == "Nosferatu").Id, DisciplineId = disciplinesList.First(d => d.Name == "Vigor").Id },

            // Ventrue: Animalism, Dominate, Resilience
            new() { ClanId = clans.First(c => c.Name == "Ventrue").Id, DisciplineId = disciplinesList.First(d => d.Name == "Animalism").Id },
            new() { ClanId = clans.First(c => c.Name == "Ventrue").Id, DisciplineId = disciplinesList.First(d => d.Name == "Dominate").Id },
            new() { ClanId = clans.First(c => c.Name == "Ventrue").Id, DisciplineId = disciplinesList.First(d => d.Name == "Resilience").Id }
        };

        await context.ClanDisciplines.AddRangeAsync(clanDisciplines);

        // 4. Load Merits from JSON
        var meritsJsonPath = @"c:\gitrepo\RequiemNexus\scraped_data.json";
        if (File.Exists(meritsJsonPath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(meritsJsonPath);
                var doc = JsonDocument.Parse(json);
                var meritsArray = doc.RootElement.GetProperty("merits");

                var meritEntities = new List<Merit>();
                foreach (var meritNode in meritsArray.EnumerateArray())
                {
                    var name = meritNode.TryGetProperty("name", out var node) ? node.GetString() : "Unknown Merit";
                    var rating = meritNode.TryGetProperty("rating", out var node2) ? node2.GetString() : "•";
                    var desc = meritNode.TryGetProperty("desc", out var node3) ? node3.GetString() : "";

                    if (string.IsNullOrWhiteSpace(name)) continue;

                    meritEntities.Add(new Merit
                    {
                        Name = name.Length > 100 ? name.Substring(0, 100) : name,
                        ValidRatings = rating ?? "•",
                        Description = desc ?? "",
                        RequiresSpecification = false,
                        CanBePurchasedMultipleTimes = false
                    });
                }
                
                await context.Merits.AddRangeAsync(meritEntities);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error seeding merits: {ex.Message}");
            }
        }

        await context.SaveChangesAsync();
    }
}
