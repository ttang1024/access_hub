namespace AccessHub.Cardholder.Application.Features.AccessGroups;

public sealed record UpdateAccessGroupCommand(Guid AccessGroupId, string Name, IReadOnlyList<AccessGroupDoorInput> Doors, int? ExpectedVersion);
