using AccessHub.EventIngestion.Application.Features.IngestEvents;

namespace AccessHub.EventIngestion.Api.Endpoints;

public sealed record IngestEventsRequest(IReadOnlyList<IncomingDoorEvent> Events);
