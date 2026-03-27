using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.Contracts;
using RequiemNexus.Data;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Events;

namespace RequiemNexus.Application.Events.Handlers;

/// <summary>
/// Triggers an automatic Hunger frenzy save when Vitae is depleted.
/// </summary>
public sealed class VitaeDepletedEventHandler(
    ApplicationDbContext dbContext,
    IFrenzyService frenzyService,
    ILogger<VitaeDepletedEventHandler> logger) : IDomainEventHandler<VitaeDepletedEvent>
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly IFrenzyService _frenzyService = frenzyService;
    private readonly ILogger<VitaeDepletedEventHandler> _logger = logger;

    /// <inheritdoc />
    public void Handle(VitaeDepletedEvent domainEvent)
    {
        string? ownerId = _dbContext.Characters
            .AsNoTracking()
            .Where(c => c.Id == domainEvent.CharacterId)
            .Select(c => c.ApplicationUserId)
            .FirstOrDefault();

        if (string.IsNullOrEmpty(ownerId))
        {
            _logger.LogWarning(
                "VitaeDepletedEvent for character {CharacterId}: no owner id; skipping Hunger frenzy save.",
                domainEvent.CharacterId);
            return;
        }

        try
        {
            var result = _frenzyService
                .RollFrenzySaveAsync(
                    domainEvent.CharacterId,
                    ownerId,
                    FrenzyTrigger.Hunger,
                    spendWillpower: false,
                    CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            if (!result.IsSuccess)
            {
                _logger.LogWarning(
                    "Hunger frenzy save returned failure for character {CharacterId}: {Error}",
                    domainEvent.CharacterId,
                    result.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Hunger frenzy save threw after Vitae depletion for character {CharacterId}.",
                domainEvent.CharacterId);
        }
    }
}
