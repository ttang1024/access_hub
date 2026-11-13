namespace AccessHub.Alarm.Application.Abstractions;

/// <summary>Recent AccessDenied events per card, kept in the Alarm database so the rule survives restarts.</summary>
public interface IDenialStore
{
    /// <summary>
    /// Locks the batch's cards (in a fixed order, so concurrent batches cannot deadlock), returns the denials already
    /// stored for those cards within <paramref name="window"/> of the batch, then stores the batch's denials (once per
    /// source event).
    /// </summary>
    Task<IReadOnlyList<StoredDenial>> RecordAsync(Guid siteId, IReadOnlyList<StoredDenial> denials, TimeSpan window, CancellationToken cancellationToken);
}
