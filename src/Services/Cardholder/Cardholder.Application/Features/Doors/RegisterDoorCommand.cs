namespace AccessHub.Cardholder.Application.Features.Doors;

public sealed record RegisterDoorCommand(Guid SiteId, string Name);
