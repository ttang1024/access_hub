using AccessHub.BuildingBlocks.Application;
using AccessHub.BuildingBlocks.Domain;
using AccessHub.Cardholder.Application.Abstractions;
using AccessHub.Cardholder.Domain.Doors;
using AccessHub.Cardholder.Domain.Sites;

namespace AccessHub.Cardholder.Application.Features.Doors;

public sealed class RegisterDoorHandler(ISiteRepository sites, IDoorRepository doors, IUnitOfWork unitOfWork, IAuditLog audit, TimeProvider clock)
    : ICommandHandler<RegisterDoorCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(RegisterDoorCommand command, CancellationToken cancellationToken)
    {
        var siteId = new SiteId(command.SiteId);
        if (!await sites.ExistsAsync(siteId, cancellationToken))
        {
            return SiteErrors.NotFound(siteId);
        }

        var door = Door.Register(siteId, command.Name, clock.GetUtcNow());
        if (door.IsFailure)
        {
            return door.Error!;
        }

        doors.Add(door.Value);
        audit.Record("Door.Registered", nameof(Door), door.Value.Id.ToString(), null, new { SiteId = siteId.Value, door.Value.Name });
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return door.Value.Id.Value;
    }
}
