using Microsoft.EntityFrameworkCore;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Web.Contracts;

namespace RequiemNexus.Web.Services;

public class DisciplineService(ApplicationDbContext dbContext) : IDisciplineService
{
    private readonly ApplicationDbContext _dbContext = dbContext;

    public async Task<List<Discipline>> GetAllDisciplinesAsync()
    {
        return await _dbContext.Disciplines
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .ToListAsync();
    }
}
