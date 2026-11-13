using AccessHub.Alarm.Application.Abstractions;
using AccessHub.Alarm.Application.Messages;
using AccessHub.Alarm.Domain;
using MassTransit;

namespace AccessHub.Alarm.Infrastructure;

/// <summary>Publishes triggers through the consumer's outbox, in the same transaction as the rule's bookkeeping.</summary>
internal sealed class AlarmTriggerSink(IPublishEndpoint publish) : IAlarmTriggerSink
{
    public Task TriggerAsync(AlarmTrigger trigger, CancellationToken cancellationToken) =>
        publish.Publish(
            new AlarmTriggered(trigger.Rule.ToString(), trigger.Priority.ToString(), trigger.SubjectKey, trigger.SiteId, trigger.DoorId, trigger.CardNumber, trigger.SourceEventId, trigger.TriggeredAt),
            cancellationToken);
}
