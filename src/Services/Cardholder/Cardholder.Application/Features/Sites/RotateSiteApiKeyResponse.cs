namespace AccessHub.Cardholder.Application.Features.Sites;

public sealed record RotateSiteApiKeyResponse(Guid SiteId, string ApiKey);
