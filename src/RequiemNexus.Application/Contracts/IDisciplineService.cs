using RequiemNexus.Data.Models;

namespace RequiemNexus.Application.Contracts;

public interface IDisciplineService
{
    Task<List<Discipline>> GetAllDisciplinesAsync();
}
