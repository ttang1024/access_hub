using AccessHub.Alarm.Infrastructure.Sagas;
using AccessHub.BuildingBlocks.Infrastructure.Auditing;

namespace AccessHub.Alarm.Infrastructure.Persistence;

/// <summary>Adds the audit entry to the saga's DbContext, so it commits with the state change.</summary>
internal sealed class AlarmAudit(AlarmDbContext db, TimeProvider clock) : IAlarmAudit
{
    public void Record(string action, Guid alarmId, string userId, string fromState, string toState, string? note) =>
        db.AuditEntries.Add(new AuditEntry(
            clock.GetUtcNow(), userId, userId, action, "Alarm", alarmId.ToString(),
            $$"""{"state":"{{fromState}}"}""",
            System.Text.Json.JsonSerializer.Serialize(new { state = toState, note }),
            System.Diagnostics.Activity.Current?.TraceId.ToString()));
}
