namespace AccessHub.BuildingBlocks.Application;

/// <summary>
/// Commits a use case atomically. Domain events are dispatched inside the save, before commit,
/// so integration events land in the outbox in the same transaction as the state change.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>Dispatches domain events, then saves state, outbox messages and audit entries in one transaction.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Runs <paramref name="operation"/> in one database transaction, then saves (dispatching domain events and writing
    /// outbox messages) and commits. Use when the operation issues its own SQL that must commit with the outbox.
    /// </summary>
    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken);
}
