using AccessHub.Cardholder.Domain.Doors;

namespace AccessHub.Cardholder.Application.Abstractions;

public interface IDoorRepository
{
    Task<IReadOnlyList<Door>> GetManyAsync(IReadOnlyCollection<DoorId> ids, CancellationToken cancellationToken);

    void Add(Door door);
}
