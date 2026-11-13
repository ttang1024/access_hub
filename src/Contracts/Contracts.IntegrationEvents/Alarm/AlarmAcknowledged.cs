namespace AccessHub.Contracts.IntegrationEvents.Alarm;

/// <summary>An operator acknowledged an alarm.</summary>
/// <param name="EventId">Event ID.</param>
/// <param name="OccurredAt">When it was acknowledged.</param>
/// <param name="CorrelationId">Correlation ID.</param>
/// <param name="AlarmId">Alarm ID.</param>
/// <param name="AcknowledgedBy">Operator user ID.</param>
public sealed record AlarmAcknowledged(
    Guid EventId, DateTimeOffset OccurredAt, Guid CorrelationId, Guid AlarmId, string AcknowledgedBy) : IIntegrationEvent;
