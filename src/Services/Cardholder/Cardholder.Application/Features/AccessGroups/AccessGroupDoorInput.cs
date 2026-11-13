using AccessHub.Cardholder.Application.Abstractions;

namespace AccessHub.Cardholder.Application.Features.AccessGroups;

public sealed record AccessGroupDoorInput(Guid DoorId, IReadOnlyList<TimeWindowDto> Windows);
