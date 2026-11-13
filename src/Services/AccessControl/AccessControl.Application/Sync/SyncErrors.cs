using AccessHub.BuildingBlocks.Domain;

namespace AccessHub.AccessControl.Application.Sync;

public static class SyncErrors
{
    public static Error UnknownSite(Guid siteId) => Error.NotFound("Sync.UnknownSite", $"Site {siteId} has no access list.");

    /// <summary>Mapped to gRPC FAILED_PRECONDITION: the site must take a new snapshot.</summary>
    public static Error SequenceUnavailable(long fromSequence, long prunedThrough, long latest) =>
        Error.Failure("Sync.SequenceUnavailable", $"Cannot resume from {fromSequence}: the log holds {prunedThrough + 1}..{latest}. Take a new snapshot.");
}
