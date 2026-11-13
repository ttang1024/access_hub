using AccessHub.Cardholder.Application.Abstractions;
using AccessHub.Cardholder.Domain.Doors;
using Microsoft.EntityFrameworkCore;

namespace AccessHub.Cardholder.Infrastructure.Persistence.Repositories;

internal sealed class DoorRepository(CardholderDbContext db) : IDoorRepository
{
    public async Task<IReadOnlyList<Door>> GetManyAsync(IReadOnlyCollection<DoorId> ids, CancellationToken cancellationToken) =>
        await db.Doors.Where(d => ids.Contains(d.Id)).ToListAsync(cancellationToken);

    public void Add(Door door) => db.Doors.Add(door);
}
