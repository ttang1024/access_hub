using AccessHub.BuildingBlocks.Domain;

namespace AccessHub.Cardholder.Domain.Sites;

public sealed record SiteApiKeyRotatedDomainEvent(SiteId SiteId, string Name, string TimeZoneId, byte[] ApiKeyHash, int Version, DateTimeOffset OccurredAt) : IDomainEvent;
