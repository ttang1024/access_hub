using AccessHub.BuildingBlocks.Application;
using AccessHub.BuildingBlocks.Domain;
using AccessHub.Cardholder.Application.Abstractions;
using AccessHub.Cardholder.Domain.AccessGroups;

namespace AccessHub.Cardholder.Application.Features.AccessGroups;

public sealed class CreateAccessGroupHandler(IAccessGroupRepository groups, IDoorRepository doors, IUnitOfWork unitOfWork, IAuditLog audit, TimeProvider clock)
    : ICommandHandler<CreateAccessGroupCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(CreateAccessGroupCommand command, CancellationToken cancellationToken)
    {
        var doorAccess = await DoorAccessBuilder.BuildAsync(doors, command.Doors, cancellationToken);
        if (doorAccess.IsFailure)
        {
            return doorAccess.Error!;
        }

        var group = AccessGroup.Create(command.Name, doorAccess.Value, clock.GetUtcNow());
        if (group.IsFailure)
        {
            return group.Error!;
        }

        groups.Add(group.Value);
        audit.Record("AccessGroup.Created", nameof(AccessGroup), group.Value.Id.ToString(), null, DoorAccessBuilder.Summary(group.Value));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return group.Value.Id.Value;
    }
}
