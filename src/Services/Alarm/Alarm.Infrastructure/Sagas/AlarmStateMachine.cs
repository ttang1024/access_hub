using AccessHub.Alarm.Application.Messages;
using AccessHub.Alarm.Domain;
using AccessHub.BuildingBlocks.Application;
using AccessHub.Contracts.IntegrationEvents.Alarm;
using MassTransit;
using Microsoft.Extensions.Options;

namespace AccessHub.Alarm.Infrastructure.Sagas;

/// <summary>
/// Alarm lifecycle: Raised → Acknowledged → Closed. A high-priority alarm still Raised after the escalation delay
/// escalates (and stays Raised); acknowledging cancels the scheduled escalation. Invalid commands are answered with a
/// rejection and change nothing.
/// </summary>
public sealed class AlarmStateMachine : MassTransitStateMachine<AlarmState>
{
    private const string High = nameof(AlarmPriority.High);

    public AlarmStateMachine(IOptions<AlarmOptions> options, TimeProvider clock)
    {
        InstanceState(x => x.CurrentState);

        Event(() => Triggered, e =>
        {
            // One open alarm per subject: correlate to the alarm that is not closed, or start a new one. A partial
            // unique index on (subject_key) where not closed turns a concurrent double-start into a retry.
            e.CorrelateBy((alarm, context) => alarm.SubjectKey == context.Message.SubjectKey && alarm.CurrentState != AlarmStatus.Closed);
            e.SelectId(_ => Guid.CreateVersion7());
        });
        Event(() => Acknowledge, e =>
        {
            e.CorrelateById(context => context.Message.AlarmId);
            e.OnMissingInstance(m => m.ExecuteAsync(c => c.RespondAsync(new AlarmCommandRejected(c.Message.AlarmId, "NotFound", null))));
        });
        Event(() => Close, e =>
        {
            e.CorrelateById(context => context.Message.AlarmId);
            e.OnMissingInstance(m => m.ExecuteAsync(c => c.RespondAsync(new AlarmCommandRejected(c.Message.AlarmId, "NotFound", null))));
        });
        Schedule(() => Escalation, x => x.EscalationTokenId, s =>
        {
            s.Delay = options.Value.EscalationDelay;
            s.Received = r => r.CorrelateById(context => context.Message.AlarmId);
        });

        Initially(
            When(Triggered)
                .Then(c =>
                {
                    var m = c.Message;
                    var alarm = c.Saga;
                    (alarm.Rule, alarm.Priority, alarm.SubjectKey, alarm.SiteId, alarm.DoorId, alarm.CardNumber) = (m.Rule, m.Priority, m.SubjectKey, m.SiteId, m.DoorId, m.CardNumber);
                    alarm.FirstSourceEventId = m.SourceEventId;
                    alarm.TriggeredAt = m.TriggeredAt;
                    alarm.LastTriggeredAt = m.TriggeredAt;
                    alarm.RaisedAt = clock.GetUtcNow();
                    alarm.OccurrenceCount = 1;
                })
                .Publish(c => new AlarmRaised(
                    Guid.CreateVersion7(), c.Saga.RaisedAt, Correlation.Current(), c.Saga.CorrelationId, c.Saga.Rule, c.Saga.Priority, c.Saga.SiteId, c.Saga.DoorId, c.Saga.CardNumber, c.Saga.TriggeredAt))
                .If(c => c.Saga.Priority == High, b => b.Schedule(Escalation, c => new EscalationDue(c.Saga.CorrelationId)))
                .TransitionTo(Raised));

        During(
            Raised,
            Acknowledged,
            When(Triggered)
                .Then(c =>
                {
                    c.Saga.OccurrenceCount++;
                    if (c.Message.TriggeredAt > c.Saga.LastTriggeredAt)
                    {
                        c.Saga.LastTriggeredAt = c.Message.TriggeredAt;
                    }
                })
                .Publish(c => new AlarmOccurrenceRecorded(c.Saga.CorrelationId, c.Saga.OccurrenceCount, c.Saga.LastTriggeredAt)));

        During(
            Raised,
            When(Escalation!.Received)
                .Then(c =>
                {
                    c.Saga.Escalated = true;
                    c.Saga.EscalatedAt = clock.GetUtcNow();
                    c.Saga.EscalationTokenId = null;
                })
                .Publish(c => new AlarmEscalated(Guid.CreateVersion7(), c.Saga.EscalatedAt!.Value, Correlation.Current(), c.Saga.CorrelationId, c.Saga.Rule, c.Saga.SiteId)),
            When(Acknowledge)
                .Unschedule(Escalation)
                .Then(c =>
                {
                    c.Saga.AcknowledgedBy = c.Message.UserId;
                    c.Saga.AcknowledgedAt = clock.GetUtcNow();
                    c.GetServiceOrCreateInstance<IAlarmAudit>().Record("Alarm.Acknowledged", c.Saga.CorrelationId, c.Message.UserId, AlarmStatus.Raised, AlarmStatus.Acknowledged, null);
                })
                .Publish(c => new AlarmAcknowledged(Guid.CreateVersion7(), c.Saga.AcknowledgedAt!.Value, Correlation.Current(), c.Saga.CorrelationId, c.Message.UserId))
                .ThenAsync(c => c.RespondAsync(new AlarmCommandAccepted(c.Saga.CorrelationId, AlarmStatus.Acknowledged)))
                .TransitionTo(Acknowledged),
            When(Close)
                .ThenAsync(c => c.RespondAsync(new AlarmCommandRejected(c.Saga.CorrelationId, "InvalidTransition", AlarmStatus.Raised))));

        During(
            Acknowledged,
            Ignore(Escalation.Received),
            When(Acknowledge)
                .ThenAsync(c => c.RespondAsync(new AlarmCommandRejected(c.Saga.CorrelationId, "InvalidTransition", AlarmStatus.Acknowledged))),
            When(Close)
                .IfElse(
                    c => AlarmErrors.IsValidNote(c.Message.Note),
                    valid => valid
                        .Then(c =>
                        {
                            c.Saga.ClosedBy = c.Message.UserId;
                            c.Saga.ClosedAt = clock.GetUtcNow();
                            c.Saga.CloseNote = c.Message.Note;
                            c.GetServiceOrCreateInstance<IAlarmAudit>().Record("Alarm.Closed", c.Saga.CorrelationId, c.Message.UserId, AlarmStatus.Acknowledged, AlarmStatus.Closed, c.Message.Note);
                        })
                        .Publish(c => new AlarmClosed(Guid.CreateVersion7(), c.Saga.ClosedAt!.Value, Correlation.Current(), c.Saga.CorrelationId, c.Message.UserId, c.Message.Note))
                        .ThenAsync(c => c.RespondAsync(new AlarmCommandAccepted(c.Saga.CorrelationId, AlarmStatus.Closed)))
                        .TransitionTo(Closed),
                    invalid => invalid
                        .ThenAsync(c => c.RespondAsync(new AlarmCommandRejected(c.Saga.CorrelationId, "NoteRequired", AlarmStatus.Acknowledged)))));

        During(
            Closed,
            Ignore(Escalation.Received),
            Ignore(Triggered),
            When(Acknowledge).ThenAsync(c => c.RespondAsync(new AlarmCommandRejected(c.Saga.CorrelationId, "InvalidTransition", AlarmStatus.Closed))),
            When(Close).ThenAsync(c => c.RespondAsync(new AlarmCommandRejected(c.Saga.CorrelationId, "InvalidTransition", AlarmStatus.Closed))));
    }

    public State Raised { get; private set; } = null!;

    public State Acknowledged { get; private set; } = null!;

    public State Closed { get; private set; } = null!;

    public Event<AlarmTriggered> Triggered { get; private set; } = null!;

    public Event<AcknowledgeAlarm> Acknowledge { get; private set; } = null!;

    public Event<CloseAlarm> Close { get; private set; } = null!;

    public Schedule<AlarmState, EscalationDue> Escalation { get; private set; } = null!;
}
