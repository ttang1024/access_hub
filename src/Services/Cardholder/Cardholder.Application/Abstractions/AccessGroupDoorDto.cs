namespace AccessHub.Cardholder.Application.Abstractions;

public sealed record AccessGroupDoorDto(Guid DoorId, Guid SiteId, IReadOnlyList<TimeWindowDto> Windows);
