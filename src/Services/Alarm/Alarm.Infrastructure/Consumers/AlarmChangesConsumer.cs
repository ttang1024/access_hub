using AccessHub.Alarm.Application.Abstractions;
using AccessHub.Alarm.Application.Messages;
using AccessHub.Contracts.IntegrationEvents.Alarm;
using MassTransit;

namespace AccessHub.Alarm.Infrastructure.Consumers;

/// <summary>Every alarm change (published by the saga after commit) is pushed to dashboards with the alarm's current state.</summary>
public sealed class AlarmChangesConsumer(IAlarmReadModel alarms, IDashboardNotifier notifier)
    : IConsumer<AlarmRaised>, IConsumer<AlarmEscalated>, IConsumer<AlarmAcknowledged>, IConsumer<AlarmClosed>, IConsumer<AlarmOccurrenceRecorded>
{
    public Task Consume(ConsumeContext<AlarmRaised> context) => PushAsync(context.Message.AlarmId, context.CancellationToken);

    public Task Consume(ConsumeContext<AlarmEscalated> context) => PushAsync(context.Message.AlarmId, context.CancellationToken);

    public Task Consume(ConsumeContext<AlarmAcknowledged> context) => PushAsync(context.Message.AlarmId, context.CancellationToken);

    public Task Consume(ConsumeContext<AlarmClosed> context) => PushAsync(context.Message.AlarmId, context.CancellationToken);

    public Task Consume(ConsumeContext<AlarmOccurrenceRecorded> context) => PushAsync(context.Message.AlarmId, context.CancellationToken);

    private async Task PushAsync(Guid alarmId, CancellationToken cancellationToken)
    {
        if (await alarms.GetAsync(alarmId, cancellationToken) is { } alarm)
        {
            await notifier.AlarmChangedAsync(alarm, cancellationToken);
        }
    }
}
