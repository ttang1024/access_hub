using System.Diagnostics;
using System.Text.Json;
using AccessHub.BuildingBlocks.Application;
using AccessHub.BuildingBlocks.Infrastructure.Persistence;

namespace AccessHub.BuildingBlocks.Infrastructure.Auditing;

/// <summary>Adds audit entries to the service DbContext so they commit with the change they describe.</summary>
public sealed class AuditLog(AccessHubDbContext dbContext, ICurrentUser currentUser, TimeProvider timeProvider) : IAuditLog
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public void Record(string action, string entityType, string entityId, object? before, object? after)
    {
        dbContext.AuditEntries.Add(new AuditEntry(
            timeProvider.GetUtcNow(),
            currentUser.UserId,
            currentUser.UserName,
            action,
            entityType,
            entityId,
            before is null ? null : JsonSerializer.Serialize(before, JsonOptions),
            after is null ? null : JsonSerializer.Serialize(after, JsonOptions),
            Activity.Current?.TraceId.ToString()));
    }
}
