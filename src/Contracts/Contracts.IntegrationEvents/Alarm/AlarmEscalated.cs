namespace AccessHub.Contracts.IntegrationEvents.Alarm;

/// <summary>A high-priority alarm was not acknowledged in time.</summary>
/// <param name="EventId">Event ID.</param>
/// <param name="OccurredAt">When it escalated.</param>
/// <param name="CorrelationId">Correlation ID.</param>
/// <param name="AlarmId">Alarm ID.</param>
/// <param name="Rule">Rule.</param>
/// <param name="SiteId">Site.</param>
public sealed record AlarmEscalated(
    Guid EventId, DateTimeOffset OccurredAt, Guid CorrelationId, Guid AlarmId, string Rule, Guid SiteId) : IIntegrationEvent;
