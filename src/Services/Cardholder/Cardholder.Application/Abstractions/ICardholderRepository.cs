using AccessHub.Cardholder.Domain.Cardholders;

namespace AccessHub.Cardholder.Application.Abstractions;

public interface ICardholderRepository
{
    Task<Domain.Cardholders.Cardholder?> GetAsync(CardholderId id, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(CardholderId id, CancellationToken cancellationToken);

    void Add(Domain.Cardholders.Cardholder cardholder);
}
