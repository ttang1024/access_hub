# ADR 0010: Type-routed door events and a stateless live feed

- Status: Accepted. Amended by [ADR 0011](0011-batched-door-events.md): the routed message is now the per-type batch `DoorEventsRecorded`.
- Date: 2026-11-12

## Context

The ingestion load target is 10 batches of 200 events per second, which was 2,000 `DoorEventRecorded` messages per second at the time.

The first measured run against the local AppHost met the error and throughput targets but missed latency (p99 896 ms against 250 ms). It also showed something worse: each consumer drained only about 90 messages per second, leaving about 234,000 messages queued per consumer.

Postgres was saturated on commits, not queries. Every consumer paid a full inbox transaction for every event, including the majority it ignores:

| Consumer                     | Needs                                           |
| ---------------------------- | ----------------------------------------------- |
| AccessControl (stale grants) | `AccessGranted`                                 |
| Alarm rules                  | `DoorForced`, `DoorHeldOpen`, `AccessDenied`    |
| Live feed                    | Everything, but it writes nothing to a database |

## Decision

1. **Route door events by type.** They are published to a topic exchange with the event type as routing key (`DoorEventRouting.ConfigureDoorEventTopology`, applied on every bus). Each consumer's queue binds only to the types it handles. Consumers keep their own type checks, so a broader binding, or the in-memory test transport, which has no routing, is never incorrect, only slower.
2. **The live feed consumer skips the inbox and outbox.** It only pushes the event to connected dashboards over SignalR. A redelivery pushes the same event twice and the dashboard deduplicates by event ID. It still retries, and still goes to the error queue on failure.

## Consequences

- An event a consumer would ignore no longer costs it a transaction. Under the k6 mix, AccessControl and the alarm rules each receive about a quarter of the events.
- Every bus that touches the exchange, including test listeners, must declare it as a topic exchange. One shared helper does this. Changing the exchange type on an existing broker requires deleting the old exchange once.
- One consumer intentionally has at-least-once _push_ semantics without deduplication. It is documented here and tested (`EventsTheRulesIgnore_NeverReachTheRulesQueue_ButStillReachTheLiveFeed`).
- Routing alone did not meet the latency target (p99 670 ms). It removed wasted work; ADR 0011 removed the per-event cost.
