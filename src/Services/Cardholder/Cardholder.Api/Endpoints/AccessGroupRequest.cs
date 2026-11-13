using AccessHub.Cardholder.Application.Features.AccessGroups;

namespace AccessHub.Cardholder.Api.Endpoints;

public sealed record AccessGroupRequest(string Name, IReadOnlyList<AccessGroupDoorInput> Doors, int? ExpectedVersion);
