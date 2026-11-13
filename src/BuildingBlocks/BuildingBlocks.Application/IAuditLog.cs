namespace AccessHub.BuildingBlocks.Application;

/// <summary>
/// Records who changed what. Entries are written to the service's own <c>audit_entries</c> table
/// in the same transaction as the change.
/// </summary>
public interface IAuditLog
{
    /// <summary>Adds an audit entry to the current unit of work.</summary>
    /// <param name="action">What happened, for example <c>Card.Revoked</c>.</param>
    /// <param name="entityType">The kind of entity changed.</param>
    /// <param name="entityId">The changed entity's ID.</param>
    /// <param name="before">Summary of the state before the change, or null on creation.</param>
    /// <param name="after">Summary of the state after the change, or null on deletion.</param>
    void Record(string action, string entityType, string entityId, object? before, object? after);
}
