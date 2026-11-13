namespace AccessHub.Contracts.IntegrationEvents.AccessControl;

/// <summary>
/// A site granted access although the cloud already knew the card was revoked or expired, or the cardholder inactive,
/// at the time of the grant. Published once per source event.
/// </summary>
/// <param name="EventId">Event ID.</param>
/// <param name="OccurredAt">When the stale grant was detected.</param>
/// <param name="CorrelationId">Correlation ID.</param>
/// <param name="SiteId">Site that granted access.</param>
/// <param name="DoorId">Door.</param>
/// <param name="CardNumber">Card.</param>
/// <param name="SourceEventId">The site-generated AccessGranted event ID.</param>
/// <param name="GrantedAt">When the site granted access.</param>
/// <param name="Reason">Why the grant was stale: CardRevoked, CardExpired or CardholderInactive.</param>
/// <param name="AccessListSequence">The sequence the site had applied, if reported.</param>
public sealed record StaleAccessGranted(
    Guid EventId, DateTimeOffset OccurredAt, Guid CorrelationId,
    Guid SiteId, Guid DoorId, string CardNumber, Guid SourceEventId, DateTimeOffset GrantedAt, string Reason, long? AccessListSequence) : IIntegrationEvent;
