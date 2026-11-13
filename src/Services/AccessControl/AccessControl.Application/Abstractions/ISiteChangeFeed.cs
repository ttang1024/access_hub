namespace AccessHub.AccessControl.Application.Abstractions;

/// <summary>In-process fan-out of newly committed changes to this replica's open streams.</summary>
public interface ISiteChangeFeed
{
    ISiteChangeSubscription Subscribe(Guid siteId, long fromSequence);
}
