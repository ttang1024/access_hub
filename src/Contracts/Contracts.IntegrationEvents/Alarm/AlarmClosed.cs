namespace AccessHub.Contracts.IntegrationEvents.Alarm;

/// <summary>An operator closed an acknowledged alarm with a note.</summary>
/// <param name="EventId">Event ID.</param>
/// <param name="OccurredAt">When it was closed.</param>
/// <param name="CorrelationId">Correlation ID.</param>
/// <param name="AlarmId">Alarm ID.</param>
/// <param name="ClosedBy">Operator user ID.</param>
/// <param name="Note">Closing note.</param>
public sealed record AlarmClosed(
    Guid EventId, DateTimeOffset OccurredAt, Guid CorrelationId, Guid AlarmId, string ClosedBy, string Note) : IIntegrationEvent;
