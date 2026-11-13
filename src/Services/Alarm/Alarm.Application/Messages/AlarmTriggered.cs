namespace AccessHub.Alarm.Application.Messages;

/// <summary>Internal to the Alarm service: a rule fired. Correlated to the open alarm for the subject, or starts one.</summary>
public sealed record AlarmTriggered(
    string Rule, string Priority, string SubjectKey, Guid SiteId, Guid? DoorId, string? CardNumber, Guid SourceEventId, DateTimeOffset TriggeredAt);
