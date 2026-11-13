namespace AccessHub.Alarm.Infrastructure.Persistence;

/// <summary>An AccessDenied event remembered for the repeated-denials rule.</summary>
public sealed class RecentDenial
{
    public Guid SourceEventId { get; set; }

    public Guid SiteId { get; set; }

    public string CardNumber { get; set; } = string.Empty;

    public DateTimeOffset OccurredAt { get; set; }
}
