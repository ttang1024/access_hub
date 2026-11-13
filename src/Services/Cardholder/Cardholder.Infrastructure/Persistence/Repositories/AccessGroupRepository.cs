using AccessHub.Cardholder.Application.Abstractions;
using AccessHub.Cardholder.Domain.AccessGroups;
using Microsoft.EntityFrameworkCore;

namespace AccessHub.Cardholder.Infrastructure.Persistence.Repositories;

internal sealed class AccessGroupRepository(CardholderDbContext db) : IAccessGroupRepository
{
    public Task<AccessGroup?> GetAsync(AccessGroupId id, CancellationToken cancellationToken) =>
        db.AccessGroups.SingleOrDefaultAsync(g => g.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Guid>> FindMissingAsync(IReadOnlyCollection<AccessGroupId> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        var found = await db.AccessGroups.AsNoTracking()
            .Where(g => ids.Contains(g.Id) && !g.IsDeleted)
            .Select(g => g.Id)
            .ToListAsync(cancellationToken);
        return ids.Except(found).Select(i => i.Value).ToList();
    }

    public void Add(AccessGroup group) => db.AccessGroups.Add(group);
}
