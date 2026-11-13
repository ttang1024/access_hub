namespace AccessHub.Cardholder.Application.Features.AccessGroups;

public sealed record DeleteAccessGroupCommand(Guid AccessGroupId, int? ExpectedVersion);
