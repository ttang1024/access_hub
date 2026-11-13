using AccessHub.BuildingBlocks.Application;
using AccessHub.BuildingBlocks.Domain;
using AccessHub.Cardholder.Application.Abstractions;
using AccessHub.Cardholder.Domain.AccessGroups;

namespace AccessHub.Cardholder.Application.Features.AccessGroups;

public sealed class UpdateAccessGroupHandler(IAccessGroupRepository groups, IDoorRepository doors, IUnitOfWork unitOfWork, IAuditLog audit, TimeProvider clock)
    : ICommandHandler<UpdateAccessGroupCommand>
{
    public async Task<Result> HandleAsync(UpdateAccessGroupCommand command, CancellationToken cancellationToken)
    {
        var id = new AccessGroupId(command.AccessGroupId);
        var group = await groups.GetAsync(id, cancellationToken);
        if (group is null)
        {
            return AccessGroupErrors.NotFound(id);
        }

        var doorAccess = await DoorAccessBuilder.BuildAsync(doors, command.Doors, cancellationToken);
        if (doorAccess.IsFailure)
        {
            return doorAccess.Error!;
        }

        var before = DoorAccessBuilder.Summary(group);
        var updated = group.Update(command.Name, doorAccess.Value, command.ExpectedVersion, clock.GetUtcNow());
        if (updated.IsFailure)
        {
            return updated;
        }

        audit.Record("AccessGroup.Updated", nameof(AccessGroup), id.ToString(), before, DoorAccessBuilder.Summary(group));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
