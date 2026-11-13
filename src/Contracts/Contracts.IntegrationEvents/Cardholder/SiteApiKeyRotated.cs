namespace AccessHub.Contracts.IntegrationEvents.Cardholder;

/// <summary>A site's API key was replaced. The previous key stops working once this is consumed.</summary>
/// <param name="EventId">Event ID.</param>
/// <param name="OccurredAt">When the key was rotated.</param>
/// <param name="CorrelationId">Correlation ID.</param>
/// <param name="SiteId">Site ID.</param>
/// <param name="Name">Site name (full state).</param>
/// <param name="TimeZoneId">IANA time zone (full state).</param>
/// <param name="ApiKeyHash">Base64 SHA-256 hash of the new key.</param>
/// <param name="Version">Aggregate version.</param>
public sealed record SiteApiKeyRotated(
    Guid EventId, DateTimeOffset OccurredAt, Guid CorrelationId,
    Guid SiteId, string Name, string TimeZoneId, string ApiKeyHash, int Version) : IVersionedStateEvent;
