# ADR 0011: Door events published per stored batch and type

- Status: Accepted
- Date: 2026-11-13

## Context

The ingestion target is 10 batches of 200 events per second with p99 under 250 ms. Publishing one `DoorEventRecorded` per event cost, for every upload:

- 200 MassTransit outbox rows written by EF, one `INSERT ... RETURNING` each;
- 200 publisher-confirmed broker publishes;
- one inbox transaction per event in every downstream consumer.

Measured on the development laptop: outbox delivery of about 600 messages/s per instance against 2,000 produced, consumers saturating Postgres on commits, and ingestion p99 between 376 ms and 896 ms. Adding ingestion replicas raised delivery but made latency worse (p99 16 s), because the consumers then saturated the shared Postgres.

## Decision

- EventIngestion publishes **`DoorEventsRecorded`**: one message per event type per stored batch, so at most six per upload, carrying the newly stored events of that type in `OccurredAt` order. It still goes through the outbox, in the same transaction as the insert, and only for events actually inserted. A replayed batch publishes nothing.
- `DoorEventRecorded` keeps its shape as the per-event view, produced by `DoorEventsRecorded.Expand()`. It is no longer published on its own. This follows the contract versioning rule: a new type, not a changed one.
- Each message holds one type, so [ADR 0010](0010-type-routed-door-events-and-stateless-live-feed.md)'s routing still applies (routing key = type).
- Consumers work set-based on the batch, in one inbox transaction:
  - **Stale grants (AccessControl):** loads all the batch's cards and cardholders in two queries.
  - **Repeated denials (Alarm):** locks the batch's cards in sorted order, so concurrent batches cannot deadlock; reads the stored denials; inserts the new ones; then evaluates each denial in `OccurredAt` order against what came before. A batch triggers exactly as its events would arriving one at a time, and already-stored events trigger nothing.
  - **Live feed:** pushes each event.

## Alternative rejected

**Keep per-event messages and scale out.** Tried: three ingestion replicas tripled outbox delivery and moved the bottleneck to consumer commits. The per-event transaction cost is the problem, and more replicas do not remove it.

## Consequences

- Per upload: one insert plus at most six outbox rows. Downstream: one transaction per type per upload, not one per event.
- Measured on the same laptop, warm: p99 71 to 241 ms, median about 17 ms, with the outbox and every queue at about zero throughout.
- Messages are larger, up to 500 events. The broker's default 128 MiB frame limit leaves wide headroom.
- Inbox deduplication now works per batch message. Per-event idempotency still comes from the event key (ingestion), the detection key (stale grants) and the stored denials (alarms).
- A failing event now fails its whole batch message, which is retried as a unit.
- Any future consumer must handle `DoorEventsRecorded`.
