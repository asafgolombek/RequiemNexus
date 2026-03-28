using Microsoft.EntityFrameworkCore;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Services;

public class DisciplineService(ApplicationDbContext dbContext) : IDisciplineService
{
    private readonly ApplicationDbContext _dbContext = dbContext;

    public async Task<List<Discipline>> GetAllDisciplinesAsync()
    {
        return await _dbContext.Disciplines
            .AsNoTracking()
            .Include(d => d.Covenant)
            .Include(d => d.Bloodline)
            .OrderBy(d => d.Name)
            .ToListAsync();
    }
}
