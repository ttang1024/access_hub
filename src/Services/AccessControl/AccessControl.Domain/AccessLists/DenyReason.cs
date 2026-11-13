namespace AccessHub.AccessControl.Domain.AccessLists;

/// <summary>Why access was denied. Checks run in this order; the first failure wins.</summary>
public enum DenyReason
{
    UnknownCard = 1,
    CardRevoked = 2,
    CardExpired = 3,
    CardholderInactive = 4,
    NoAccessToDoor = 5,
    OutsideSchedule = 6,
}
