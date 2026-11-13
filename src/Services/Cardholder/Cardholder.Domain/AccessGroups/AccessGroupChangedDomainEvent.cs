using AccessHub.BuildingBlocks.Domain;

namespace AccessHub.Cardholder.Domain.AccessGroups;

public sealed record AccessGroupChangedDomainEvent(
    AccessGroupId AccessGroupId, string Name, bool IsDeleted, IReadOnlyList<DoorAccess> Doors, int Version, DateTimeOffset OccurredAt) : IDomainEvent;
