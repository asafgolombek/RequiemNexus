using Microsoft.EntityFrameworkCore;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Services;

public class ClanService(ApplicationDbContext dbContext) : IClanService
{
    private readonly ApplicationDbContext _dbContext = dbContext;

    public async Task<List<Clan>> GetAllClansAsync()
    {
        return await _dbContext.Clans
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync();
    }
}
