namespace AccessHub.Contracts.IntegrationEvents;

/// <summary>
/// Every integration event carries a unique ID, the time the underlying change happened and a correlation ID.
/// Events are immutable; a breaking change creates a new type with a version suffix.
/// </summary>
public interface IIntegrationEvent
{
    /// <summary>Unique ID of this event.</summary>
    Guid EventId { get; }

    /// <summary>When the underlying change happened (UTC).</summary>
    DateTimeOffset OccurredAt { get; }

    /// <summary>Correlates the event with the request or event that caused it.</summary>
    Guid CorrelationId { get; }
}
