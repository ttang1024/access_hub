namespace AccessHub.Cardholder.Application.Abstractions;

public sealed record DoorDto(Guid Id, Guid SiteId, string Name, int Version);
