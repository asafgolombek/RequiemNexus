using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Contracts;

public interface IClanService
{
    Task<List<Clan>> GetAllClansAsync();
}
