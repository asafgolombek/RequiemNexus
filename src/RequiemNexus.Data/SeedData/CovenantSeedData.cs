using RequiemNexus.Data.Models;

namespace RequiemNexus.Data.SeedData;

/// <summary>
/// Seed data for covenant definitions. Maps from Covenants.json.
/// VII is seeded for worldbuilding but IsPlayable = false (block character join).
/// </summary>
public static class CovenantSeedData
{
    /// <summary>
    /// Creates covenant definitions. Five core covenants are playable; VII is not.
    /// </summary>
    public static List<CovenantDefinition> GetAllCovenants()
    {
        return
        [
            new CovenantDefinition
            {
                Name = "The Carthian Movement",
                Description = "Vampiric idealists applying modern mortal political systems and democracy to Kindred society.",
                IsPlayable = true,
            },
            new CovenantDefinition
            {
                Name = "The Circle of the Crone",
                Description = "A covenant of ritualistic Kindred who revere pagan gods, spirits, pantheons, and progenitors.",
                IsPlayable = true,
                SupportsBloodSorcery = true,
            },
            new CovenantDefinition
            {
                Name = "The Invictus",
                Description = "A covenant of vampires determined to protect the Masquerade and rule as elites.",
                IsPlayable = true,
            },
            new CovenantDefinition
            {
                Name = "The Lancea et Sanctum",
                Description = "The vampiric church believing Kindred are cursed to do God's dark work.",
                IsPlayable = true,
                SupportsBloodSorcery = true,
            },
            new CovenantDefinition
            {
                Name = "The Ordo Dracul",
                Description = "A covenant of vampires known for mystic studies and desire to transcend the curse.",
                IsPlayable = true,
            },
            new CovenantDefinition
            {
                Name = "VII",
                Description = "A mysterious group of vampires that detests Kindred and seeks to destroy them.",
                IsPlayable = false,
            },
        ];
    }
}
