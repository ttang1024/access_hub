using AccessHub.Alarm.Application.Abstractions;
using AccessHub.Alarm.Domain;
using AccessHub.BuildingBlocks.Application;
using AccessHub.BuildingBlocks.Domain;
using AccessHub.Contracts.IntegrationEvents.EventIngestion;

namespace AccessHub.Alarm.Application.Features.Rules;

/// <summary>
/// Applies the door and repeated-denial rules to a batch of recorded site events, using OccurredAt, never arrival
/// time. Denials are evaluated one by one in OccurredAt order against what was already stored, so a batch triggers
/// exactly as the same events arriving one at a time would; an event already recorded (a redelivery) is skipped.
/// </summary>
public sealed class EvaluateDoorEventsHandler(IDenialStore denials, IAlarmTriggerSink triggers) : ICommandHandler<DoorEventsRecorded>
{
    public async Task<Result> HandleAsync(DoorEventsRecorded batch, CancellationToken cancellationToken)
    {
        var events = batch.Expand().OrderBy(e => e.OccurredAt).ToList();
        foreach (var e in events)
        {
            if (AlarmRules.ForDoorEvent(e.Type.ToString(), e.SiteId, e.DoorId, e.SourceEventId, e.OccurredAt) is { } doorTrigger)
            {
                await triggers.TriggerAsync(doorTrigger, cancellationToken);
            }
        }

        if (batch.Type != DoorEventType.AccessDenied)
        {
            return Result.Success();
        }

        var incoming = events.Where(e => e.CardNumber is not null).Select(e => new StoredDenial(e.SourceEventId, e.CardNumber!, e.OccurredAt)).ToList();
        if (incoming.Count == 0)
        {
            return Result.Success();
        }

        var stored = await denials.RecordAsync(batch.SiteId, incoming, AlarmRules.DenialWindow, cancellationToken);
        var seen = stored.Select(d => d.SourceEventId).ToHashSet();
        var history = stored.GroupBy(d => d.CardNumber, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.Select(d => d.OccurredAt).ToList(), StringComparer.Ordinal);
        foreach (var denial in incoming)
        {
            if (!seen.Add(denial.SourceEventId))
            {
                continue;
            }

            if (!history.TryGetValue(denial.CardNumber, out var times))
            {
                history[denial.CardNumber] = times = [];
            }

            times.Add(denial.OccurredAt);
            if (AlarmRules.RepeatedDenialsTriggered(times, denial.OccurredAt))
            {
                var e = events.First(x => x.SourceEventId == denial.SourceEventId);
                await triggers.TriggerAsync(
                    AlarmTrigger.ForCard(AlarmRule.RepeatedDenials, AlarmPriority.Medium, batch.SiteId, e.DoorId, denial.CardNumber, denial.SourceEventId, denial.OccurredAt),
                    cancellationToken);
            }
        }

        return Result.Success();
    }
}
