using AccessHub.AccessControl.Application.Abstractions;
using AccessHub.AccessControl.Domain.AccessLists;
using AccessHub.AccessControl.Domain.ReadModel;
using AccessHub.BuildingBlocks.Application;
using AccessHub.BuildingBlocks.Domain;
using AccessHub.Contracts.IntegrationEvents.Cardholder;

namespace AccessHub.AccessControl.Application.Projection;

public sealed class ApplyDoorRegisteredHandler(IProjectionStore store, SiteListReconciler reconciler) : ICommandHandler<DoorRegistered>
{
    public async Task<Result> HandleAsync(DoorRegistered e, CancellationToken cancellationToken)
    {
        await store.AcquireProjectionLockAsync(cancellationToken);
        var door = await store.FindDoorAsync(e.DoorId, cancellationToken);
        if (door is null)
        {
            door = DoorState.Create(e.DoorId);
            store.Add(door);
        }

        if (!door.Apply(e.SiteId, e.Name, e.Version))
        {
            return Result.Success();
        }

        await reconciler.ReconcileAsync([new ReconcileTarget(AccessListEntryKind.Door, e.DoorId, [e.SiteId])], e.OccurredAt, cancellationToken);
        return Result.Success();
    }
}
