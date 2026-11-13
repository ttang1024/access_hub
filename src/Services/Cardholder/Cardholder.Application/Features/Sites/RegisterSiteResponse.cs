namespace AccessHub.Cardholder.Application.Features.Sites;

/// <summary>The API key is returned exactly once, at registration. Only its hash is stored.</summary>
public sealed record RegisterSiteResponse(Guid SiteId, string ApiKey);
