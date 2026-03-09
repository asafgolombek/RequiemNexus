using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Contracts;

public interface IMeritService
{
    Task<List<Merit>> GetAllMeritsAsync();
}
