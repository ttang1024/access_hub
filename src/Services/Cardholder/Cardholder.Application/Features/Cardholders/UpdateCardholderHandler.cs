using AccessHub.BuildingBlocks.Application;
using AccessHub.BuildingBlocks.Domain;
using AccessHub.Cardholder.Application.Abstractions;
using AccessHub.Cardholder.Domain.AccessGroups;
using AccessHub.Cardholder.Domain.Cardholders;

namespace AccessHub.Cardholder.Application.Features.Cardholders;

public sealed class UpdateCardholderHandler(ICardholderRepository cardholders, IAccessGroupRepository groups, IUnitOfWork unitOfWork, IAuditLog audit, TimeProvider clock)
    : ICommandHandler<UpdateCardholderCommand>
{
    public async Task<Result> HandleAsync(UpdateCardholderCommand command, CancellationToken cancellationToken)
    {
        var id = new CardholderId(command.CardholderId);
        var cardholder = await cardholders.GetAsync(id, cancellationToken);
        if (cardholder is null)
        {
            return CardholderErrors.NotFound(id);
        }

        var groupIds = (command.AccessGroupIds ?? []).Select(g => new AccessGroupId(g)).ToList();
        var missing = await groups.FindMissingAsync(groupIds, cancellationToken);
        if (missing.Count > 0)
        {
            return AccessGroupErrors.NotFoundMany(missing);
        }

        var before = CreateCardholderHandler.Summary(cardholder);
        var updated = cardholder.Update(command.FirstName, command.LastName, command.IsActive, groupIds, command.ExpectedVersion, clock.GetUtcNow());
        if (updated.IsFailure)
        {
            return updated;
        }

        audit.Record("Cardholder.Updated", "Cardholder", id.ToString(), before, CreateCardholderHandler.Summary(cardholder));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
