using Microsoft.EntityFrameworkCore;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.Events;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Events;

namespace RequiemNexus.Application.Services;

/// <inheritdoc />
public sealed class HumanityService(
    ApplicationDbContext dbContext,
    IAuthorizationHelper authorizationHelper,
    IDomainEventDispatcher domainEventDispatcher) : IHumanityService
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly IAuthorizationHelper _authorizationHelper = authorizationHelper;
    private readonly IDomainEventDispatcher _domainEventDispatcher = domainEventDispatcher;

    /// <inheritdoc />
    public int GetEffectiveMaxHumanity(Character character)
    {
        return 10 - character.GetDisciplineRating("Crúac");
    }

    /// <inheritdoc />
    public async Task EvaluateStainsAsync(int characterId, string userId)
    {
        await _authorizationHelper.RequireCharacterAccessAsync(characterId, userId, "evaluate Humanity stains");

        Character? character = await _dbContext.Characters
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == characterId);

        if (character == null)
        {
            throw new InvalidOperationException($"Character {characterId} not found.");
        }

        if (character.HumanityStains >= character.Humanity)
        {
            _domainEventDispatcher.Dispatch(
                new DegenerationCheckRequiredEvent(characterId, DegenerationReason.StainsThreshold));
        }
    }
}
