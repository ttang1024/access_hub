namespace AccessHub.AccessControl.Application.Features.Explain;

public sealed record ExplainAccessQuery(Guid SiteId, Guid DoorId, string CardNumber, DateTimeOffset? Timestamp);
