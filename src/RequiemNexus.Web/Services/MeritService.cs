using Microsoft.EntityFrameworkCore;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Web.Contracts;

namespace RequiemNexus.Web.Services;

public class MeritService(ApplicationDbContext dbContext) : IMeritService
{
    private readonly ApplicationDbContext dbContext = dbContext;

    public async Task<List<Merit>> GetAllMeritsAsync()
    {
        return await dbContext.Merits
            .AsNoTracking()
            .OrderBy(m => m.Name)
            .ToListAsync();
    }
}
