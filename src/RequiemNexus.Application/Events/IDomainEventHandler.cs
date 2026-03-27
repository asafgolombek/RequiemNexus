namespace RequiemNexus.Application.Events;

/// <summary>
/// Handles a domain event dispatched in-process on the same DI scope (and typically the same DbContext) as the publisher.
/// </summary>
/// <typeparam name="TEvent">The event type.</typeparam>
public interface IDomainEventHandler<in TEvent>
    where TEvent : class
{
    /// <summary>
    /// Handles the event synchronously.
    /// </summary>
    void Handle(TEvent domainEvent);
}
