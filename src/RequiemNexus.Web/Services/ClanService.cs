using Microsoft.EntityFrameworkCore;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Web.Contracts;

namespace RequiemNexus.Web.Services;

public class ClanService(ApplicationDbContext dbContext) : IClanService
{
    private readonly ApplicationDbContext dbContext = dbContext;

    public async Task<List<Clan>> GetAllClansAsync()
    {
        return await dbContext.Clans
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync();
    }
}
