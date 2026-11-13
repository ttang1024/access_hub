namespace AccessHub.Contracts.IntegrationEvents.Cardholder;

/// <summary>A site was registered. Carries the API key hash so site-facing services can authenticate the site.</summary>
/// <param name="EventId">Event ID.</param>
/// <param name="OccurredAt">When the site was registered.</param>
/// <param name="CorrelationId">Correlation ID.</param>
/// <param name="SiteId">Site ID.</param>
/// <param name="Name">Site name.</param>
/// <param name="TimeZoneId">IANA time zone.</param>
/// <param name="ApiKeyHash">Base64 SHA-256 hash of the site's API key.</param>
/// <param name="Version">Aggregate version.</param>
public sealed record SiteRegistered(
    Guid EventId, DateTimeOffset OccurredAt, Guid CorrelationId,
    Guid SiteId, string Name, string TimeZoneId, string ApiKeyHash, int Version) : IVersionedStateEvent;
