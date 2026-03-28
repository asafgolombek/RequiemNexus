using System.Text.Json;
using Microsoft.Extensions.Logging;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.SeedData;

/// <summary>
/// Contains all discipline definitions for database seeding.
/// Separated from DbInitializer to keep seed data organized and maintainable.
/// </summary>
public static class DisciplineSeedData
{
    private const string _costOneVitae = "1 Vitae";
    private const string _costOneWillpower = "1 Willpower";

    public static List<Discipline> GetAll() =>
    [
        CreateAnimalism(),
        CreateAuspex(),
        CreateCelerity(),
        CreateDominate(),
        CreateMajesty(),
        CreateNightmare(),
        CreateObfuscate(),
        CreateProtean(),
        CreateResilience(),
        CreateVigor(),
        CreateCrúac(),
        CreateThebanSorcery(),
        CreateNecromancy(),
    ];

    /// <summary>
    /// Loads Discipline definitions from Disciplines.json (acquisition flags and pool JSON).
    /// CovenantId and BloodlineId are resolved in a second database seed pass after covenants and bloodlines exist.
    /// </summary>
    /// <param name="logger">Logger for parse failures.</param>
    /// <returns>Parsed disciplines, or <see cref="GetAll"/> when the file is missing or invalid.</returns>
    public static List<Discipline> LoadFromDocs(ILogger logger)
    {
        using JsonDocument? doc = SeedDataLoader.TryLoadJson("Disciplines.json", logger);
        if (doc == null)
        {
            return GetAll();
        }

        var result = new List<Discipline>();
        foreach (JsonElement el in doc.RootElement.EnumerateArray())
        {
            string name = el.TryGetProperty("name", out var n) ? n.GetString() ?? string.Empty : string.Empty;
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var discipline = new Discipline
            {
                Name = name,
                Description = el.TryGetProperty("description", out var d) ? d.GetString() ?? string.Empty : string.Empty,
                CanLearnIndependently = ReadBool(el, "canLearnIndependently"),
                RequiresMentorBloodToLearn = ReadBool(el, "requiresMentorBloodToLearn"),
                IsCovenantDiscipline = ReadBool(el, "isCovenantDiscipline"),
                IsBloodlineDiscipline = ReadBool(el, "isBloodlineDiscipline"),
                IsNecromancy = ReadBool(el, "isNecromancy"),
                Powers = [],
            };

            if (el.TryGetProperty("powers", out var powers))
            {
                int rank = 0;
                foreach (JsonElement p in powers.EnumerateArray())
                {
                    rank++;
                    string powerName = p.TryGetProperty("name", out var pn) ? pn.GetString() ?? $"{name} {rank}" : $"{name} {rank}";
                    int level = ReadPowerRanking(p, rank);
                    string pool = p.TryGetProperty("roll", out var pr) ? pr.GetString() ?? string.Empty : string.Empty;
                    string cost = p.TryGetProperty("cost", out var pc) ? pc.GetString() ?? "—" : "—";
                    string? poolJson = p.TryGetProperty("poolDefinitionJson", out var pj) && pj.ValueKind != JsonValueKind.Null
                        ? pj.GetString()
                        : null;

                    discipline.Powers.Add(new DisciplinePower
                    {
                        Level = level,
                        Name = powerName,
                        Description = p.TryGetProperty("description", out var pd) ? pd.GetString() ?? string.Empty : string.Empty,
                        DicePool = pool,
                        Cost = cost,
                        PoolDefinitionJson = poolJson,
                    });
                }
            }

            result.Add(discipline);
        }

        return result.Count > 0 ? result : GetAll();
    }

    private static bool ReadBool(JsonElement el, string propertyName) =>
        el.TryGetProperty(propertyName, out var p) && p.ValueKind == JsonValueKind.True;

    private static int ReadPowerRanking(JsonElement p, int fallbackRank)
    {
        if (!p.TryGetProperty("ranking", out var rv))
        {
            return fallbackRank;
        }

        if (rv.TryGetInt32(out int ri))
        {
            return ri;
        }

        if (rv.ValueKind == JsonValueKind.String && int.TryParse(rv.GetString(), out int rs))
        {
            return rs;
        }

        return fallbackRank;
    }

