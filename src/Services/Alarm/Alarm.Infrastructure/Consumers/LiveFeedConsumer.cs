using AccessHub.Alarm.Application.Abstractions;
using AccessHub.Contracts.IntegrationEvents.EventIngestion;
using MassTransit;

namespace AccessHub.Alarm.Infrastructure.Consumers;

/// <summary>Pushes every recorded site event to connected dashboards (its own queue, independent of the rules).</summary>
public sealed class LiveFeedConsumer(IDashboardNotifier notifier) : IConsumer<DoorEventsRecorded>
{
    public async Task Consume(ConsumeContext<DoorEventsRecorded> context)
    {
        foreach (var e in context.Message.Expand())
        {
            await notifier.SiteEventAsync(new SiteEventDto(e.SourceEventId, e.SiteId, e.DoorId, e.Type.ToString(), e.CardNumber, e.DenyReason, e.OccurredAt, e.RecordedAt), context.CancellationToken);
        }
    }
}
