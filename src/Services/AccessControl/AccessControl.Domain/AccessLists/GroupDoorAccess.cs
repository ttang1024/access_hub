namespace AccessHub.AccessControl.Domain.AccessLists;

public sealed record GroupDoorAccess(Guid DoorId, IReadOnlyList<ScheduleWindow> Windows);
