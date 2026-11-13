using MassTransit;

namespace AccessHub.Alarm.Infrastructure.Sagas;

/// <summary>One alarm. CorrelationId is the alarm ID. Closed alarms are kept as history.</summary>
public sealed class AlarmState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }

    public string CurrentState { get; set; } = string.Empty;

    public string Rule { get; set; } = string.Empty;

    public string Priority { get; set; } = string.Empty;

    public string SubjectKey { get; set; } = string.Empty;

    public Guid SiteId { get; set; }

    public Guid? DoorId { get; set; }

    public string? CardNumber { get; set; }

    public Guid FirstSourceEventId { get; set; }

    public DateTimeOffset RaisedAt { get; set; }

    public DateTimeOffset TriggeredAt { get; set; }

    public DateTimeOffset LastTriggeredAt { get; set; }

    public int OccurrenceCount { get; set; }

    public bool Escalated { get; set; }

    public DateTimeOffset? EscalatedAt { get; set; }

    /// <summary>Token of the scheduled escalation message, so acknowledging can cancel it.</summary>
    public Guid? EscalationTokenId { get; set; }

    public string? AcknowledgedBy { get; set; }

    public DateTimeOffset? AcknowledgedAt { get; set; }

    public string? ClosedBy { get; set; }

    public DateTimeOffset? ClosedAt { get; set; }

    public string? CloseNote { get; set; }
}
