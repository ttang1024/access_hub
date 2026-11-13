using AccessHub.BuildingBlocks.Domain;
using AccessHub.Cardholder.Domain.Sites;

namespace AccessHub.Cardholder.Domain.Doors;

public sealed record DoorRegisteredDomainEvent(DoorId DoorId, SiteId SiteId, string Name, int Version, DateTimeOffset OccurredAt) : IDomainEvent;
