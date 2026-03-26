namespace RequiemNexus.Application.Events;

/// <summary>
/// Dispatches in-process domain events to registered handlers in the current DI scope (synchronous).
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>
    /// Invokes all <see cref="IDomainEventHandler{TEvent}"/> implementations for <typeparamref name="TEvent"/> in registration order.
    /// </summary>
    void Dispatch<TEvent>(TEvent domainEvent)
        where TEvent : class;
}
