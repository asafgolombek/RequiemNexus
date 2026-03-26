using Microsoft.Extensions.DependencyInjection;

namespace RequiemNexus.Application.Events;

/// <summary>
/// Resolves <see cref="IDomainEventHandler{TEvent}"/> from the scoped <see cref="IServiceProvider"/> and invokes them in order.
/// </summary>
public sealed class DomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    /// <inheritdoc />
    public void Dispatch<TEvent>(TEvent domainEvent)
        where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        foreach (IDomainEventHandler<TEvent> handler in _serviceProvider.GetServices<IDomainEventHandler<TEvent>>())
        {
            handler.Handle(domainEvent);
        }
    }
}
