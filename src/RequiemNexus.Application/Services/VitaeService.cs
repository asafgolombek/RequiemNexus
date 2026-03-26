using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Application.Events;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Events;
using RequiemNexus.Domain.Models;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Applies Vitae spends and gains with Masquerade checks and Vitae-depleted domain dispatch.
/// </summary>
public sealed class VitaeService(
    ApplicationDbContext dbContext,
    IAuthorizationHelper authorizationHelper,
    IDomainEventDispatcher domainEventDispatcher,
    ILogger<VitaeService> logger) : IVitaeService
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly IAuthorizationHelper _authorizationHelper = authorizationHelper;
    private readonly IDomainEventDispatcher _domainEventDispatcher = domainEventDispatcher;
    private readonly ILogger<VitaeService> _logger = logger;

    /// <inheritdoc />
    public async Task<Result<int>> SpendVitaeAsync(
        int characterId,
        string userId,
        int amount,
        string reason,
        CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
        {
            return Result<int>.Failure("Vitae spend amount must be positive.");
        }

        await _authorizationHelper.RequireCharacterAccessAsync(characterId, userId, "spend Vitae");

        Character? character = await _dbContext.Characters
            .FirstOrDefaultAsync(c => c.Id == characterId, cancellationToken);

        if (character is null)
        {
            return Result<int>.Failure("Character not found.");
        }

        if (character.CurrentVitae < amount)
        {
            return Result<int>.Failure("Not enough Vitae.");
        }

        character.CurrentVitae -= amount;
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (character.CurrentVitae == 0)
        {
            _domainEventDispatcher.Dispatch(new VitaeDepletedEvent(characterId));
        }

        _logger.LogInformation(
            "Character {CharacterId} spent {Amount} Vitae ({Reason}). Remaining: {Remaining}.",
            characterId,
            amount,
            LogSanitizer.ForLog(reason),
            character.CurrentVitae);

        return Result<int>.Success(character.CurrentVitae);
    }

    /// <inheritdoc />
    public async Task<Result<int>> GainVitaeAsync(
        int characterId,
        string userId,
        int amount,
        string reason,
        CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
        {
            return Result<int>.Failure("Vitae gain amount must be positive.");
        }

        await _authorizationHelper.RequireCharacterAccessAsync(characterId, userId, "gain Vitae");

        Character? character = await _dbContext.Characters
            .FirstOrDefaultAsync(c => c.Id == characterId, cancellationToken);

        if (character is null)
        {
            return Result<int>.Failure("Character not found.");
        }

        character.CurrentVitae = Math.Min(character.MaxVitae, character.CurrentVitae + amount);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Character {CharacterId} gained {Amount} Vitae ({Reason}). Current: {Current}.",
            characterId,
            amount,
            LogSanitizer.ForLog(reason),
            character.CurrentVitae);

        return Result<int>.Success(character.CurrentVitae);
    }
}
