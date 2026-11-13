using System.Diagnostics;
using AccessHub.SiteSimulator.Storage;
using AccessHub.SiteSimulator.Sync;

namespace AccessHub.SiteSimulator.Simulation;

/// <summary>
/// Generates realistic activity: swipes decided locally (never a network call), door open/close, and occasional
/// forced doors, held-open doors and bursts of denied swipes. Every event goes to the SQLite buffer first.
/// </summary>
public sealed class SwipeGenerator(SiteIdentity site, AccessListHolder holder, SiteStore store, SimulatorMetrics metrics, TimeProvider clock, Random random)
{
    public async Task RunAsync(TimeSpan interval, CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            Tick();
            try
            {
                await Task.Delay(interval * (0.5 + random.NextDouble()), clock, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal shutdown between swipes.
            }
        }
    }

    /// <summary>One step of activity at one door.</summary>
    public void Tick()
    {
        var door = site.DoorIds[random.Next(site.DoorIds.Count)];
        var roll = random.NextDouble();
        var events = roll switch
        {
            < 0.01 => Forced(door),
            < 0.03 => DeniedBurst(door),
            _ => Swipe(door, PickCard(), heldOpen: random.NextDouble() < 0.03),
        };
        store.BufferEvents(events);
    }

    /// <summary>Decides one swipe locally and returns the events it produces.</summary>
    public List<BufferedEvent> Swipe(Guid door, string cardNumber, bool heldOpen = false)
    {
        var now = clock.GetUtcNow();
        var started = Stopwatch.GetTimestamp();
        var (decision, sequence) = holder.Decide(door, cardNumber, now);
        metrics.RecordDecision(decision, Stopwatch.GetElapsedTime(started), site.SiteId);

        var events = new List<BufferedEvent>
        {
            new(Guid.CreateVersion7(), door, decision.Granted ? "AccessGranted" : "AccessDenied", now, cardNumber, decision.Reason?.ToString(), sequence),
        };
        if (decision.Granted)
        {
            events.Add(Simple(door, "DoorOpened", now.AddSeconds(1)));
            if (heldOpen)
            {
                events.Add(Simple(door, "DoorHeldOpen", now.AddSeconds(31)));
                events.Add(Simple(door, "DoorClosed", now.AddSeconds(45)));
            }
            else
            {
                events.Add(Simple(door, "DoorClosed", now.AddSeconds(5)));
            }
        }

        return events;
    }

    private List<BufferedEvent> Forced(Guid door)
    {
        var now = clock.GetUtcNow();
        return [Simple(door, "DoorForced", now), Simple(door, "DoorClosed", now.AddSeconds(8))];
    }

    // Someone tries the same bad card three times in quick succession: the repeated-denials alarm should fire.
    private List<BufferedEvent> DeniedBurst(Guid door)
    {
        var card = $"X{random.Next(100_000, 999_999)}";
        return [.. Swipe(door, card), .. Swipe(door, card), .. Swipe(door, card)];
    }

    private string PickCard()
    {
        var known = site.CardNumbers.Count > 0 ? site.CardNumbers : holder.CardNumbers();
        return known.Count == 0 || random.NextDouble() < 0.04 ? $"U{random.Next(100_000, 999_999)}" : known[random.Next(known.Count)];
    }

    private static BufferedEvent Simple(Guid door, string type, DateTimeOffset at) => new(Guid.CreateVersion7(), door, type, at, null, null, null);
}
