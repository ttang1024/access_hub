namespace AccessHub.Cardholder.Application.Abstractions;

public sealed record AccessGroupDto(Guid Id, string Name, IReadOnlyList<AccessGroupDoorDto> Doors, int Version);
