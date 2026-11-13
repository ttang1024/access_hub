using AccessHub.BuildingBlocks.Domain;

namespace AccessHub.Cardholder.Domain.Cards;

public static class CardErrors
{
    public static Error NotFound(CardId id) => Error.NotFound("Card.NotFound", $"Card {id} was not found.");

    public static Error InvalidNumber => Error.Validation("Card.InvalidNumber", "A card number is 4 to 32 letters or digits.");

    public static Error ExpiryInPast => Error.Validation("Card.ExpiryInPast", "A card cannot be issued with an expiry in the past.");

    public static Error AlreadyRevoked(CardId id) => Error.Conflict("Card.AlreadyRevoked", $"Card {id} is already revoked.");

    public static Error NumberInUse(string number) => Error.Conflict("Card.NumberInUse", $"Card number {number} is already issued.");
}
