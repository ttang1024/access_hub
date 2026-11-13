using AccessHub.BuildingBlocks.Application;
using AccessHub.Cardholder.Domain.AccessGroups;
using AccessHub.Contracts.IntegrationEvents.Cardholder;

namespace AccessHub.Cardholder.Application.IntegrationEvents;

public sealed class AccessGroupEventsPublisher(IIntegrationEventPublisher publisher) : IDomainEventHandler<AccessGroupChangedDomainEvent>
{
    public Task HandleAsync(AccessGroupChangedDomainEvent e, CancellationToken cancellationToken) =>
        publisher.PublishAsync(
            new AccessGroupChanged(
                Guid.CreateVersion7(), e.OccurredAt, Correlation.Current(), e.AccessGroupId.Value, e.Name, e.IsDeleted,
                e.Doors.Select(d => new AccessGroupDoor(
                    d.DoorId.Value,
                    d.SiteId.Value,
                    d.Schedule.Windows.Select(w => new WeeklyTimeWindow(w.Day, w.StartMinute, w.EndMinute)).ToList())).ToList(),
                e.Version),
            cancellationToken);
}
