namespace AccessHub.Cardholder.Application.Features.AccessGroups;

public sealed record CreateAccessGroupCommand(string Name, IReadOnlyList<AccessGroupDoorInput> Doors);
