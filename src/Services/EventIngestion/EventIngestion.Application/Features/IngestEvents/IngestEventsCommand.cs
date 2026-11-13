namespace AccessHub.EventIngestion.Application.Features.IngestEvents;

/// <param name="SiteId">The site in the route.</param>
/// <param name="AuthenticatedSiteId">The site the API key belongs to; must match.</param>
/// <param name="Events">1 to 500 events.</param>
public sealed record IngestEventsCommand(Guid SiteId, Guid AuthenticatedSiteId, IReadOnlyList<IncomingDoorEvent> Events);
