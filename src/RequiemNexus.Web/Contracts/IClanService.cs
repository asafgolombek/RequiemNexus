using RequiemNexus.Data.Models;

namespace RequiemNexus.Web.Contracts;

public interface IClanService
{
    Task<List<Clan>> GetAllClansAsync();
}
