namespace AccessHub.AccessControl.Domain.ReadModel;

/// <summary>
/// A site as AccessControl knows it, plus the per-site change counter. A placeholder (version 0) is created when a
/// change for the site arrives before <c>SiteRegistered</c>; the site cannot authenticate until its key hash arrives.
/// </summary>
public sealed class SiteState : VersionedState
{
    private SiteState()
    {
    }

    public Guid SiteId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string TimeZoneId { get; private set; } = "UTC";

    public byte[]? ApiKeyHash { get; private set; }

    /// <summary>Sequence of the last change appended to this site's log. Starts at 0; the first change is 1.</summary>
    public long LastSequence { get; private set; }

    /// <summary>Changes up to and including this sequence have been pruned from the log.</summary>
    public long PrunedThroughSequence { get; private set; }

    public static SiteState Placeholder(Guid siteId) => new() { SiteId = siteId };

    public bool Apply(string name, string timeZoneId, byte[] apiKeyHash, int version)
    {
        if (!IsNewer(version))
        {
            return false;
        }

        Name = name;
        TimeZoneId = timeZoneId;
        ApiKeyHash = apiKeyHash;
        Version = version;
        return true;
    }

    /// <summary>Allocates the next sequence. The caller must hold the site row lock.</summary>
    public long AllocateSequence() => ++LastSequence;
}
