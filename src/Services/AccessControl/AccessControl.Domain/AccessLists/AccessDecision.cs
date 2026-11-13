namespace AccessHub.AccessControl.Domain.AccessLists;

public readonly record struct AccessDecision(bool Granted, DenyReason? Reason)
{
    public static AccessDecision Grant() => new(true, null);

    public static AccessDecision Deny(DenyReason reason) => new(false, reason);
}
