namespace AccessHub.AccessControl.Domain.ReadModel;

public sealed class CardholderState : VersionedState
{
    private CardholderState()
    {
    }

    public Guid CardholderId { get; private set; }

    public bool IsActive { get; private set; }

    public IReadOnlyList<Guid> AccessGroupIds { get; private set; } = [];

    /// <summary>When the cardholder became inactive (the source event's time), for stale grant detection.</summary>
    public DateTimeOffset? InactiveSince { get; private set; }

    public static CardholderState Create(Guid cardholderId) => new() { CardholderId = cardholderId };

    public bool Apply(bool isActive, IReadOnlyList<Guid> accessGroupIds, int version, DateTimeOffset occurredAt)
    {
        if (!IsNewer(version))
        {
            return false;
        }

        if (!isActive && (IsActive || Version == 0 || InactiveSince is null))
        {
            InactiveSince = occurredAt;
        }
        else if (isActive)
        {
            InactiveSince = null;
        }

        IsActive = isActive;
        AccessGroupIds = accessGroupIds.Distinct().Order().ToList();
        Version = version;
        return true;
    }

    public bool WasInactiveAt(DateTimeOffset timestamp) => !IsActive && InactiveSince is { } since && since <= timestamp;
}
