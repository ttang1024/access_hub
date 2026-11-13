using AccessHub.Cardholder.Domain.AccessGroups;

namespace AccessHub.Cardholder.Application.Abstractions;

public interface IAccessGroupRepository
{
    Task<AccessGroup?> GetAsync(AccessGroupId id, CancellationToken cancellationToken);

    /// <summary>Returns the IDs that do not exist or are deleted.</summary>
    Task<IReadOnlyList<Guid>> FindMissingAsync(IReadOnlyCollection<AccessGroupId> ids, CancellationToken cancellationToken);

    void Add(AccessGroup group);
}
