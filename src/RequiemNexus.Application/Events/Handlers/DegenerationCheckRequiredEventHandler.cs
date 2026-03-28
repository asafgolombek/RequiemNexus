using Microsoft.Extensions.Logging;
using RequiemNexus.Domain.Events;

namespace RequiemNexus.Application.Events.Handlers;

/// <summary>
/// Phase 19: logs degeneration check requirements. Phase 17 will add ST notification and roll UI.
/// </summary>
public sealed class DegenerationCheckRequiredEventHandler(
    ILogger<DegenerationCheckRequiredEventHandler> logger)
    : IDomainEventHandler<DegenerationCheckRequiredEvent>
{
    /// <inheritdoc />
    public void Handle(DegenerationCheckRequiredEvent domainEvent)
    {
        logger.LogInformation(
            "Degeneration check required for Character {CharacterId}. Reason: {Reason}",
            domainEvent.CharacterId,
            domainEvent.Reason);
    }
}
