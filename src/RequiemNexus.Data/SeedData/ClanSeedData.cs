using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.SeedData;

/// <summary>
/// Contains clan definitions and clan-discipline mappings for database seeding.
/// </summary>
public static class ClanSeedData
{
    public static List<Clan> GetAllClans() =>
    [
        new() { Name = "Daeva", Description = "The succubi, masters of passion and manipulation." },
        new() { Name = "Gangrel", Description = "The savages, predators closer to the Beast than man." },
        new() { Name = "Mekhet", Description = "The shadows, secretive keepers of occult knowledge." },
        new() { Name = "Nosferatu", Description = "The haunts, terrifying monsters twisted by the Curse." },
        new() { Name = "Ventrue", Description = "The lords, aristocratic tyrants who demand fealty." }
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
            new() { ClanId = Clan("Daeva").Id, DisciplineId = Disc("Celerity").Id },
            new() { ClanId = Clan("Daeva").Id, DisciplineId = Disc("Majesty").Id },
            new() { ClanId = Clan("Daeva").Id, DisciplineId = Disc("Vigor").Id },

            // Gangrel: Animalism, Protean, Resilience
            new() { ClanId = Clan("Gangrel").Id, DisciplineId = Disc("Animalism").Id },
            new() { ClanId = Clan("Gangrel").Id, DisciplineId = Disc("Protean").Id },
            new() { ClanId = Clan("Gangrel").Id, DisciplineId = Disc("Resilience").Id },

            // Mekhet: Auspex, Celerity, Obfuscate
            new() { ClanId = Clan("Mekhet").Id, DisciplineId = Disc("Auspex").Id },
            new() { ClanId = Clan("Mekhet").Id, DisciplineId = Disc("Celerity").Id },
            new() { ClanId = Clan("Mekhet").Id, DisciplineId = Disc("Obfuscate").Id },

            // Nosferatu: Nightmare, Obfuscate, Vigor
            new() { ClanId = Clan("Nosferatu").Id, DisciplineId = Disc("Nightmare").Id },
            new() { ClanId = Clan("Nosferatu").Id, DisciplineId = Disc("Obfuscate").Id },
            new() { ClanId = Clan("Nosferatu").Id, DisciplineId = Disc("Vigor").Id },

            // Ventrue: Animalism, Dominate, Resilience
            new() { ClanId = Clan("Ventrue").Id, DisciplineId = Disc("Animalism").Id },
            new() { ClanId = Clan("Ventrue").Id, DisciplineId = Disc("Dominate").Id },
            new() { ClanId = Clan("Ventrue").Id, DisciplineId = Disc("Resilience").Id }
        ];
    }
}
