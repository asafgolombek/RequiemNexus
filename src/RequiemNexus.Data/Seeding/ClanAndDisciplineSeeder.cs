using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.SeedData;

namespace RequiemNexus.Data.Seeding;

/// <summary>
/// Seeds clans, disciplines, and clan–discipline mappings when the catalog is empty.
/// </summary>
public sealed class ClanAndDisciplineSeeder : ISeeder
{
    /// <inheritdoc />
    public int Order => 20;

    /// <inheritdoc />
    public async Task SeedAsync(ApplicationDbContext context, ILogger logger)
    {
        bool hasClansAndDisciplines = await context.Clans.AnyAsync() && await context.Disciplines.AnyAsync();

        if (!hasClansAndDisciplines)
        {
            List<Clan> clans = ClanSeedData.GetAllClans();
            await context.Clans.AddRangeAsync(clans);
            await context.SaveChangesAsync();

            List<Discipline> disciplinesList = DisciplineSeedData.LoadFromDocs(logger);
            await context.Disciplines.AddRangeAsync(disciplinesList);
            await context.SaveChangesAsync();

            List<ClanDiscipline> clanDisciplines = ClanSeedData.GetClanDisciplineMappings(clans, disciplinesList);
            await context.ClanDisciplines.AddRangeAsync(clanDisciplines);
            await context.SaveChangesAsync();
        }
    }
}
