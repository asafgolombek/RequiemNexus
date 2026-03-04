using RequiemNexus.Data.Models;

namespace RequiemNexus.Web.Contracts;

public interface IMeritService
{
    Task<List<Merit>> GetAllMeritsAsync();
}
