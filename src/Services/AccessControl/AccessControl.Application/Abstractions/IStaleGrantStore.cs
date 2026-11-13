using AccessHub.AccessControl.Domain.ReadModel;

namespace AccessHub.AccessControl.Application.Abstractions;

public interface IStaleGrantStore
{
    /// <summary>The newest known card for each number (numbers not found are absent).</summary>
    Task<IReadOnlyDictionary<string, CardState>> FindCardsByNumbersAsync(IReadOnlyCollection<string> cardNumbers, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, CardholderState>> FindCardholdersAsync(IReadOnlyCollection<Guid> cardholderIds, CancellationToken cancellationToken);

    /// <summary>Records that this source event was flagged. Returns false if it already was (publish once per event).</summary>
    Task<bool> TryRecordDetectionAsync(Guid sourceEventId, string reason, DateTimeOffset detectedAt, CancellationToken cancellationToken);
}
