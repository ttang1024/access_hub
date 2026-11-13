using AccessHub.BuildingBlocks.Application;
using AccessHub.BuildingBlocks.Domain;
using AccessHub.Cardholder.Application.Abstractions;
using AccessHub.Cardholder.Domain.Cards;

namespace AccessHub.Cardholder.Application.Features.Cards;

public sealed class RevokeCardHandler(ICardRepository cards, IUnitOfWork unitOfWork, IAuditLog audit, TimeProvider clock)
    : ICommandHandler<RevokeCardCommand>
{
    public async Task<Result> HandleAsync(RevokeCardCommand command, CancellationToken cancellationToken)
    {
        var id = new CardId(command.CardId);
        var card = await cards.GetAsync(id, cancellationToken);
        if (card is null)
        {
            return CardErrors.NotFound(id);
        }

        var revoked = card.Revoke(clock.GetUtcNow());
        if (revoked.IsFailure)
        {
            return revoked;
        }

        audit.Record("Card.Revoked", nameof(Card), id.ToString(), new { IsRevoked = false }, new { IsRevoked = true, card.RevokedAt });
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
