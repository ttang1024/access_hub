namespace AccessHub.Cardholder.Domain.AccessGroups;

public readonly record struct AccessGroupId(Guid Value)
{
    public static AccessGroupId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}
