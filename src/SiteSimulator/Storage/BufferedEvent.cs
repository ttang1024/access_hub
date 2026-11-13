namespace AccessHub.SiteSimulator.Storage;

/// <summary>A site event as stored in the local buffer and uploaded to the cloud.</summary>
public sealed record BufferedEvent(
    Guid EventId, Guid DoorId, string Type, DateTimeOffset OccurredAt, string? CardNumber, string? DenyReason, long? AccessListSequence);
