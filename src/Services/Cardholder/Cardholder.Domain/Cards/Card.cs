using AccessHub.BuildingBlocks.Domain;
using AccessHub.Cardholder.Domain.Cardholders;

namespace AccessHub.Cardholder.Domain.Cards;

/// <summary>A card issued to a cardholder. Revocation is permanent.</summary>
public sealed class Card : AggregateRoot<CardId>
{
    private Card()
    {
    }

    private Card(CardId id, CardholderId cardholderId, CardNumber number, DateTimeOffset? expiresAt, DateTimeOffset issuedAt)
        : base(id)
    {
        CardholderId = cardholderId;
        Number = number.Value;
        ExpiresAt = expiresAt;
        IssuedAt = issuedAt;
    }

    public CardholderId CardholderId { get; private set; }

    public string Number { get; private set; } = string.Empty;

    public DateTimeOffset IssuedAt { get; private set; }

    public DateTimeOffset? ExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public bool IsRevoked => RevokedAt is not null;

    public static Result<Card> Issue(CardholderId cardholderId, string number, DateTimeOffset? expiresAt, DateTimeOffset now)
    {
        var cardNumber = CardNumber.Create(number);
        if (cardNumber.IsFailure)
        {
            return cardNumber.Error!;
        }

        if (expiresAt is { } expiry && expiry <= now)
        {
            return CardErrors.ExpiryInPast;
        }

        var card = new Card(CardId.New(), cardholderId, cardNumber.Value, expiresAt?.ToUniversalTime(), now);
        card.Apply(v => new CardIssuedDomainEvent(card.Id, cardholderId, card.Number, card.ExpiresAt, v, now));
        return card;
    }

    public Result Revoke(DateTimeOffset now)
    {
        if (IsRevoked)
        {
            return CardErrors.AlreadyRevoked(Id);
        }

        RevokedAt = now;
        Apply(v => new CardRevokedDomainEvent(Id, CardholderId, Number, ExpiresAt, now, v, now));
        return Result.Success();
    }
}
