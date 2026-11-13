using AccessHub.BuildingBlocks.Domain;
using AccessHub.Cardholder.Domain.AccessGroups;

namespace AccessHub.Cardholder.Domain.Cardholders;

public sealed record CardholderChangedDomainEvent(
    CardholderId CardholderId, string FirstName, string LastName, bool IsActive, IReadOnlyList<AccessGroupId> AccessGroupIds, int Version, DateTimeOffset OccurredAt) : IDomainEvent;
