namespace AccessHub.Contracts.IntegrationEvents.Cardholder;

/// <summary>
/// Cardholder events carry the aggregate version and its full current state, so a consumer can upsert from any
/// single event and ignore anything not newer than what it already holds.
/// </summary>
public interface IVersionedStateEvent : IIntegrationEvent
{
    /// <summary>The aggregate version after the change.</summary>
    int Version { get; }
}
