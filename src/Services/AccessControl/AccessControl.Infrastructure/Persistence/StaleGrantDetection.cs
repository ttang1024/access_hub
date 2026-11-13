namespace AccessHub.AccessControl.Infrastructure.Persistence;

/// <summary>One row per flagged grant; its key makes the detection idempotent.</summary>
public sealed class StaleGrantDetection
{
    public Guid SourceEventId { get; set; }

    public string Reason { get; set; } = string.Empty;

    public DateTimeOffset DetectedAt { get; set; }
}
