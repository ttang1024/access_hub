using AccessHub.BuildingBlocks.Application;
using AccessHub.BuildingBlocks.Domain;
using AccessHub.Cardholder.Application.Abstractions;
using AccessHub.Cardholder.Domain.Cardholders;
using AccessHub.Cardholder.Domain.Cards;

namespace AccessHub.Cardholder.Application.Features.Cards;

public sealed class IssueCardHandler(ICardholderRepository cardholders, ICardRepository cards, IUnitOfWork unitOfWork, IAuditLog audit, TimeProvider clock)
    : ICommandHandler<IssueCardCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(IssueCardCommand command, CancellationToken cancellationToken)
    {
        var cardholderId = new CardholderId(command.CardholderId);
        if (!await cardholders.ExistsAsync(cardholderId, cancellationToken))
        {
            return CardholderErrors.NotFound(cardholderId);
        }

        var card = Card.Issue(cardholderId, command.CardNumber, command.ExpiresAt, clock.GetUtcNow());
        if (card.IsFailure)
        {
            return card.Error!;
        }

        // The unique index is the real guard against a concurrent issue; this check gives the common case a clear error.
        if (await cards.NumberExistsAsync(card.Value.Number, cancellationToken))
        {
            return CardErrors.NumberInUse(card.Value.Number);
        }

        cards.Add(card.Value);
        audit.Record("Card.Issued", nameof(Card), card.Value.Id.ToString(), null, new { CardholderId = cardholderId.Value, card.Value.Number, card.Value.ExpiresAt });
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return card.Value.Id.Value;
    }
}
