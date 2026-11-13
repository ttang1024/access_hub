using AccessHub.Cardholder.Application.Abstractions;
using AccessHub.Cardholder.Domain.Cardholders;
using Microsoft.EntityFrameworkCore;
using CardholderAggregate = AccessHub.Cardholder.Domain.Cardholders.Cardholder;

namespace AccessHub.Cardholder.Infrastructure.Persistence.Repositories;

internal sealed class CardholderRepository(CardholderDbContext db) : ICardholderRepository
{
    public Task<CardholderAggregate?> GetAsync(CardholderId id, CancellationToken cancellationToken) =>
        db.Cardholders.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(CardholderId id, CancellationToken cancellationToken) =>
        db.Cardholders.AnyAsync(c => c.Id == id, cancellationToken);

    public void Add(CardholderAggregate cardholder) => db.Cardholders.Add(cardholder);
}
