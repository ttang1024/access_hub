namespace AccessHub.Cardholder.Domain.Cards;

public readonly record struct CardId(Guid Value)
{
    public static CardId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}