    private static Discipline CreateAnimalism()
    {
        var d = new Discipline { Name = "Animalism", Description = "Dominion over beasts and the feral nature." };
        d.Powers.Add(new DisciplinePower { Name = "Feral Whispers", Level = 1, Description = "Speak with and command animals.", DicePool = "Manipulation + Animal Ken + Animalism" });
        d.Powers.Add(new DisciplinePower { Name = "Raise the Familiar", Level = 2, Description = "Turn dead animal into a loyal proto-vampire.", Cost = _costOneVitae });
        d.Powers.Add(new DisciplinePower { Name = "Summon the Hunt", Level = 3, Description = "Call animals to a location or target with spilled blood.", DicePool = "Presence + Animal Ken + Animalism" });
        d.Powers.Add(new DisciplinePower { Name = "Feral Infection", Level = 4, Description = "Drives animals, humans and vampires into a frenzy.", DicePool = "Presence + Intimidation + Animalism" });
        d.Powers.Add(new DisciplinePower { Name = "Lord of the Land", Level = 5, Description = "Mark territory as own, intruders take penalties.", Cost = _costOneWillpower });
        return d;
    }

    private static Discipline CreateAuspex()
    {
        var d = new Discipline { Name = "Auspex", Description = "Preternatural perception and psychic awareness." };
        d.Powers.Add(new DisciplinePower { Name = "Beast's Senses", Level = 1, Description = "Heighten senses to supernatural levels." });
        d.Powers.Add(new DisciplinePower { Name = "Aura Perception", Level = 2, Description = "Read the emotional resonance and nature of a subject.", DicePool = "Wits + Empathy + Auspex" });
        d.Powers.Add(new DisciplinePower { Name = "The Spirit's Touch", Level = 3, Description = "Read psychic residue from objects.", DicePool = "Wits + Occult + Auspex" });
        d.Powers.Add(new DisciplinePower { Name = "Lay Open the Mind", Level = 4, Description = "Read the surface thoughts of a target.", DicePool = "Intelligence + Investigation + Auspex vs Resolve + Blood Potency" });
        d.Powers.Add(new DisciplinePower { Name = "Twilight Projection", Level = 5, Description = "Project awareness out of the body in twilight state.", Cost = _costOneWillpower });
        return d;
    }

    private static Discipline CreateCelerity()
    {
        var d = new Discipline { Name = "Celerity", Description = "Supernatural speed and reflexes." };
        d.Powers.Add(new DisciplinePower { Name = "Between the Ticks", Level = 1, Description = "Add dots to Initiative, subtract from attack pools.", Cost = _costOneVitae });
        d.Powers.Add(new DisciplinePower { Name = "Impulses", Level = 2, Description = "Can take a reflexive dash action.", Cost = _costOneVitae });
        d.Powers.Add(new DisciplinePower { Name = "Quick March", Level = 3, Description = "Subtract from all incoming attack pools.", Cost = _costOneVitae });
        d.Powers.Add(new DisciplinePower { Name = "Downgrading Strikes", Level = 4, Description = "Can ignore minor environmental hazards.", Cost = _costOneVitae });
        d.Powers.Add(new DisciplinePower { Name = "Out of Time", Level = 5, Description = "Take two actions instead of one.", Cost = _costOneVitae });
        return d;
    }

    private static Discipline CreateDominate()
    {
        var d = new Discipline { Name = "Dominate", Description = "Crushing mental control over others." };
        d.Powers.Add(new DisciplinePower { Name = "Mesmerize", Level = 1, Description = "Plant a single command in a hypnotic trance.", DicePool = "Intelligence + Expression + Dominate vs Resolve + Blood Potency" });
        d.Powers.Add(new DisciplinePower { Name = "Command", Level = 2, Description = "Give a simple, immediate order.", DicePool = "Manipulation + Intimidation + Dominate vs Resolve + Blood Potency" });
        d.Powers.Add(new DisciplinePower { Name = "The Forgetful Mind", Level = 3, Description = "Alter or erase recent memories.", DicePool = "Wits + Subterfuge + Dominate vs Resolve + Blood Potency" });
        d.Powers.Add(new DisciplinePower { Name = "Conditioning", Level = 4, Description = "Long-term programming and susceptibility.", Cost = _costOneWillpower });
        d.Powers.Add(new DisciplinePower { Name = "Possession", Level = 5, Description = "Take direct control of a mortal's body.", Cost = _costOneWillpower, DicePool = "Intelligence + Intimidation + Dominate vs Resolve + Blood Potency" });
        return d;
    }

