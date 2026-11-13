using AccessHub.AccessControl.Domain.AccessLists;

namespace AccessHub.AccessControl.Domain.ReadModel;

public sealed record GroupDoorState(Guid DoorId, Guid SiteId, IReadOnlyList<ScheduleWindow> Windows);
