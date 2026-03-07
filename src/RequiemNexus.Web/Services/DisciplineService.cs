using Microsoft.EntityFrameworkCore;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Web.Contracts;

namespace RequiemNexus.Web.Services;

public class DisciplineService(ApplicationDbContext dbContext) : IDisciplineService
{
    private readonly ApplicationDbContext dbContext = dbContext;

    public async Task<List<Discipline>> GetAllDisciplinesAsync()
    {
        return await dbContext.Disciplines
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .ToListAsync();
    }
}
