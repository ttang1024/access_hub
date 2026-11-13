namespace AccessHub.BuildingBlocks.Infrastructure.Auditing;

/// <summary>One audited state change: who, what, when, and a before/after summary.</summary>
public sealed class AuditEntry
{
    private AuditEntry()
    {
    }

    /// <summary>Creates an entry.</summary>
    public AuditEntry(DateTimeOffset occurredAt, string userId, string userName, string action, string entityType, string entityId, string? before, string? after, string? traceId)
    {
        Id = Guid.CreateVersion7();
        OccurredAt = occurredAt;
        UserId = userId;
        UserName = userName;
        Action = action;
        EntityType = entityType;
        EntityId = entityId;
        Before = before;
        After = after;
        TraceId = traceId;
    }

    /// <summary>Entry ID.</summary>
    public Guid Id { get; private set; }

    /// <summary>When the change was made (UTC).</summary>
    public DateTimeOffset OccurredAt { get; private set; }

    /// <summary>Who made it.</summary>
    public string UserId { get; private set; } = string.Empty;

    /// <summary>Display name of who made it.</summary>
    public string UserName { get; private set; } = string.Empty;

    /// <summary>What was done.</summary>
    public string Action { get; private set; } = string.Empty;

    /// <summary>Kind of entity changed.</summary>
    public string EntityType { get; private set; } = string.Empty;

    /// <summary>ID of the entity changed.</summary>
    public string EntityId { get; private set; } = string.Empty;

    /// <summary>JSON summary before the change.</summary>
    public string? Before { get; private set; }

    /// <summary>JSON summary after the change.</summary>
    public string? After { get; private set; }

    /// <summary>Trace ID of the request, to correlate with telemetry.</summary>
    public string? TraceId { get; private set; }
}
