using Microsoft.EntityFrameworkCore;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Services;

public class MeritService(ApplicationDbContext dbContext) : IMeritService
{
    private readonly ApplicationDbContext _dbContext = dbContext;

    public async Task<List<Merit>> GetAllMeritsAsync()
    {
        return await _dbContext.Merits
            .AsNoTracking()
            .OrderBy(m => m.Name)
            .ToListAsync();
    }
}