    private static Discipline CreateMajesty()
    {
        var d = new Discipline { Name = "Majesty", Description = "Supernatural allure and emotional manipulation." };
        d.Powers.Add(new DisciplinePower { Name = "Awe", Level = 1, Description = "Draw all eyes and fascinate onlookers.", DicePool = "Presence + Expression + Majesty vs Composure + Blood Potency" });
        d.Powers.Add(new DisciplinePower { Name = "Confidant", Level = 2, Description = "Make a target view you as an intimately trusted friend.", DicePool = "Manipulation + Empathy + Majesty vs Composure + Blood Potency" });
        d.Powers.Add(new DisciplinePower { Name = "Entrancement", Level = 3, Description = "Inspire terrifying devotion.", DicePool = "Manipulation + Persuasion + Majesty vs Composure + Blood Potency" });
        d.Powers.Add(new DisciplinePower { Name = "Summoning", Level = 4, Description = "Call a person to your side.", DicePool = "Presence + Persuasion + Majesty vs Composure + Blood Potency" });
        d.Powers.Add(new DisciplinePower { Name = "Sovereignty", Level = 5, Description = "Paralyze others with submissive fear and worship.", DicePool = "Presence + Intimidation + Majesty vs Composure + Blood Potency" });
        return d;
    }

    private static Discipline CreateNightmare()
    {
        var d = new Discipline { Name = "Nightmare", Description = "Weaponized terror." };
        d.Powers.Add(new DisciplinePower { Name = "Dread", Level = 1, Description = "Elicit creeping paranoia and fear.", DicePool = "Presence + Empathy + Nightmare vs Composure + Blood Potency" });
        d.Powers.Add(new DisciplinePower { Name = "Face of the Beast", Level = 2, Description = "Reveal a monstrous visage.", Cost = _costOneVitae });
        d.Powers.Add(new DisciplinePower { Name = "Aura of Terror", Level = 3, Description = "Radiate overwhelming fear.", Cost = _costOneVitae, DicePool = "Presence + Intimidation + Nightmare vs Composure + Blood Potency" });
        d.Powers.Add(new DisciplinePower { Name = "Waking Nightmare", Level = 4, Description = "Force a target's worst fears to hallucinate.", Cost = _costOneWillpower });
        d.Powers.Add(new DisciplinePower { Name = "Mortal Fear", Level = 5, Description = "A terrifying shock to the system.", Cost = _costOneVitae, DicePool = "Presence + Intimidation + Nightmare vs Composure + Blood Potency" });
        return d;
    }

    private static Discipline CreateObfuscate()
    {
        var d = new Discipline { Name = "Obfuscate", Description = "The power to remain unseen and ignored." };
        d.Powers.Add(new DisciplinePower { Name = "Touch of Shadow", Level = 1, Description = "Hide an object on your person.", DicePool = "Wits + Larceny + Obfuscate" });
        d.Powers.Add(new DisciplinePower { Name = "Mask of Tranquility", Level = 2, Description = "Hide your Predatory Aura and emotions.", Cost = _costOneVitae });
        d.Powers.Add(new DisciplinePower { Name = "Cloak of Night", Level = 3, Description = "Turn utterly invisible.", Cost = _costOneVitae, DicePool = "Intelligence + Stealth + Obfuscate" });
        d.Powers.Add(new DisciplinePower { Name = "The Familiar Stranger", Level = 4, Description = "Disguise yourself as whoever the target expects.", Cost = _costOneVitae });
        d.Powers.Add(new DisciplinePower { Name = "Oubliette", Level = 5, Description = "Erase a person or place from perception entirely.", Cost = _costOneWillpower });
        return d;
    }

