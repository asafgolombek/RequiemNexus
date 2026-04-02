using Microsoft.EntityFrameworkCore;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Services;

public class DisciplineService(ApplicationDbContext dbContext, IReferenceDataCache referenceData) : IDisciplineService
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly IReferenceDataCache _referenceData = referenceData;

    public async Task<List<Discipline>> GetAllDisciplinesAsync()
    {
        List<Discipline> homebrew = await _dbContext.Disciplines
            .AsNoTracking()
            .Where(d => d.IsHomebrew)
            .Include(d => d.Covenant)
            .Include(d => d.Bloodline)
            .OrderBy(d => d.Name)
            .ToListAsync();

        return _referenceData.ReferenceDisciplines
            .Concat(homebrew)
            .OrderBy(d => d.Name)
            .ToList();
    }
}
