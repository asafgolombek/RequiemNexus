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
    ];

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
        d.Powers.Add(new DisciplinePower { Name = "Celerity 1", Level = 1, Description = "Add dots to Initiative, subtract from attack pools.", Cost = _costOneVitae });
        d.Powers.Add(new DisciplinePower { Name = "Celerity 2", Level = 2, Description = "Can take a reflexive dash action.", Cost = _costOneVitae });
        d.Powers.Add(new DisciplinePower { Name = "Celerity 3", Level = 3, Description = "Subtract from all incoming attack pools.", Cost = _costOneVitae });
        d.Powers.Add(new DisciplinePower { Name = "Celerity 4", Level = 4, Description = "Can ignore minor environmental hazards.", Cost = _costOneVitae });
        d.Powers.Add(new DisciplinePower { Name = "Celerity 5", Level = 5, Description = "Take two actions instead of one.", Cost = _costOneVitae });
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
        d.Powers.Add(new DisciplinePower { Name = "Resilience 1", Level = 1, Description = "Add to Stamina and downgrade agg damage.", Cost = _costOneVitae });
        d.Powers.Add(new DisciplinePower { Name = "Resilience 2", Level = 2, Description = "Ignore wound penalties temporarily.", Cost = _costOneVitae });
        d.Powers.Add(new DisciplinePower { Name = "Resilience 3", Level = 3, Description = "Downgrade lethal damage to bashing.", Cost = _costOneVitae });
        d.Powers.Add(new DisciplinePower { Name = "Resilience 4", Level = 4, Description = "Resist all mundane sources of damage.", Cost = _costOneVitae });
        d.Powers.Add(new DisciplinePower { Name = "Resilience 5", Level = 5, Description = "Shrug off almost anything.", Cost = _costOneWillpower });
        return d;
    }

    private static Discipline CreateVigor()
    {
        var d = new Discipline { Name = "Vigor", Description = "Supernatural strength." };
        d.Powers.Add(new DisciplinePower { Name = "Vigor 1", Level = 1, Description = "Add dots to Strength and jumping.", Cost = _costOneVitae });
        d.Powers.Add(new DisciplinePower { Name = "Vigor 2", Level = 2, Description = "Increase carrying capacity massively.", Cost = _costOneVitae });
        d.Powers.Add(new DisciplinePower { Name = "Vigor 3", Level = 3, Description = "Break down doors easily.", Cost = _costOneVitae });
        d.Powers.Add(new DisciplinePower { Name = "Vigor 4", Level = 4, Description = "Leap incredible distances.", Cost = _costOneVitae });
        d.Powers.Add(new DisciplinePower { Name = "Vigor 5", Level = 5, Description = "Strike with earth-shattering force.", Cost = _costOneVitae });
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
}
