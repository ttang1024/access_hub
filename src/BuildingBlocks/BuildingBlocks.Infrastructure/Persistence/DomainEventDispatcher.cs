using System.Collections.Concurrent;
using AccessHub.BuildingBlocks.Application;
using AccessHub.BuildingBlocks.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace AccessHub.BuildingBlocks.Infrastructure.Persistence;

/// <summary>Resolves domain event handlers from the current DI scope.</summary>
public sealed class DomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    private static readonly ConcurrentDictionary<Type, Func<object, IDomainEvent, CancellationToken, Task>> Invokers = new();

    /// <inheritdoc />
    public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var eventType = domainEvent.GetType();
        var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
        var invoke = Invokers.GetOrAdd(eventType, static t => BuildInvoker(t));
        foreach (var handler in serviceProvider.GetServices(handlerType))
        {
            await invoke(handler!, domainEvent, cancellationToken);
        }
    }

    private static Func<object, IDomainEvent, CancellationToken, Task> BuildInvoker(Type eventType)
    {
        var method = typeof(DomainEventDispatcher)
            .GetMethod(nameof(InvokeAsync), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .MakeGenericMethod(eventType);
        return (Func<object, IDomainEvent, CancellationToken, Task>)Delegate.CreateDelegate(typeof(Func<object, IDomainEvent, CancellationToken, Task>), method);
    }

    private static Task InvokeAsync<TEvent>(object handler, IDomainEvent domainEvent, CancellationToken cancellationToken)
        where TEvent : IDomainEvent =>
        ((IDomainEventHandler<TEvent>)handler).HandleAsync((TEvent)domainEvent, cancellationToken);
}