    private static Discipline CreateProtean()
    {
        var d = new Discipline { Name = "Protean", Description = "Shape-shifting and bestial adaptation." };
        d.Powers.Add(new DisciplinePower { Name = "Unnatural Aspect", Level = 1, Description = "Gain feral eyes, or retractible claws.", Cost = _costOneVitae });
        d.Powers.Add(new DisciplinePower { Name = "Haven of Soil", Level = 2, Description = "Meld into the earth to rest safely.", Cost = _costOneVitae });
        d.Powers.Add(new DisciplinePower { Name = "Beast's Skin", Level = 3, Description = "Transform into a predatory animal (wolf, bat).", Cost = _costOneVitae });
        d.Powers.Add(new DisciplinePower { Name = "Shape of the Beast", Level = 4, Description = "Transform into a swarm or combat-beast.", Cost = _costOneVitae });
        d.Powers.Add(new DisciplinePower { Name = "Primeval Miasma", Level = 5, Description = "Transform into mist.", Cost = _costOneVitae });
        return d;
    }

    private static Discipline CreateResilience()
    {
        var d = new Discipline { Name = "Resilience", Description = "Supernatural toughness." };
        d.Powers.Add(new DisciplinePower { Name = "Tough as Nails", Level = 1, Description = "Add to Stamina and downgrade agg damage.", Cost = _costOneVitae });
        d.Powers.Add(new DisciplinePower { Name = "General Sturdiness", Level = 2, Description = "Ignore wound penalties temporarily.", Cost = _costOneVitae });
        d.Powers.Add(new DisciplinePower { Name = "Bending and Breaking", Level = 3, Description = "Downgrade lethal damage to bashing.", Cost = _costOneVitae });
        d.Powers.Add(new DisciplinePower { Name = "The Fire Dies", Level = 4, Description = "Resist all mundane sources of damage.", Cost = _costOneVitae });
        d.Powers.Add(new DisciplinePower { Name = "Unbreakable", Level = 5, Description = "Shrug off almost anything.", Cost = _costOneWillpower });
        return d;
    }

    private static Discipline CreateVigor()
    {
        var d = new Discipline { Name = "Vigor", Description = "Supernatural strength." };
        d.Powers.Add(new DisciplinePower { Name = "Deadweight", Level = 1, Description = "Add dots to Strength and jumping.", Cost = _costOneVitae });
        d.Powers.Add(new DisciplinePower { Name = "Feat of Strength", Level = 2, Description = "Increase carrying capacity massively.", Cost = _costOneVitae });
        d.Powers.Add(new DisciplinePower { Name = "Punch Through", Level = 3, Description = "Break down doors easily.", Cost = _costOneVitae });
        d.Powers.Add(new DisciplinePower { Name = "Great Leap", Level = 4, Description = "Leap incredible distances.", Cost = _costOneVitae });
        d.Powers.Add(new DisciplinePower { Name = "Crushing Blow", Level = 5, Description = "Strike with earth-shattering force.", Cost = _costOneVitae });
        return d;
    }

    private static Discipline CreateCrúac()
    {
        return new Discipline
        {
            Name = "Crúac",
            Description = "Blood rites of the Circle of the Crone. Rituals draw on pagan power.",
        };
    }

    private static Discipline CreateThebanSorcery()
    {
        return new Discipline
        {
            Name = "Theban Sorcery",
            Description = "Sacraments of the Lancea et Sanctum. Miracles channel divine condemnation.",
        };
    }

    private static Discipline CreateNecromancy()
    {
        var d = new Discipline { Name = "Necromancy", Description = "Death sorcery of the Mekhet and the death-touched bloodlines." };
        d.Powers.Add(new DisciplinePower { Name = "Death Sight", Level = 1, Description = "Perceive ghosts and deathly resonance.", DicePool = "Wits + Occult + Necromancy" });
        d.Powers.Add(new DisciplinePower { Name = "Summon Shade", Level = 2, Description = "Call a minor ghost to answer questions.", Cost = _costOneVitae });
        d.Powers.Add(new DisciplinePower { Name = "Rotten Shroud", Level = 3, Description = "Curse a subject with decay and ill fortune.", Cost = _costOneVitae });
        d.Powers.Add(new DisciplinePower { Name = "Blighted Grasp", Level = 4, Description = "Inflict lethal harm through spectral touch.", Cost = _costOneVitae });
        d.Powers.Add(new DisciplinePower { Name = "Legion", Level = 5, Description = "Command a host of shades.", Cost = _costOneWillpower });
        return d;
    }
}
