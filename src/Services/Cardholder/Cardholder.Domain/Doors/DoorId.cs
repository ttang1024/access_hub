namespace AccessHub.Cardholder.Domain.Doors;

public readonly record struct DoorId(Guid Value)
{
    public static DoorId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}
