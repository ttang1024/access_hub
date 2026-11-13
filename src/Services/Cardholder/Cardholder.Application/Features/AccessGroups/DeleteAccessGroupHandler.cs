using AccessHub.BuildingBlocks.Application;
using AccessHub.BuildingBlocks.Domain;
using AccessHub.Cardholder.Application.Abstractions;
using AccessHub.Cardholder.Domain.AccessGroups;

namespace AccessHub.Cardholder.Application.Features.AccessGroups;

public sealed class DeleteAccessGroupHandler(IAccessGroupRepository groups, IUnitOfWork unitOfWork, IAuditLog audit, TimeProvider clock)
    : ICommandHandler<DeleteAccessGroupCommand>
{
    public async Task<Result> HandleAsync(DeleteAccessGroupCommand command, CancellationToken cancellationToken)
    {
        var id = new AccessGroupId(command.AccessGroupId);
        var group = await groups.GetAsync(id, cancellationToken);
        if (group is null)
        {
            return AccessGroupErrors.NotFound(id);
        }

        var before = DoorAccessBuilder.Summary(group);
        var deleted = group.Delete(command.ExpectedVersion, clock.GetUtcNow());
        if (deleted.IsFailure)
        {
            return deleted;
        }

        audit.Record("AccessGroup.Deleted", nameof(AccessGroup), id.ToString(), before, null);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
