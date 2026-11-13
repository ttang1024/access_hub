namespace AccessHub.Cardholder.Domain.Cardholders;

public readonly record struct CardholderId(Guid Value)
{
    public static CardholderId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}
