namespace AccessHub.AccessControl.Domain.ReadModel;

/// <summary>
/// Read-model state built from Cardholder events that carry full state and a version. A change is applied only when
/// its version is newer, so redelivered and out-of-order events are no-ops.
/// </summary>
public abstract class VersionedState
{
    public int Version { get; protected set; }

    public bool IsNewer(int version) => version > Version;
}
