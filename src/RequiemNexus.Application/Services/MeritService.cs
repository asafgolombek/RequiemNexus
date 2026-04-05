using Microsoft.EntityFrameworkCore;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Services;

public class MeritService(ApplicationDbContext dbContext, IReferenceDataCache referenceData) : IMeritService
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly IReferenceDataCache _referenceData = referenceData;

    public async Task<List<Merit>> GetAllMeritsAsync()
    {
        List<Merit> homebrew = await _dbContext.Merits
            .AsNoTracking()
            .Where(m => m.IsHomebrew)
            .OrderBy(m => m.Name)
            .ToListAsync();

        return _referenceData.ReferenceMerits
            .Concat(homebrew)
            .OrderBy(m => m.Name)
            .ToList();
    }
}
