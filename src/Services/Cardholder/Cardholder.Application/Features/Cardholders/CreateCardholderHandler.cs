using AccessHub.BuildingBlocks.Application;
using AccessHub.BuildingBlocks.Domain;
using AccessHub.Cardholder.Application.Abstractions;
using AccessHub.Cardholder.Domain.AccessGroups;
using CardholderAggregate = AccessHub.Cardholder.Domain.Cardholders.Cardholder;

namespace AccessHub.Cardholder.Application.Features.Cardholders;

public sealed class CreateCardholderHandler(ICardholderRepository cardholders, IAccessGroupRepository groups, IUnitOfWork unitOfWork, IAuditLog audit, TimeProvider clock)
    : ICommandHandler<CreateCardholderCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(CreateCardholderCommand command, CancellationToken cancellationToken)
    {
        var groupIds = (command.AccessGroupIds ?? []).Select(id => new AccessGroupId(id)).ToList();
        var missing = await groups.FindMissingAsync(groupIds, cancellationToken);
        if (missing.Count > 0)
        {
            return AccessGroupErrors.NotFoundMany(missing);
        }

        var cardholder = CardholderAggregate.Create(command.FirstName, command.LastName, groupIds, clock.GetUtcNow());
        if (cardholder.IsFailure)
        {
            return cardholder.Error!;
        }

        cardholders.Add(cardholder.Value);
        audit.Record("Cardholder.Created", nameof(CardholderAggregate), cardholder.Value.Id.ToString(), null, Summary(cardholder.Value));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return cardholder.Value.Id.Value;
    }

    internal static object Summary(CardholderAggregate c) =>
        new { c.FirstName, c.LastName, c.IsActive, AccessGroupIds = c.AccessGroupIds.Select(g => g.Value).ToList(), c.Version };
}
