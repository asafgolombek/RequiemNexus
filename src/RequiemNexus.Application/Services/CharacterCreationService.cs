using RequiemNexus.Application.Contracts;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Models;

namespace RequiemNexus.Application.Services;

/// <inheritdoc />
public sealed class CharacterCreationService : ICharacterCreationService
{
    /// <inheritdoc />
    public Result<bool> ValidateCreationDisciplines(Character character)
    {
        int totalDots = character.Disciplines.Sum(d => d.Rating);
        if (totalDots < 3)
        {
            return Result<bool>.Success(true);
        }

        int inClanDots = character.Disciplines
            .Where(d => character.IsDisciplineInClan(d.DisciplineId))
            .Sum(d => d.Rating);

        int outOfClanDots = totalDots - inClanDots;
        if (outOfClanDots > 1)
        {
            return Result<bool>.Failure(
                "At least 2 of your 3 starting Discipline dots must be in-clan Disciplines.");
        }

        return Result<bool>.Success(true);
    }
}
