using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RequiemNexus.Application.RealTime;
using RequiemNexus.Data;
using RequiemNexus.Data.Models;
using RequiemNexus.Data.RealTime;
using RequiemNexus.Domain.Enums;
using RequiemNexus.Domain.Events;

namespace RequiemNexus.Application.Events.Handlers;

/// <summary>
/// Logs degeneration check requirements and pushes a chronicle SignalR patch so the Storyteller Glimpse can show a banner.
/// </summary>
public sealed class DegenerationCheckRequiredEventHandler(
    ApplicationDbContext dbContext,
    ISessionService sessionService,
    ILogger<DegenerationCheckRequiredEventHandler> logger)
    : IDomainEventHandler<DegenerationCheckRequiredEvent>
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly ISessionService _sessionService = sessionService;
    private readonly ILogger<DegenerationCheckRequiredEventHandler> _logger = logger;

    /// <inheritdoc />
    public void Handle(DegenerationCheckRequiredEvent domainEvent)
    {
        _logger.LogInformation(
            "Degeneration check required for Character {CharacterId}. Reason: {Reason}",
            domainEvent.CharacterId,
            domainEvent.Reason);

        try
        {
            PushChronicleAlertAsync(domainEvent).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to broadcast degeneration alert for Character {CharacterId}.",
                domainEvent.CharacterId);
        }
    }

    private async Task PushChronicleAlertAsync(DegenerationCheckRequiredEvent domainEvent)
    {
        Character? character = await _dbContext.Characters
            .AsNoTracking()
            .Include(c => c.Attributes)
            .FirstOrDefaultAsync(c => c.Id == domainEvent.CharacterId);

        if (character?.CampaignId is not int chronicleId)
        {
            return;
        }

        int resolve = character.GetAttributeRating(AttributeId.Resolve);
        var alert = new DegenerationCheckAlertDto(
            character.Id,
            character.Name,
            character.Humanity,
            resolve);

        await _sessionService.BroadcastChronicleUpdateAsync(
            new ChronicleUpdateDto(chronicleId, DegenerationCheckRequired: alert));
    }
}
