namespace AccessHub.Contracts.IntegrationEvents.Alarm;

/// <summary>A new alarm was raised.</summary>
/// <param name="EventId">Event ID.</param>
/// <param name="OccurredAt">When the alarm was raised.</param>
/// <param name="CorrelationId">Correlation ID.</param>
/// <param name="AlarmId">Alarm ID.</param>
/// <param name="Rule">Rule that raised it.</param>
/// <param name="Priority">High or Medium.</param>
/// <param name="SiteId">Site.</param>
/// <param name="DoorId">Door, if the subject is a door.</param>
/// <param name="CardNumber">Card, if the subject is a card.</param>
/// <param name="TriggeredAt">When the triggering event happened at the site.</param>
public sealed record AlarmRaised(
    Guid EventId, DateTimeOffset OccurredAt, Guid CorrelationId,
    Guid AlarmId, string Rule, string Priority, Guid SiteId, Guid? DoorId, string? CardNumber, DateTimeOffset TriggeredAt) : IIntegrationEvent;
