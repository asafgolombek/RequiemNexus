using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.SeedData;

/// <summary>
/// Contains clan definitions and clan-discipline mappings for database seeding.
/// </summary>
public static class ClanSeedData
{
    private const string DaevaName = "Daeva";
    private const string GangrelName = "Gangrel";
    private const string MekhetName = "Mekhet";
    private const string NosferatuName = "Nosferatu";
    private const string VentrueName = "Ventrue";

    public static List<Clan> GetAllClans() =>
    [
        new() { Name = DaevaName, Description = "The succubi, masters of passion and manipulation." },
        new() { Name = GangrelName, Description = "The savages, predators closer to the Beast than man." },
        new() { Name = MekhetName, Description = "The shadows, secretive keepers of occult knowledge." },
        new() { Name = NosferatuName, Description = "The haunts, terrifying monsters twisted by the Curse." },
        new() { Name = VentrueName, Description = "The lords, aristocratic tyrants who demand fealty." }
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
            new() { ClanId = Clan(DaevaName).Id, DisciplineId = Disc("Celerity").Id },
            new() { ClanId = Clan(DaevaName).Id, DisciplineId = Disc("Majesty").Id },
            new() { ClanId = Clan(DaevaName).Id, DisciplineId = Disc("Vigor").Id },

            // Gangrel: Animalism, Protean, Resilience
            new() { ClanId = Clan(GangrelName).Id, DisciplineId = Disc("Animalism").Id },
            new() { ClanId = Clan(GangrelName).Id, DisciplineId = Disc("Protean").Id },
            new() { ClanId = Clan(GangrelName).Id, DisciplineId = Disc("Resilience").Id },

            // Mekhet: Auspex, Celerity, Obfuscate
            new() { ClanId = Clan(MekhetName).Id, DisciplineId = Disc("Auspex").Id },
            new() { ClanId = Clan(MekhetName).Id, DisciplineId = Disc("Celerity").Id },
            new() { ClanId = Clan(MekhetName).Id, DisciplineId = Disc("Obfuscate").Id },

            // Nosferatu: Nightmare, Obfuscate, Vigor
            new() { ClanId = Clan(NosferatuName).Id, DisciplineId = Disc("Nightmare").Id },
            new() { ClanId = Clan(NosferatuName).Id, DisciplineId = Disc("Obfuscate").Id },
            new() { ClanId = Clan(NosferatuName).Id, DisciplineId = Disc("Vigor").Id },

            // Ventrue: Animalism, Dominate, Resilience
            new() { ClanId = Clan(VentrueName).Id, DisciplineId = Disc("Animalism").Id },
            new() { ClanId = Clan(VentrueName).Id, DisciplineId = Disc("Dominate").Id },
            new() { ClanId = Clan(VentrueName).Id, DisciplineId = Disc("Resilience").Id }
        ];
    }
}
