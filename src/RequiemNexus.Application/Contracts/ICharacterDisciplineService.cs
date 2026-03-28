using RequiemNexus.Application.DTOs;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Models;

namespace RequiemNexus.Application.Contracts;

/// <summary>
/// Manages discipline purchases for characters, including XP deduction and ledger recording.
/// </summary>
public interface ICharacterDisciplineService
{
    /// <summary>Returns all disciplines available in the catalogue, ordered by name.</summary>
    Task<List<Discipline>> GetAvailableDisciplinesAsync();

    /// <summary>Adds a new Discipline at the given rating, enforcing all acquisition gates.</summary>
    /// <param name="request">Purchase parameters.</param>
    /// <param name="userId">Authenticated user (owner or Storyteller).</param>
    Task<Result<CharacterDiscipline>> AddDisciplineAsync(DisciplineAcquisitionRequest request, string userId);

    /// <summary>Upgrades an existing Discipline to the target rating, enforcing all acquisition gates.</summary>
    /// <param name="request">Must reference an existing character Discipline via <see cref="DisciplineAcquisitionRequest.DisciplineId"/>.</param>
    /// <param name="userId">Authenticated user (owner or Storyteller).</param>
    Task<Result<CharacterDiscipline>> TryUpgradeDisciplineAsync(DisciplineAcquisitionRequest request, string userId);
}
