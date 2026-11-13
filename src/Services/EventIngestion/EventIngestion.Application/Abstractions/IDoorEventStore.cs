using AccessHub.EventIngestion.Domain.Events;

namespace AccessHub.EventIngestion.Application.Abstractions;

public interface IDoorEventStore
{
    /// <summary>
    /// Inserts the events, skipping any (site, event ID) already stored, and returns only the newly stored ones.
    /// Must run inside the unit of work's transaction.
    /// </summary>
    Task<IReadOnlyList<DoorEvent>> InsertNewAsync(IReadOnlyList<DoorEvent> events, CancellationToken cancellationToken);
}
