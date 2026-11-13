namespace AccessHub.Contracts.IntegrationEvents.Cardholder;

/// <summary>A door was registered at a site.</summary>
/// <param name="EventId">Event ID.</param>
/// <param name="OccurredAt">When the door was registered.</param>
/// <param name="CorrelationId">Correlation ID.</param>
/// <param name="DoorId">Door ID.</param>
/// <param name="SiteId">The site the door belongs to.</param>
/// <param name="Name">Door name.</param>
/// <param name="Version">Aggregate version.</param>
public sealed record DoorRegistered(
    Guid EventId, DateTimeOffset OccurredAt, Guid CorrelationId,
    Guid DoorId, Guid SiteId, string Name, int Version) : IVersionedStateEvent;
