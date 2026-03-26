using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Domain.Models;

namespace RequiemNexus.Application.Services;

/// <summary>
/// Applies Willpower spends and recovery with Masquerade checks.
/// </summary>
public sealed class WillpowerService(
    ApplicationDbContext dbContext,
    IAuthorizationHelper authorizationHelper,
    ILogger<WillpowerService> logger) : IWillpowerService
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly IAuthorizationHelper _authorizationHelper = authorizationHelper;
    private readonly ILogger<WillpowerService> _logger = logger;

    /// <inheritdoc />
    public async Task<Result<int>> SpendWillpowerAsync(
        int characterId,
        string userId,
        int amount,
        CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
        {
            return Result<int>.Failure("Willpower spend amount must be positive.");
        }

        await _authorizationHelper.RequireCharacterAccessAsync(characterId, userId, "spend Willpower");

        Character? character = await _dbContext.Characters
            .FirstOrDefaultAsync(c => c.Id == characterId, cancellationToken);

        if (character is null)
        {
            return Result<int>.Failure("Character not found.");
        }

        if (character.CurrentWillpower < amount)
        {
            return Result<int>.Failure("Not enough Willpower.");
        }

        character.CurrentWillpower -= amount;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Character {CharacterId} spent {Amount} Willpower. Remaining: {Remaining}.",
            characterId,
            amount,
            character.CurrentWillpower);

        return Result<int>.Success(character.CurrentWillpower);
    }

    /// <inheritdoc />
    public async Task<Result<int>> RecoverWillpowerAsync(
        int characterId,
        string userId,
        int amount,
        CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
        {
            return Result<int>.Failure("Willpower recovery amount must be positive.");
        }

        await _authorizationHelper.RequireCharacterAccessAsync(characterId, userId, "recover Willpower");

        Character? character = await _dbContext.Characters
            .FirstOrDefaultAsync(c => c.Id == characterId, cancellationToken);

        if (character is null)
        {
            return Result<int>.Failure("Character not found.");
        }

        character.CurrentWillpower = Math.Min(character.MaxWillpower, character.CurrentWillpower + amount);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Character {CharacterId} recovered {Amount} Willpower. Current: {Current}.",
            characterId,
            amount,
            character.CurrentWillpower);

        return Result<int>.Success(character.CurrentWillpower);
    }
}
