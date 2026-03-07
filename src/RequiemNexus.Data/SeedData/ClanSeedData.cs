using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.SeedData;

/// <summary>
/// Contains clan definitions and clan-discipline mappings for database seeding.
/// </summary>
public static class ClanSeedData
{
    private const string _daevaName = "Daeva";
    private const string _gangrelName = "Gangrel";
    private const string _mekhetName = "Mekhet";
    private const string _nosferatuName = "Nosferatu";
    private const string _ventrueName = "Ventrue";

    public static List<Clan> GetAllClans() =>
    [
        new() { Name = _daevaName, Description = "The succubi, masters of passion and manipulation." },
        new() { Name = _gangrelName, Description = "The savages, predators closer to the Beast than man." },
        new() { Name = _mekhetName, Description = "The shadows, secretive keepers of occult knowledge." },
        new() { Name = _nosferatuName, Description = "The haunts, terrifying monsters twisted by the Curse." },
        new() { Name = _ventrueName, Description = "The lords, aristocratic tyrants who demand fealty." }
    ];

    /// <summary>
    /// Maps each clan to its three in-clan disciplines.
    /// Requires the clans and disciplines to already be saved (so they have IDs).
    /// </summary>
    public static List<ClanDiscipline> GetClanDisciplineMappings(List<Clan> clans, List<Discipline> disciplines)
    {
        Clan Clan(string name) => clans.First(c => c.Name == name);
        Discipline Disc(string name) => disciplines.First(d => d.Name == name);

        return
        [

            // Daeva: Celerity, Majesty, Vigor
            new() { ClanId = Clan(_daevaName).Id, DisciplineId = Disc("Celerity").Id },
            new() { ClanId = Clan(_daevaName).Id, DisciplineId = Disc("Majesty").Id },
            new() { ClanId = Clan(_daevaName).Id, DisciplineId = Disc("Vigor").Id },

            // Gangrel: Animalism, Protean, Resilience
            new() { ClanId = Clan(_gangrelName).Id, DisciplineId = Disc("Animalism").Id },
            new() { ClanId = Clan(_gangrelName).Id, DisciplineId = Disc("Protean").Id },
            new() { ClanId = Clan(_gangrelName).Id, DisciplineId = Disc("Resilience").Id },

            // Mekhet: Auspex, Celerity, Obfuscate
            new() { ClanId = Clan(_mekhetName).Id, DisciplineId = Disc("Auspex").Id },
            new() { ClanId = Clan(_mekhetName).Id, DisciplineId = Disc("Celerity").Id },
            new() { ClanId = Clan(_mekhetName).Id, DisciplineId = Disc("Obfuscate").Id },

            // Nosferatu: Nightmare, Obfuscate, Vigor
            new() { ClanId = Clan(_nosferatuName).Id, DisciplineId = Disc("Nightmare").Id },
            new() { ClanId = Clan(_nosferatuName).Id, DisciplineId = Disc("Obfuscate").Id },
            new() { ClanId = Clan(_nosferatuName).Id, DisciplineId = Disc("Vigor").Id },

            // Ventrue: Animalism, Dominate, Resilience
            new() { ClanId = Clan(_ventrueName).Id, DisciplineId = Disc("Animalism").Id },
            new() { ClanId = Clan(_ventrueName).Id, DisciplineId = Disc("Dominate").Id },
            new() { ClanId = Clan(_ventrueName).Id, DisciplineId = Disc("Resilience").Id }
        ];
    }
}
