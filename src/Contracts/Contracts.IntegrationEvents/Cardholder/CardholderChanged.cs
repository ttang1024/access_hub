namespace AccessHub.Contracts.IntegrationEvents.Cardholder;

/// <summary>A cardholder was created or changed. Carries the full current state.</summary>
/// <param name="EventId">Event ID.</param>
/// <param name="OccurredAt">When the change happened.</param>
/// <param name="CorrelationId">Correlation ID.</param>
/// <param name="CardholderId">Cardholder ID.</param>
/// <param name="FirstName">First name.</param>
/// <param name="LastName">Last name.</param>
/// <param name="IsActive">Whether the cardholder is active.</param>
/// <param name="AccessGroupIds">Access groups the cardholder belongs to.</param>
/// <param name="Version">Aggregate version.</param>
public sealed record CardholderChanged(
    Guid EventId, DateTimeOffset OccurredAt, Guid CorrelationId,
    Guid CardholderId, string FirstName, string LastName, bool IsActive, IReadOnlyList<Guid> AccessGroupIds, int Version) : IVersionedStateEvent;
