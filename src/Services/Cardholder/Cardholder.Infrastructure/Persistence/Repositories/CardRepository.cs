using AccessHub.Cardholder.Application.Abstractions;
using AccessHub.Cardholder.Domain.Cards;
using Microsoft.EntityFrameworkCore;

namespace AccessHub.Cardholder.Infrastructure.Persistence.Repositories;

internal sealed class CardRepository(CardholderDbContext db) : ICardRepository
{
    public Task<Card?> GetAsync(CardId id, CancellationToken cancellationToken) =>
        db.Cards.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<bool> NumberExistsAsync(string number, CancellationToken cancellationToken) =>
        db.Cards.AnyAsync(c => c.Number == number, cancellationToken);

    public void Add(Card card) => db.Cards.Add(card);
}
