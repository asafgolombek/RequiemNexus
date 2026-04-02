using Microsoft.EntityFrameworkCore;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Services;

public class ClanService(ApplicationDbContext dbContext, IReferenceDataCache referenceData) : IClanService
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly IReferenceDataCache _referenceData = referenceData;

    public async Task<List<Clan>> GetAllClansAsync()
    {
        List<Clan> homebrew = await _dbContext.Clans
            .AsNoTracking()
            .Where(c => c.IsHomebrew)
            .Include(c => c.ClanDisciplines)
            .OrderBy(c => c.Name)
            .ToListAsync();

        return _referenceData.ReferenceClans
            .Concat(homebrew)
            .OrderBy(c => c.Name)
            .ToList();
    }
}
