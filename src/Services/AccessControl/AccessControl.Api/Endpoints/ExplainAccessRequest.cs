namespace AccessHub.AccessControl.Api.Endpoints;

public sealed record ExplainAccessRequest(Guid SiteId, Guid DoorId, string CardNumber, DateTimeOffset? Timestamp);
