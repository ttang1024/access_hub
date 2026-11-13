using AccessHub.AccessControl.Domain.AccessLists;

namespace AccessHub.AccessControl.Application.Projection;

/// <summary>An entity whose entry may have changed, and the sites that may now need it.</summary>
public sealed record ReconcileTarget(AccessListEntryKind Kind, Guid EntityId, IReadOnlyCollection<Guid> CandidateSites);
