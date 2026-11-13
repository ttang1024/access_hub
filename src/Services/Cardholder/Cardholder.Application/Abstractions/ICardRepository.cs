using AccessHub.Cardholder.Domain.Cards;

namespace AccessHub.Cardholder.Application.Abstractions;

public interface ICardRepository
{
    Task<Card?> GetAsync(CardId id, CancellationToken cancellationToken);

    Task<bool> NumberExistsAsync(string number, CancellationToken cancellationToken);

    void Add(Card card);
}
