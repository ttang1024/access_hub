namespace AccessHub.AccessControl.Application.Abstractions;

public interface ISiteChangeLog
{
    /// <summary>Reads the list and its sequence in one consistent snapshot.</summary>
    Task<SiteSnapshot?> GetSnapshotAsync(Guid siteId, CancellationToken cancellationToken);

    Task<ChangeLogPosition?> GetPositionAsync(Guid siteId, CancellationToken cancellationToken);

    /// <summary>Changes with sequence greater than <paramref name="afterSequence"/>, in order.</summary>
    Task<IReadOnlyList<SiteChangeRecord>> ReadChangesAsync(Guid siteId, long afterSequence, int limit, CancellationToken cancellationToken);
}
