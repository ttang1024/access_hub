namespace AccessHub.EventIngestion.Application.Features.IngestEvents;

public sealed record IncomingDoorEvent(
    Guid EventId, Guid DoorId, string Type, DateTimeOffset OccurredAt, string? CardNumber, string? DenyReason, long? AccessListSequence);
