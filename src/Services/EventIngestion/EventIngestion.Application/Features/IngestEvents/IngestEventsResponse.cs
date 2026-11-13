namespace AccessHub.EventIngestion.Application.Features.IngestEvents;

/// <summary>Duplicates are a success: the site may safely retry a batch whose acknowledgement it never received.</summary>
public sealed record IngestEventsResponse(int Accepted, int Duplicate);
