using RequiemNexus.Data.Models;

namespace RequiemNexus.Web.Contracts;

public interface IDisciplineService
{
    Task<List<Discipline>> GetAllDisciplinesAsync();
}
